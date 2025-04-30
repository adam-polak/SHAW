using Microsoft.AspNetCore.Mvc;
using StarFederation.Datastar.DependencyInjection;

namespace SHAW.Controllers;

[Route("home")]
public class HomeController : ControllerBase
{
    private IDatastarServerSentEventService _sse;
    private IHostEnvironment _env;

    public HomeController(IDatastarServerSentEventService sse, IHostEnvironment env)
    {
        _sse = sse;
        _env = env;
    }

    [HttpGet("secret")]
    public async Task GetSecretMessage()
    {
        await _sse.MergeSignalsAsync("{secret: 'hello world'}");
    }
}
