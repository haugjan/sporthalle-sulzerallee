using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SporthalleWeb.Pages;

[Authorize]
public class PassivMemberAdminModel : PageModel
{
    public void OnGet() { }
}
