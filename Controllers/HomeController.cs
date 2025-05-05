using Microsoft.AspNetCore.Mvc;
using StarFederation.Datastar.DependencyInjection;
using EnvironmentName = Microsoft.AspNetCore.Hosting.EnvironmentName;
using SHAW.Controllers.Util;
using SHAW.DataAccess.Controllers;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;
namespace SHAW.Controllers;

[Route("home")]
public class HomeController : ControllerBase
{
    private IHostEnvironment _env;

    private DataAccess.Controllers.UserController CreateUserDbController() =>
        new DataAccess.Controllers.UserController(DbConnectionFactory.CreateDbConnection(_env));

    public HomeController(IHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("")]
    public async Task<IActionResult> HomePage(string key)
    {
        if (!string.IsNullOrEmpty(key)) return BadRequest("Key is required for this page");
        using (var c = CreateUserDbController())
        {
            bool isValid = await c.ValidateLoginKey(key);
            if (!isValid)
            {
                return Unauthorized("Invalid Token.");
            }
            return PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "home.html"),
                "text/html"
            );
        }
    }
}
