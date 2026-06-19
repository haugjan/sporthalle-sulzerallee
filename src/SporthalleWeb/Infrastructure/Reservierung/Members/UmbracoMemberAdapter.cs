using Microsoft.AspNetCore.Identity;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using SporthalleWeb.Application.Reservierung;
using SporthalleWeb.Domain.Reservierung;
using SporthalleWeb.Domain.Reservierung.Ports;

namespace SporthalleWeb.Infrastructure.Reservierung.Members;

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
        var user = new MemberIdentityUser
        {
            UserName = cmd.Email,
            Email = cmd.Email,
            Name = cmd.ContactPerson,
            MemberTypeAlias = MemberTypeAlias,
            IsApproved = true
        };

        IdentityResult result = password is not null
            ? await memberManager.CreateAsync(user, password)
            : await memberManager.CreateAsync(user);

        if (!result.Succeeded)
            throw new DomainException(string.Join("; ", result.Errors.Select(e => e.Description)));

        // Set custom properties via IMemberService
        var member = memberService.GetByEmail(cmd.Email)
            ?? throw new DomainException("Member konnte nach der Erstellung nicht gefunden werden.");

        member.SetValue("renterType", cmd.RenterType.Value.ToString());
        member.SetValue("billingName", cmd.BillingName);
        member.SetValue("billingAddress", cmd.BillingAddress);
        member.SetValue("billingPostalCode", cmd.BillingPostalCode);
        member.SetValue("billingCity", cmd.BillingCity);
        member.SetValue("billingCountry", cmd.BillingCountry);
        member.SetValue("phone", cmd.Phone ?? "");
        member.SetValue("hasKey", cmd.HasKey);
        memberService.Save(member);

        // Reload to get fresh state
        var freshUser = await memberManager.FindByEmailAsync(cmd.Email)
            ?? throw new DomainException("Member konnte nach dem Speichern nicht geladen werden.");
        return Map(freshUser, member);
    }

    public Task UpdateProfileAsync(
        int memberId, string contactPerson, string billingName,
        string billingAddress, string billingPostalCode, string billingCity,
        string? phone, bool hasKey)
    {
        var member = memberService.GetById(memberId)
            ?? throw new DomainException($"Member {memberId} nicht gefunden.");

        member.Name = contactPerson;
        member.SetValue("billingName", billingName);
        member.SetValue("billingAddress", billingAddress);
        member.SetValue("billingPostalCode", billingPostalCode);
        member.SetValue("billingCity", billingCity);
        member.SetValue("phone", phone ?? "");
        member.SetValue("hasKey", hasKey);
        memberService.Save(member);
        return Task.CompletedTask;
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

    private static HallMember Map(MemberIdentityUser user, IMember member) =>
        new(
            Id: int.Parse(user.Id),
            Email: user.Email ?? "",
            ContactPerson: user.Name ?? "",
            RenterType: new RenterType(member.GetValue<string>("renterType") ?? "Privatperson"),
            BillingName: member.GetValue<string>("billingName") ?? "",
            BillingAddress: member.GetValue<string>("billingAddress") ?? "",
            BillingPostalCode: member.GetValue<string>("billingPostalCode") ?? "",
            BillingCity: member.GetValue<string>("billingCity") ?? "",
            BillingCountry: member.GetValue<string>("billingCountry") ?? "Schweiz",
            Phone: member.GetValue<string>("phone").NullIfEmpty(),
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
