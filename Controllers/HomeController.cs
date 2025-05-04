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

    public HomeController(IDatastarServerSentEventService sse, IDatastarSignalsReaderService reader,
        IHostEnvironment env)
    {
        _sse = sse;
        _reader = reader;
        _env = env;
    }
    [HttpGet("")]
    public async Task<IActionResult> HomePage(string token)
    {
        // TODO: @Adam
        Func<string, bool> validate = delegate(string token)
        {
            if (token == "")
            {
                return false;
            }

            return true;
        };

        if (!validate(token))
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
