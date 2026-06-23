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

        member.SetValue("renterType", cmd.RenterType.Value.ToString());
        member.SetValue("orgName",cmd.Name ?? "");
        member.SetValue("contactFirstName", cmd.ContactFirstName);
        member.SetValue("contactLastName", cmd.ContactLastName);
        member.SetValue("billingAddress", cmd.BillingAddress);
        member.SetValue("addressLine2", cmd.AddressLine2 ?? "");
        member.SetValue("billingPostalCode", cmd.BillingPostalCode);
        member.SetValue("billingCity", cmd.BillingCity);
        member.SetValue("billingCountry", cmd.BillingCountry);
        member.SetValue("phone", cmd.Phone ?? "");
        member.SetValue("hasKey", cmd.HasKey);
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
        member.SetValue("orgName",name ?? "");
        member.SetValue("contactFirstName", contactFirstName);
        member.SetValue("contactLastName", contactLastName);
        member.SetValue("billingAddress", billingAddress);
        member.SetValue("addressLine2", addressLine2 ?? "");
        member.SetValue("billingPostalCode", billingPostalCode);
        member.SetValue("billingCity", billingCity);
        member.SetValue("phone", phone ?? "");
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
            var firstName = member.GetValue<string>("contactFirstName") ?? "";
            var lastName  = member.GetValue<string>("contactLastName") ?? "";
            var orgName   = member.GetValue<string>("orgName") ?? "";
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
                    RenterType: new RenterType(member.GetValue<string>("renterType") ?? "Privatperson"),
                    Name: orgName.NullIfEmpty(),
                    ContactFirstName: firstName,
                    ContactLastName: lastName,
                    BillingAddress: member.GetValue<string>("billingAddress") ?? "",
                    AddressLine2: member.GetValue<string>("addressLine2").NullIfEmpty(),
                    BillingPostalCode: member.GetValue<string>("billingPostalCode") ?? "",
                    BillingCity: member.GetValue<string>("billingCity") ?? "",
                    BillingCountry: member.GetValue<string>("billingCountry") ?? "Schweiz",
                    Phone: member.GetValue<string>("phone").NullIfEmpty(),
                    Notes: member.GetValue<string>("notes").NullIfEmpty(),
                    HasKey: member.GetValue<bool>("hasKey"),
                    HasPassword: false,
                    MagicLinkSentAt: null,
                    PasswordResetSentAt: null));
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

    public Task<DateTime?> GetMagicLinkSentAtAsync(int memberId)
    {
        var member = memberService.GetById(memberId);
        var val = member?.GetValue<DateTime?>("magicLinkSentAt");
        return Task.FromResult(val.HasValue ? DateTime.SpecifyKind(val.Value, DateTimeKind.Utc) : (DateTime?)null);
    }

    public Task SetMagicLinkSentAtAsync(int memberId, DateTime sentAt)
    {
        var member = memberService.GetById(memberId)
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");
        member.SetValue("magicLinkSentAt", sentAt);
        memberService.Save(member);
        return Task.CompletedTask;
    }

    public Task<DateTime?> GetPasswordResetSentAtAsync(int memberId)
    {
        var member = memberService.GetById(memberId);
        var val = member?.GetValue<DateTime?>("passwordResetSentAt");
        return Task.FromResult(val.HasValue ? DateTime.SpecifyKind(val.Value, DateTimeKind.Utc) : (DateTime?)null);
    }

    public Task SetPasswordResetSentAtAsync(int memberId, DateTime sentAt)
    {
        var member = memberService.GetById(memberId)
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");
        member.SetValue("passwordResetSentAt", sentAt);
        memberService.Save(member);
        return Task.CompletedTask;
    }

    private static HallMember Map(MemberIdentityUser user, IMember member) => new(
        Id: int.Parse(user.Id),
        Email: user.Email ?? "",
        RenterType: new RenterType(member.GetValue<string>("renterType") ?? "Privatperson"),
        Name: member.GetValue<string>("orgName").NullIfEmpty(),
        ContactFirstName: member.GetValue<string>("contactFirstName") ?? "",
        ContactLastName: member.GetValue<string>("contactLastName") ?? "",
        BillingAddress: member.GetValue<string>("billingAddress") ?? "",
        AddressLine2: member.GetValue<string>("addressLine2").NullIfEmpty(),
        BillingPostalCode: member.GetValue<string>("billingPostalCode") ?? "",
        BillingCity: member.GetValue<string>("billingCity") ?? "",
        BillingCountry: member.GetValue<string>("billingCountry") ?? "Schweiz",
        Phone: member.GetValue<string>("phone").NullIfEmpty(),
        Notes: member.GetValue<string>("notes").NullIfEmpty(),
        HasKey: member.GetValue<bool>("hasKey"),
        HasPassword: user.PasswordHash is not null,
        MagicLinkSentAt: member.GetValue<DateTime?>("magicLinkSentAt"),
        PasswordResetSentAt: member.GetValue<DateTime?>("passwordResetSentAt")
    );
}

file static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
