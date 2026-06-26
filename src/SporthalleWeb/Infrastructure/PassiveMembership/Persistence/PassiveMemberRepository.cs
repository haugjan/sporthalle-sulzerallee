using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;
using SporthalleWeb.Infrastructure.Booking.Members;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Persistence;

public class PassiveMemberRepository : IPassiveMemberRepository
{
    private const string MemberTypeAlias = "passivMember";

    private readonly IMemberService _memberService;
    private readonly IMemberManager _memberManager;
    private readonly ILogger<PassiveMemberRepository> _logger;

    public PassiveMemberRepository(IMemberService memberService, IMemberManager memberManager,
        ILogger<PassiveMemberRepository> logger)
    {
        _memberService = memberService;
        _memberManager = memberManager;
        _logger = logger;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public Task<bool> IsFieldTakenAsync(FieldNumber field)
    {
        var fieldStr = field.Value.ToString();
        var taken = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Any(m => m.GetValue<string>("fieldNumber") == fieldStr
                   && UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null) != MemberStatus.Deleted);
        return Task.FromResult(taken);
    }

    public Task<IReadOnlyList<PassiveMember>> GetPendingAsync()
    {
        var result = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => (UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null) ?? MemberStatus.Pending) == MemberStatus.Pending)
            .OrderBy(m => m.CreateDate)
            .Select(Reconstitute)
            .ToList();
        return Task.FromResult<IReadOnlyList<PassiveMember>>(result);
    }

    public Task<IReadOnlyList<PassiveMember>> GetConfirmedAsync()
    {
        var result = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null) == MemberStatus.Confirmed)
            .OrderBy(m => int.TryParse(m.GetValue<string>("fieldNumber"), out var fn) ? fn : 0)
            .Select(Reconstitute)
            .ToList();
        return Task.FromResult<IReadOnlyList<PassiveMember>>(result);
    }

    public Task<PassiveMember?> FindByIdAsync(int id)
    {
        var m = _memberService.GetById(id);
        if (m is null || m.ContentType.Alias != MemberTypeAlias)
            return Task.FromResult<PassiveMember?>(null);
        return Task.FromResult<PassiveMember?>(Reconstitute(m));
    }

    public Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync()
    {
        var result = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null) != MemberStatus.Deleted)
            .Select(m =>
            {
                _ = int.TryParse(m.GetValue<string>("fieldNumber"), out var fn);
                var status = UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null);
                var show = m.GetValue<bool>("showNameOnFloor");
                var displayName = show && status == MemberStatus.Confirmed
                    ? m.GetValue<string>("floorDisplayName").NullIfEmpty()
                    : null;
                return (new FieldNumber(fn), displayName);
            })
            .ToList();
        return Task.FromResult<IReadOnlyList<(FieldNumber, string?)>>(result);
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public async Task<PassiveMember> SaveAsync(PassiveMember member)
    {
        try
        {
            return await SaveInternalAsync(member);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save PassiveMember for field {Field}", member.FieldNumber.Value);
            throw new DomainException($"Registrierung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task<PassiveMember> SaveInternalAsync(PassiveMember member)
    {
        var username = Username(member.FieldNumber.Value);

        // Re-register a previously soft-deleted field: reuse the existing slot.
        var existingUser = await _memberManager.FindByNameAsync(username);
        if (existingUser is not null)
        {
            var existing = _memberService.GetById(int.Parse(existingUser.Id))
                ?? throw new DomainException("Existing member slot could not be found.");
            existing.Name = $"{member.FirstName} {member.LastName}".Trim();
            existing.Email = SyntheticEmail(member.FieldNumber.Value);
            SetProperties(existing, member);
            _memberService.Save(existing);
            return Reconstitute(existing);
        }

        var user = new MemberIdentityUser
        {
            UserName        = username,
            Email           = SyntheticEmail(member.FieldNumber.Value),
            Name            = $"{member.FirstName} {member.LastName}".Trim(),
            MemberTypeAlias = MemberTypeAlias,
            IsApproved      = true
        };

        var result = await _memberManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new DomainException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var created = await _memberManager.FindByNameAsync(username)
            ?? throw new DomainException("Member could not be found after creation.");

        var umbracoMember = _memberService.GetById(int.Parse(created.Id))
            ?? throw new DomainException("Member could not be found after creation.");

        SetProperties(umbracoMember, member);
        _memberService.Save(umbracoMember);

        return Reconstitute(umbracoMember);
    }

    public Task UpdateAsync(PassiveMember member)
    {
        var m = _memberService.GetById(member.Id)
            ?? throw new MemberNotFoundException(member.Id);
        SetProperties(m, member);
        _memberService.Save(m);
        return Task.CompletedTask;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static string Username(int fieldNumber) => $"pm-{fieldNumber:D3}";
    private static string SyntheticEmail(int fieldNumber) => $"pm-{fieldNumber:D3}@passiv.internal";

    // internal for MemberTypeConsistencyTests
    internal static void SetProperties(IMember m, PassiveMember pm)
    {
        m.SetValue(PassivMemberAliases.Email,                  pm.Email.Value);
        m.SetValue(PassivMemberAliases.FirstName,              pm.FirstName);
        m.SetValue(PassivMemberAliases.LastName,               pm.LastName);
        m.SetValue(PassivMemberAliases.FieldNumber,            pm.FieldNumber.Value.ToString());
        m.SetValue(PassivMemberAliases.MembershipLevel,        pm.Level.Key);
        m.SetValue(PassivMemberAliases.BillingAddress,         pm.AddressLine);
        m.SetValue(PassivMemberAliases.AddressLine2,           pm.AddressLine2 ?? "");
        m.SetValue(PassivMemberAliases.BillingPostalCode,      pm.PostalCode);
        m.SetValue(PassivMemberAliases.BillingCity,            pm.City);
        m.SetValue(PassivMemberAliases.BillingCountry,         pm.Country);
        m.SetValue(PassivMemberAliases.Phone,                  pm.Phone ?? "");
        m.SetValue(PassivMemberAliases.ShowNameOnFloor,        pm.ShowNameOnFloor);
        m.SetValue(PassivMemberAliases.FloorDisplayName,       pm.DisplayName ?? "");
        m.SetValue(PassivMemberAliases.Status,                 pm.Status);
        m.SetValue(PassivMemberAliases.PaidAt,                 (object?)pm.PaidAt);
        m.SetValue(PassivMemberAliases.PaidBy,                 pm.PaidBy ?? "");
        m.SetValue(PassivMemberAliases.ConfirmedAt,            (object?)pm.ConfirmedAt);
        m.SetValue(PassivMemberAliases.ConfirmedBy,            pm.ConfirmedBy ?? "");
        m.SetValue(PassivMemberAliases.ExportedToAccountingAt, (object?)pm.ExportedToAccountingAt);
        m.SetValue(PassivMemberAliases.ExportedToAccountingBy, pm.ExportedToAccountingBy ?? "");
        m.SetValue(PassivMemberAliases.Notes,                  pm.Notes ?? "");
    }

    private static PassiveMember Reconstitute(IMember m)
    {
        _ = int.TryParse(m.GetValue<string>(PassivMemberAliases.FieldNumber), out var fieldNumber);
        return PassiveMember.Reconstitute(
            id:                     m.Id,
            fieldNumber:            fieldNumber,
            firstName:              m.GetValue<string>(PassivMemberAliases.FirstName) ?? "",
            lastName:               m.GetValue<string>(PassivMemberAliases.LastName) ?? "",
            addressLine:            m.GetValue<string>(PassivMemberAliases.BillingAddress) ?? "",
            addressLine2:           m.GetValue<string>(PassivMemberAliases.AddressLine2).NullIfEmpty(),
            postalCode:             m.GetValue<string>(PassivMemberAliases.BillingPostalCode) ?? "",
            city:                   m.GetValue<string>(PassivMemberAliases.BillingCity) ?? "",
            country:                m.GetValue<string>(PassivMemberAliases.BillingCountry).NullIfEmpty() ?? "Schweiz",
            phone:                  m.GetValue<string>(PassivMemberAliases.Phone).NullIfEmpty(),
            email:                  m.GetValue<string>(PassivMemberAliases.Email) ?? "",
            levelKey:               UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>(PassivMemberAliases.MembershipLevel), null) ?? "Bronze",
            showNameOnFloor:        m.GetValue<bool>(PassivMemberAliases.ShowNameOnFloor),
            displayName:            m.GetValue<string>(PassivMemberAliases.FloorDisplayName).NullIfEmpty(),
            createdAt:              m.CreateDate,
            status:                 UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>(PassivMemberAliases.Status), null) ?? MemberStatus.Pending,
            confirmedAt:            m.GetValue<DateTime?>(PassivMemberAliases.ConfirmedAt),
            confirmedBy:            m.GetValue<string>(PassivMemberAliases.ConfirmedBy).NullIfEmpty(),
            paidAt:                 m.GetValue<DateTime?>(PassivMemberAliases.PaidAt),
            paidBy:                 m.GetValue<string>(PassivMemberAliases.PaidBy).NullIfEmpty(),
            exportedToAccountingAt: m.GetValue<DateTime?>(PassivMemberAliases.ExportedToAccountingAt),
            exportedToAccountingBy: m.GetValue<string>(PassivMemberAliases.ExportedToAccountingBy).NullIfEmpty(),
            notes:                  m.GetValue<string>(PassivMemberAliases.Notes).NullIfEmpty()
        );
    }

}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
