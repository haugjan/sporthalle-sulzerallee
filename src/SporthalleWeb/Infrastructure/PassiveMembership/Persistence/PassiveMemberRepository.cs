using Microsoft.AspNetCore.Identity;
using SporthalleWeb.Domain.PassiveMembership;
using SporthalleWeb.Domain.PassiveMembership.Ports;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace SporthalleWeb.Infrastructure.PassiveMembership.Persistence;

public class PassiveMemberRepository : IPassivMemberRepository
{
    private const string MemberTypeAlias = "passivMember";

    private readonly IMemberService _memberService;
    private readonly IMemberManager _memberManager;

    public PassiveMemberRepository(IMemberService memberService, IMemberManager memberManager)
    {
        _memberService = memberService;
        _memberManager = memberManager;
    }

    // ── Query ─────────────────────────────────────────────────────────────────

    public Task<bool> IsFieldTakenAsync(FieldNumber field)
    {
        var fieldStr = field.Value.ToString();
        var taken = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Any(m => m.GetValue<string>("fieldNumber") == fieldStr
                   && m.GetValue<string>("status") != MemberStatus.Deleted);
        return Task.FromResult(taken);
    }

    public Task<IReadOnlyList<PassiveMember>> GetPendingAsync()
    {
        var result = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => (m.GetValue<string>("status") ?? MemberStatus.Pending) == MemberStatus.Pending)
            .OrderBy(m => m.CreateDate)
            .Select(Reconstitute)
            .ToList();
        return Task.FromResult<IReadOnlyList<PassiveMember>>(result);
    }

    public Task<IReadOnlyList<PassiveMember>> GetConfirmedAsync()
    {
        var result = _memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => m.GetValue<string>("status") == MemberStatus.Confirmed)
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
            .Where(m => m.GetValue<string>("status") != MemberStatus.Deleted)
            .Select(m =>
            {
                _ = int.TryParse(m.GetValue<string>("fieldNumber"), out var fn);
                var status = m.GetValue<string>("status");
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
        var username = Username(member.FieldNumber.Value);

        // Re-register a previously soft-deleted field: reuse the existing slot.
        var existingUser = await _memberManager.FindByNameAsync(username);
        if (existingUser is not null)
        {
            var existing = _memberService.GetById(int.Parse(existingUser.Id))
                ?? throw new DomainException("Existing member slot could not be found.");
            existing.Name = $"{member.FirstName} {member.LastName}".Trim();
            existing.Email = member.Email.Value;
            SetProperties(existing, member);
            _memberService.Save(existing);
            return Reconstitute(existing);
        }

        var user = new MemberIdentityUser
        {
            UserName = username,
            Email = member.Email.Value,
            Name = $"{member.FirstName} {member.LastName}".Trim(),
            MemberTypeAlias = MemberTypeAlias,
            IsApproved = true
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

    private static void SetProperties(IMember m, PassiveMember pm)
    {
        m.SetValue("firstName",              pm.FirstName);
        m.SetValue("lastName",               pm.LastName);
        m.SetValue("fieldNumber",            pm.FieldNumber.Value.ToString());
        m.SetValue("membershipLevel",        pm.Level.Key);
        m.SetValue("billingAddress",         pm.AddressLine);
        m.SetValue("addressLine2",           pm.AddressLine2 ?? "");
        m.SetValue("billingPostalCode",      pm.PostalCode);
        m.SetValue("billingCity",            pm.City);
        m.SetValue("billingCountry",         pm.Country);
        m.SetValue("phone",                  pm.Phone ?? "");
        m.SetValue("showNameOnFloor",        pm.ShowNameOnFloor);
        m.SetValue("floorDisplayName",       pm.DisplayName ?? "");
        m.SetValue("status",                 pm.Status);
        m.SetValue("paidAt",                 pm.PaidAt?.ToString("O") ?? "");
        m.SetValue("paidBy",                 pm.PaidBy ?? "");
        m.SetValue("confirmedAt",            pm.ConfirmedAt?.ToString("O") ?? "");
        m.SetValue("confirmedBy",            pm.ConfirmedBy ?? "");
        m.SetValue("exportedToAccountingAt", pm.ExportedToAccountingAt?.ToString("O") ?? "");
        m.SetValue("exportedToAccountingBy", pm.ExportedToAccountingBy ?? "");
        m.SetValue("notes",                  pm.Notes ?? "");
    }

    private static PassiveMember Reconstitute(IMember m)
    {
        _ = int.TryParse(m.GetValue<string>("fieldNumber"), out var fieldNumber);
        return PassiveMember.Reconstitute(
            id:                    m.Id,
            fieldNumber:           fieldNumber,
            firstName:             m.GetValue<string>("firstName") ?? "",
            lastName:              m.GetValue<string>("lastName") ?? "",
            addressLine:           m.GetValue<string>("billingAddress") ?? "",
            addressLine2:          m.GetValue<string>("addressLine2").NullIfEmpty(),
            postalCode:            m.GetValue<string>("billingPostalCode") ?? "",
            city:                  m.GetValue<string>("billingCity") ?? "",
            country:               m.GetValue<string>("billingCountry").NullIfEmpty() ?? "Schweiz",
            phone:                 m.GetValue<string>("phone").NullIfEmpty(),
            email:                 m.Email ?? "",
            levelKey:              m.GetValue<string>("membershipLevel") ?? "Bronze",
            showNameOnFloor:       m.GetValue<bool>("showNameOnFloor"),
            displayName:           m.GetValue<string>("floorDisplayName").NullIfEmpty(),
            createdAt:             m.CreateDate,
            status:                m.GetValue<string>("status").NullIfEmpty() ?? MemberStatus.Pending,
            confirmedAt:           Iso(m.GetValue<string>("confirmedAt")),
            confirmedBy:           m.GetValue<string>("confirmedBy").NullIfEmpty(),
            paidAt:                Iso(m.GetValue<string>("paidAt")),
            paidBy:                m.GetValue<string>("paidBy").NullIfEmpty(),
            exportedToAccountingAt: Iso(m.GetValue<string>("exportedToAccountingAt")),
            exportedToAccountingBy: m.GetValue<string>("exportedToAccountingBy").NullIfEmpty(),
            notes:                 m.GetValue<string>("notes").NullIfEmpty()
        );
    }

    private static DateTime? Iso(string? s)
        => string.IsNullOrWhiteSpace(s) ? null
            : DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt) ? dt : null;
}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
