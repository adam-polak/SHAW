using Microsoft.AspNetCore.Mvc;
using SHAW.DataAccess.Util;

namespace SHAW.Controllers;

[Route("forum")]
public class ForumController : ControllerBase
{
    private IHostEnvironment _env;

    private DataAccess.Controllers.UserController CreateUserDbController() =>
        new DataAccess.Controllers.UserController(DbConnectionFactory.CreateDbConnection(_env));

    public ForumController(IHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("")]
    public async Task<IActionResult> ForumPage()
    {
        var cookieExists = Request.Cookies.TryGetValue("loginKey", out string? key);
        if (!cookieExists || string.IsNullOrEmpty(key))
        {
            return Redirect("index.html");
        }
        

        using (var c = CreateUserDbController())
        {
            var isValid = await c.ValidateLoginKey(key);
            return isValid switch
            {
                true => PhysicalFile(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "forum.html"),
                    "text/html"),
                false => BadRequest("Invalid login key"),
                null => BadRequest("Server error")
            };
        }
    }
}
