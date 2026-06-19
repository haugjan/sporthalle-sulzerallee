using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Umbraco.Cms.Web.Common.Authorization;

namespace SporthalleWeb.Pages;

[Authorize(Policy = AuthorizationPolicies.BackOfficeAccess)]
public class PassivMemberAdminModel : PageModel
{
    public void OnGet() { }
}
