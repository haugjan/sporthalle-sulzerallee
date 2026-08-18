using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Manifest;
using Umbraco.Extensions;

namespace SporthalleWeb.Infrastructure.Shared;

public sealed class BackOfficeAuthComposer : IComposer
{
    internal const string Scheme = "Umbraco.MicrosoftEntra";
    private const string EmailDomain = "@sporthalle-sulzerallee.ch";

    public void Compose(IUmbracoBuilder builder)
    {
        var tenantId = builder.Config["BackOfficeAuth:TenantId"];
        var clientId = builder.Config["BackOfficeAuth:ClientId"];
        var clientSecret = builder.Config["BackOfficeAuth:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(clientSecret))
        {
            return;
        }

        builder.Services.AddSingleton<IPackageManifestReader, MicrosoftEntraAuthProviderManifestReader>();

        builder.AddBackOfficeExternalLogins(logins =>
            logins.AddBackOfficeLogin(
                auth => auth.AddOpenIdConnect(Scheme, "Microsoft (@sporthalle-sulzerallee.ch)", options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                    options.ResponseType = "code";
                    options.UsePkce = true;
                    options.CallbackPath = "/umbraco-entra-signin";
                    options.SignedOutCallbackPath = "/umbraco-entra-signout";
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;
                    options.TokenValidationParameters.NameClaimType = "preferred_username";
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = ctx =>
                        {
                            ctx.ProtocolMessage.Prompt = "select_account";
                            return Task.CompletedTask;
                        }
                    };
                }),
                providerOptions =>
                {
                    providerOptions.DenyLocalLogin = true;
                    providerOptions.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
                        autoLinkExternalAccount: true,
                        defaultUserGroups: [],
                        allowManualLinking: false)
                    {
                        OnAutoLinking = (user, _) =>
                        {
                            if (!user.HasIdentity)
                                throw new InvalidOperationException(
                                    "No existing Umbraco user found for this address; auto-creation is disabled.");
                        },
                        OnExternalLogin = (_, loginInfo) => IsSporthalleAddress(loginInfo),
                    };
                }));
    }

    private static bool IsSporthalleAddress(ExternalLoginInfo loginInfo)
    {
        var identifier = loginInfo.Principal.FindFirstValue(ClaimTypes.Email)
                         ?? loginInfo.Principal.FindFirstValue("preferred_username")
                         ?? loginInfo.Principal.FindFirstValue("upn");
        return identifier is not null
               && identifier.Trim().EndsWith(EmailDomain, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class MicrosoftEntraAuthProviderManifestReader : IPackageManifestReader
{
    public Task<IEnumerable<PackageManifest>> ReadPackageManifestsAsync()
    {
        var manifest = new PackageManifest
        {
            Name = "Sporthalle.BackOfficeAuth",
            AllowPublicAccess = true,
            Extensions =
            [
                new
                {
                    type = "authProvider",
                    alias = "Sporthalle.AuthProviders.MicrosoftEntra",
                    name = "Microsoft Entra login provider",
                    forProviderName = BackOfficeAuthComposer.Scheme,
                    meta = new
                    {
                        label = "Microsoft",
                        defaultView = new
                        {
                            icon = "icon-cloud",
                            look = "primary",
                            color = "default"
                        },
                        linking = new
                        {
                            allowManualLinking = false
                        }
                    }
                }
            ]
        };

        return Task.FromResult<IEnumerable<PackageManifest>>(new[] { manifest });
    }
}
