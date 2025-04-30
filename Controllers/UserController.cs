using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StarFederation.Datastar.DependencyInjection;

namespace SHAW.Controllers;

[Route("user")]
public class UserController : ControllerBase
{
    private IDatastarServerSentEventService _sse;
    private IDatastarSignalsReaderService _reader;
    private IHostEnvironment _env;

    public UserController(IDatastarServerSentEventService sse, IDatastarSignalsReaderService reader, IHostEnvironment env)
    {
        _sse = sse;
        _reader = reader;
        _env = env;
    }

    [HttpGet("login")]
    public async Task GetSecretMessage()
    {
        string signal = await _reader.ReadSignalsAsync();

        UserSignalModel? user;
        try {
            user = JsonConvert.DeserializeObject<UserSignalModel>(signal);

            if(user == null)
            {
                throw new Exception("Failed to convert signal to user");
            }
        } catch {
            await _sse.MergeSignalsAsync("{valid: 'fail'}");
            return;
        }

        // TODO verify user login
        await _sse.MergeSignalsAsync("{valid: 'true'}");
    }

    private class UserSignalModel 
    {
        public required string username { get; set; }
        public required string password { get; set; }
        public required string valid { get; set; }
    }
}
