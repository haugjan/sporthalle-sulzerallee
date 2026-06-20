using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Pages;

[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public class PassivMemberAdminModel : PageModel
{
    public void OnGet() { }
}
