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
}

internal static class Templates 
{
    internal static string ViewPostTemplate(PostSignalModel post)
    {
        return $@"
    <main id=""morph"">
        <div class=""container my-5"">
            <div class=""row"">
                <div class=""col-md-8"">
                    <nav aria-label=""breadcrumb"">
                        <ol class=""breadcrumb"">
                            <li class=""breadcrumb-item""><a href=""#"" data-on-click=""@get('/forum')"">Forum</a></li>
                            <li class=""breadcrumb-item active"" aria-current=""page"">{post.title}</li>
                        </ol>
                    </nav>
                    <div class=""card"">
                        <div class=""card-body"">
                            <h1 class=""card-title"">{post.title}</h1>
                            <div class=""text-muted mb-3"">
                                Posted by {post.author} on {post.date}
                            </div>
                            <p class=""card-text"">{post.body}</p>
                        </div>
                    </div>
                </div>
                <div class=""col-md-4"">
                    <div class=""card"">
                        <div class=""card-body"">
                            <h5 class=""card-title"">Discussion Guidelines</h5>
                            <ul class=""list-unstyled"">
                                <li class=""mb-2"">✓ Be respectful and supportive</li>
                                <li class=""mb-2"">✓ Stay on topic</li>
                                <li class=""mb-2"">✓ Share constructive feedback</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </main>";
    }
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