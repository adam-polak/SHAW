using Microsoft.AspNetCore.Mvc;
using SHAW.Controllers.Util;
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
    public async Task<IActionResult> HomePage()
    {
        if (!RequestUtil.TryGetLoginKey(Request, out string key))
        {
            return Redirect("index.html");
        }
        

        using (var c = CreateUserDbController())
        {
            var isValid = await c.ValidateLoginKey(key);
            return isValid switch
            {
                true => PhysicalFile(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "home.html"),
                    "text/html"),
                false => BadRequest("Invalid login key"),
                null => BadRequest("Server error")
            };
        }
    }
}