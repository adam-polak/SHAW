using Microsoft.AspNetCore.Mvc;
using StarFederation.Datastar.DependencyInjection;
using EnvironmentName = Microsoft.AspNetCore.Hosting.EnvironmentName;
namespace SHAW.Controllers;

[Route("home")]
public class HomeController : ControllerBase
{
    
    private IDatastarServerSentEventService _sse;
    private IDatastarSignalsReaderService _reader;
    private IHostEnvironment _env;

    public HomeController(
        IHostEnvironment env)
    {
        _env = env;
    }
    [HttpGet("")]
    public async Task<IActionResult> HomePage(string token)
    {
        bool Validate(string token) => !string.IsNullOrEmpty(token);
        if (!Validate(token))
        {
            return Unauthorized("Bad token.");
        }
        const string fileName = "home.html";
        return PhysicalFile(
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", fileName),
            "text/html"
        );
    }
    
}
