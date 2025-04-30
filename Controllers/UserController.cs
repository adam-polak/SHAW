using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;
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
    public async Task Login()
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

        using(
            DataAccess.Controllers.UserController controller = new DataAccess.Controllers.UserController(
                DbConnectionFactory.CreateDbConnection(_env)
            )
        ) {
            string? loginKey;
            try { 
                loginKey = await controller.CorrectLogin(
                    new LoginModel() { Username = user.username, Password = user.password }
                );
            } catch(Exception e) {
                Console.WriteLine(e);
                await _sse.MergeSignalsAsync("{valid: 'fail'}");
                return;
            }

            await _sse.MergeSignalsAsync($"{{valid: '{loginKey ?? "invalid"}'}}");
        }
    }

    private class UserSignalModel 
    {
        public required string username { get; set; }
        public required string password { get; set; }
        public required string valid { get; set; }
    }
}
