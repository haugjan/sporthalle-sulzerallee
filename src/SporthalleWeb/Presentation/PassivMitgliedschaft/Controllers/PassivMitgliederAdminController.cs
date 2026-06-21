using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SporthalleWeb.Application.PassivMitgliedschaft;
using Umbraco.Cms.Core;

namespace SporthalleWeb.Presentation.PassivMitgliedschaft.Controllers;

[Route("passivmitglieder/admin")]
[Authorize(AuthenticationSchemes = Constants.Security.BackOfficeAuthenticationType)]
public sealed class PassivMitgliederAdminController : Controller
{
    private readonly AdminService _adminService;

    public PassivMitgliederAdminController(AdminService adminService)
        => _adminService = adminService;

    [HttpGet("")]
    public async Task<IActionResult> Index()
        => View(await _adminService.GetAllAsync());
}
