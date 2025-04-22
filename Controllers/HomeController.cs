using Microsoft.AspNetCore.Mvc;
using StarFederation.Datastar.DependencyInjection;
using SHAW.DataAccess.Util;
using System.Data.Common;

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
        DbConnection conn = DbConnectionFactory.CreateDbConnection(_env);
        Console.WriteLine(conn.Database);
        await _sse.MergeSignalsAsync("{secret: 'hello world'}");
    }
}
