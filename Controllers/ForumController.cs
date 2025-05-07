using System.Data.Common;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using SHAW.Controllers.Util;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;

namespace SHAW.Controllers;

public class PostSignalModel
{
    public required int id { get; set; }
    public required string title { get; set; }
    public required string author { get; set; }
    public required string date { get; set; }
    public required string body { get; set; }
}

public class PostViewSignalModel
{
    public required List<PostSignalModel> posts { get; set; }
    public required PostSignalModel selected { get; set; }
    
    public required bool isCounselor { get; set; }
}


[Route("forum")]
public class ForumController : ControllerBase
{
    private IHostEnvironment _env;

    public ForumController(IHostEnvironment env)
    {
        _env = env;
    }
    
    private DataAccess.Controllers.UserController CreateUserDbController()
    {
        return new DataAccess.Controllers.UserController(
            DbConnectionFactory.CreateDbConnection(_env)
        );
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