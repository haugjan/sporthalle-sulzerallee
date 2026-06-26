using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using SporthalleWeb.Application.Booking;
using SporthalleWeb.Domain.Booking;
using SporthalleWeb.Domain.Booking.Ports;

namespace SporthalleWeb.Infrastructure.Booking.Members;

public sealed class UmbracoMemberAdapter(
    IMemberManager memberManager,
    SignInManager<MemberIdentityUser> signInManager,
    IMemberService memberService) : IMemberManagerPort
{
    private const string MemberTypeAlias = "hallMember";

    public async Task<HallMember?> FindByEmailAsync(string email)
    {
        var user = await memberManager.FindByEmailAsync(email);
        if (user is null) return null;
        var member = memberService.GetByEmail(email);
        if (member is null) return null;
        return Map(user, member);
    }

    public async Task<HallMember?> FindByIdAsync(int memberId)
    {
        var user = await memberManager.FindByIdAsync(memberId.ToString());
        if (user is null) return null;
        var member = memberService.GetById(memberId);
        if (member is null) return null;
        return Map(user, member);
    }

    public async Task<HallMember> CreateAsync(RegisterRenterCommand cmd, string? password)
    {
        var displayName = $"{cmd.ContactFirstName} {cmd.ContactLastName}".Trim();

        var user = new MemberIdentityUser
        {
            UserName = cmd.Email,
            Email = cmd.Email,
            Name = displayName,
            MemberTypeAlias = MemberTypeAlias,
            IsApproved = true
        };

        IdentityResult result = password is not null
            ? await memberManager.CreateAsync(user, password)
            : await memberManager.CreateAsync(user);

        if (!result.Succeeded)
            throw new DomainException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var member = memberService.GetByEmail(cmd.Email)
            ?? throw new DomainException("Member konnte nach der Erstellung nicht gefunden werden.");

        SetMemberProperties(member, cmd);
        memberService.Save(member);

        var freshUser = await memberManager.FindByEmailAsync(cmd.Email)
            ?? throw new DomainException("Member konnte nach dem Speichern nicht geladen werden.");
        return Map(freshUser, member);
    }

    public Task UpdateProfileAsync(
        int memberId, string? name,
        string contactFirstName, string contactLastName,
        string billingAddress, string? addressLine2,
        string billingPostalCode, string billingCity, string? phone)
    {
        var member = memberService.GetById(memberId)
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");

        member.Name = $"{contactFirstName} {contactLastName}".Trim();
        member.SetValue(HallMemberAliases.OrgName,           name ?? "");
        member.SetValue(HallMemberAliases.ContactFirstName,  contactFirstName);
        member.SetValue(HallMemberAliases.ContactLastName,   contactLastName);
        member.SetValue(HallMemberAliases.BillingAddress,    billingAddress);
        member.SetValue(HallMemberAliases.AddressLine2,      addressLine2 ?? "");
        member.SetValue(HallMemberAliases.BillingPostalCode, billingPostalCode);
        member.SetValue(HallMemberAliases.BillingCity,       billingCity);
        member.SetValue(HallMemberAliases.Phone,             phone ?? "");
        memberService.Save(member);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<HallMember>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Task.FromResult<IReadOnlyList<HallMember>>([]);

        var members = memberService.GetMembersByMemberType(MemberTypeAlias);
        var results = new List<HallMember>();

        foreach (var member in members)
        {
            var firstName = member.GetValue<string>(HallMemberAliases.ContactFirstName) ?? "";
            var lastName  = member.GetValue<string>(HallMemberAliases.ContactLastName) ?? "";
            var orgName   = member.GetValue<string>(HallMemberAliases.OrgName) ?? "";
            var email     = member.Email ?? "";
            var fullName  = $"{firstName} {lastName}".Trim();

            if (firstName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                lastName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                orgName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                email.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                fullName.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new HallMember(
                    Id: member.Id,
                    Email: email,
                    RenterType: new RenterType(GetDropdownValue(member, HallMemberAliases.RenterType, "Privatperson")),
                    Name: orgName.NullIfEmpty(),
                    ContactFirstName: firstName,
                    ContactLastName: lastName,
                    BillingAddress: member.GetValue<string>(HallMemberAliases.BillingAddress) ?? "",
                    AddressLine2: member.GetValue<string>(HallMemberAliases.AddressLine2).NullIfEmpty(),
                    BillingPostalCode: member.GetValue<string>(HallMemberAliases.BillingPostalCode) ?? "",
                    BillingCity: member.GetValue<string>(HallMemberAliases.BillingCity) ?? "",
                    BillingCountry: member.GetValue<string>(HallMemberAliases.BillingCountry) ?? "Schweiz",
                    Phone: member.GetValue<string>(HallMemberAliases.Phone).NullIfEmpty(),
                    Notes: member.GetValue<string>(HallMemberAliases.Notes).NullIfEmpty(),
                    HasKey: member.GetValue<bool>(HallMemberAliases.HasKey),
                    HasPassword: false));
            }
        }

        return Task.FromResult<IReadOnlyList<HallMember>>(results.Take(10).ToList());
    }

    public async Task<bool> CheckPasswordAsync(string email, string password)
    {
        var user = await memberManager.FindByEmailAsync(email);
        if (user is null) return false;
        return await memberManager.CheckPasswordAsync(user, password);
    }

    public async Task SignInAsync(int memberId)
    {
        var user = await memberManager.FindByIdAsync(memberId.ToString())
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");
        await signInManager.SignInAsync(user, isPersistent: true);
    }

    public Task SignOutAsync() => signInManager.SignOutAsync();

    public async Task AddOrChangePasswordAsync(int memberId, string newPassword)
    {
        var user = await memberManager.FindByIdAsync(memberId.ToString())
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");
        var hasPassword = user.PasswordHash is not null;
        IdentityResult result;
        if (hasPassword)
        {
            var token = await memberManager.GeneratePasswordResetTokenAsync(user);
            result = await memberManager.ResetPasswordAsync(user, token, newPassword);
        }
        else
        {
            result = await memberManager.AddPasswordAsync(user, newPassword);
        }
        if (!result.Succeeded)
            throw new DomainException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<string> GeneratePasswordResetTokenAsync(int memberId)
    {
        var user = await memberManager.FindByIdAsync(memberId.ToString())
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");
        return await memberManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task ResetPasswordAsync(int memberId, string token, string newPassword)
    {
        var user = await memberManager.FindByIdAsync(memberId.ToString())
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");
        var result = await memberManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            throw new DomainException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    // internal for MemberTypeConsistencyTests
    internal static void SetMemberProperties(IMember member, RegisterRenterCommand cmd)
    {
        member.SetValue(HallMemberAliases.RenterType,        cmd.RenterType.Value.ToString());
        member.SetValue(HallMemberAliases.OrgName,           cmd.Name ?? "");
        member.SetValue(HallMemberAliases.ContactFirstName,  cmd.ContactFirstName);
        member.SetValue(HallMemberAliases.ContactLastName,   cmd.ContactLastName);
        member.SetValue(HallMemberAliases.BillingAddress,    cmd.BillingAddress);
        member.SetValue(HallMemberAliases.AddressLine2,      cmd.AddressLine2 ?? "");
        member.SetValue(HallMemberAliases.BillingPostalCode, cmd.BillingPostalCode);
        member.SetValue(HallMemberAliases.BillingCity,       cmd.BillingCity);
        member.SetValue(HallMemberAliases.BillingCountry,    cmd.BillingCountry);
        member.SetValue(HallMemberAliases.Phone,             cmd.Phone ?? "");
        member.SetValue(HallMemberAliases.HasKey,            cmd.HasKey);
    }

    private static string GetDropdownValue(IMember member, string alias, string fallback) =>
        UmbracoDropdownHelper.ParseDropdownValue(member.GetValue<string>(alias), fallback);

    private static HallMember Map(MemberIdentityUser user, IMember member) => new(
        Id: int.Parse(user.Id),
        Email: user.Email ?? "",
        RenterType: new RenterType(GetDropdownValue(member, HallMemberAliases.RenterType, "Privatperson")),
        Name: member.GetValue<string>(HallMemberAliases.OrgName).NullIfEmpty(),
        ContactFirstName: member.GetValue<string>(HallMemberAliases.ContactFirstName) ?? "",
        ContactLastName: member.GetValue<string>(HallMemberAliases.ContactLastName) ?? "",
        BillingAddress: member.GetValue<string>(HallMemberAliases.BillingAddress) ?? "",
        AddressLine2: member.GetValue<string>(HallMemberAliases.AddressLine2).NullIfEmpty(),
        BillingPostalCode: member.GetValue<string>(HallMemberAliases.BillingPostalCode) ?? "",
        BillingCity: member.GetValue<string>(HallMemberAliases.BillingCity) ?? "",
        BillingCountry: member.GetValue<string>(HallMemberAliases.BillingCountry) ?? "Schweiz",
        Phone: member.GetValue<string>(HallMemberAliases.Phone).NullIfEmpty(),
        Notes: member.GetValue<string>(HallMemberAliases.Notes).NullIfEmpty(),
        HasKey: member.GetValue<bool>(HallMemberAliases.HasKey),
        HasPassword: user.PasswordHash is not null
    );
}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
