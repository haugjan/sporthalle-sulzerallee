using SporthalleWeb.Features.PassiveMembership.Registration;
using SporthalleWeb.Domain.PassiveMembership.PassiveMemberAggregate;
using SporthalleWeb.Infrastructure.Shared;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;

namespace SporthalleWeb.Infrastructure.PassiveMembership;

public class UmbracoPassiveMembers(
    IMemberService memberService,
    IMemberManager memberManager,
    ILogger<UmbracoPassiveMembers> logger)
    : IPassiveMembers
{
    private const string MemberTypeAlias = "passivMember";

    public Task<bool> IsFieldTakenAsync(FieldNumber field)
    {
        var fieldStr = field.Value.ToString();
        var taken = memberService.GetMembersByMemberType(MemberTypeAlias)
            .Any(m => m.GetValue<string>("fieldNumber") == fieldStr
                   && UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null) != MemberStatus.Deleted.Key);
        return Task.FromResult(taken);
    }

    public Task<IReadOnlyList<PassiveMember>> GetPendingAsync()
    {
        var result = memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => (UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null)) == MemberStatus.Pending.Key)
            .OrderBy(m => m.CreateDate)
            .Select(ReconstituteOrNull)
            .OfType<PassiveMember>()
            .ToList();
        return Task.FromResult<IReadOnlyList<PassiveMember>>(result);
    }

    public Task<IReadOnlyList<PassiveMember>> GetConfirmedAsync()
    {
        var result = memberService.GetMembersByMemberType(MemberTypeAlias)
            .Where(m => UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null) == MemberStatus.Confirmed.Key)
            .OrderBy(m => int.TryParse(m.GetValue<string>("fieldNumber"), out var fn) ? fn : 0)
            .Select(ReconstituteOrNull)
            .OfType<PassiveMember>()
            .ToList();
        return Task.FromResult<IReadOnlyList<PassiveMember>>(result);
    }

    private PassiveMember? ReconstituteOrNull(IMember m)
    {
        try
        {
            return Reconstitute(m);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Skipping passivMember {MemberId} with invalid data.", m.Id);
            return null;
        }
    }

    public Task<PassiveMember?> FindByIdAsync(int id)
    {
        var m = memberService.GetById(id);
        if (m is null || m.ContentType.Alias != MemberTypeAlias)
            return Task.FromResult<PassiveMember?>(null);
        return Task.FromResult<PassiveMember?>(Reconstitute(m));
    }

    public Task<IReadOnlyList<(FieldNumber Field, string? DisplayName)>> GetOccupiedFieldsAsync()
    {
        var result = new List<(FieldNumber, string?)>();
        foreach (var m in memberService.GetMembersByMemberType(MemberTypeAlias))
        {
            var status = UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>("status"), null);
            if (status == MemberStatus.Deleted.Key) continue;

            var raw = m.GetValue<string>("fieldNumber");
            if (!int.TryParse(raw, out var fn) || fn < 1 || fn > FloorGrid.TotalFields)
            {
                logger.LogWarning(
                    "Skipping passivMember {MemberId} with invalid fieldNumber '{FieldNumber}'.", m.Id, raw);
                continue;
            }

            var isPaid = m.GetValue<DateTime?>(PassivMemberAliases.PaidAt).HasValue;
            var show = m.GetValue<bool>(PassivMemberAliases.ShowNameOnFloor);
            var displayName = show && isPaid
                ? m.GetValue<string>(PassivMemberAliases.FloorDisplayName).NullIfEmpty()
                : null;
            result.Add((new FieldNumber(fn), displayName));
        }
        return Task.FromResult<IReadOnlyList<(FieldNumber, string?)>>(result);
    }

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
            logger.LogError(ex, "Failed to save PassiveMember for field {Field}", member.FieldNumber.Value);
            throw new DomainException($"Registrierung fehlgeschlagen: {ex.Message}");
        }
    }

    private async Task<PassiveMember> SaveInternalAsync(PassiveMember member)
    {
        var username = Username(member.FieldNumber.Value);

        var existingUser = await memberManager.FindByNameAsync(username);
        if (existingUser is not null)
        {
            var existing = memberService.GetById(int.Parse(existingUser.Id))
                ?? throw new DomainException("Existing member slot could not be found.");
            existing.Name = $"{member.FirstName} {member.LastName}".Trim();
            existing.Email = SyntheticEmail(member.FieldNumber.Value);
            SetProperties(existing, member);
            memberService.Save(existing);
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

        var result = await memberManager.CreateAsync(user);
        if (!result.Succeeded)
            throw new DomainException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var created = await memberManager.FindByNameAsync(username)
            ?? throw new DomainException("Member could not be found after creation.");

        var umbracoMember = memberService.GetById(int.Parse(created.Id))
            ?? throw new DomainException("Member could not be found after creation.");

        SetProperties(umbracoMember, member);
        memberService.Save(umbracoMember);

        return Reconstitute(umbracoMember);
    }

    public Task UpdateAsync(PassiveMember member)
    {
        var m = memberService.GetById(member.Id)
            ?? throw new MemberNotFoundException(member.Id);
        SetProperties(m, member);
        memberService.Save(m);
        return Task.CompletedTask;
    }

    private static string Username(int fieldNumber) => $"pm-{fieldNumber:D3}";
    private static string SyntheticEmail(int fieldNumber) => $"pm-{fieldNumber:D3}@passiv.internal";

    // Store single-select dropdown values in the same JSON-array format the back-office uses,
    // so that opening and re-saving a member in the Umbraco back-office doesn't clear the value.
    private static string DropdownJson(string key) => $"[\"{key}\"]";

    internal static void SetProperties(IMember m, PassiveMember pm)
    {
        m.SetValue(PassivMemberAliases.Email,                  pm.Email.Value);
        m.SetValue(PassivMemberAliases.FirstName,              pm.FirstName);
        m.SetValue(PassivMemberAliases.LastName,               pm.LastName);
        m.SetValue(PassivMemberAliases.FieldNumber,            pm.FieldNumber.Value.ToString());
        m.SetValue(PassivMemberAliases.MembershipLevel,        DropdownJson(pm.Level.Key));
        m.SetValue(PassivMemberAliases.Phone,                  pm.Phone ?? "");
        m.SetValue(PassivMemberAliases.ShowNameOnFloor,        pm.ShowNameOnFloor);
        m.SetValue(PassivMemberAliases.FloorDisplayName,       pm.DisplayName ?? "");
        m.SetValue(PassivMemberAliases.Status,                 DropdownJson(pm.Status.Key));
        m.SetValue(PassivMemberAliases.PaidAt,                 pm.PaidAt);
        m.SetValue(PassivMemberAliases.PaidBy,                 pm.PaidBy ?? "");
        m.SetValue(PassivMemberAliases.ConfirmedAt,            pm.ConfirmedAt);
        m.SetValue(PassivMemberAliases.ConfirmedBy,            pm.ConfirmedBy ?? "");
        m.SetValue(PassivMemberAliases.ExportedToAccountingAt, pm.ExportedToAccountingAt);
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
            phone:                  m.GetValue<string>(PassivMemberAliases.Phone).NullIfEmpty(),
            email:                  m.GetValue<string>(PassivMemberAliases.Email) ?? "",
            levelKey:               UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>(PassivMemberAliases.MembershipLevel), null),
            showNameOnFloor:        m.GetValue<bool>(PassivMemberAliases.ShowNameOnFloor),
            displayName:            m.GetValue<string>(PassivMemberAliases.FloorDisplayName).NullIfEmpty(),
            createdAt:              m.CreateDate,
            status:                 UmbracoDropdownHelper.ParseDropdownValue(m.GetValue<string>(PassivMemberAliases.Status), null) ?? MemberStatus.Pending.Key,
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
