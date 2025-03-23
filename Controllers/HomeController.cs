using Microsoft.AspNetCore.Mvc;
using StarFederation.Datastar.DependencyInjection;

namespace SHAW.Controllers;

[Route("home")]
public class HomeController : ControllerBase
{
    private IDatastarServerSentEventService _sse;

    public HomeController(IDatastarServerSentEventService sse)
    {
        _sse = sse;
    }

    [HttpGet("secret")]
    public async Task GetSecretMessage()
    {
        await _sse.MergeSignalsAsync("{secret: 'hello world'}");
    }
}
