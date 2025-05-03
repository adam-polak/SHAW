using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;
using StarFederation.Datastar.DependencyInjection;
using EnvironmentName = Microsoft.AspNetCore.Hosting.EnvironmentName;

namespace SHAW.Controllers;

[Route("user")]
public class UserController : ControllerBase
{
    private IDatastarServerSentEventService _sse;
    private IDatastarSignalsReaderService _reader;
    private IHostEnvironment _env;

    public UserController(IDatastarServerSentEventService sse, IDatastarSignalsReaderService reader,
        IHostEnvironment env)
    {
        _sse = sse;
        _reader = reader;
        _env = env;
    }

    [HttpGet("login")]
    public async Task LoginPage()
    {
        var model = new LoginSignalModel
        {
            username = "",
            password = "",
            valid = ""
        };
        await _sse.MergeFragmentsAsync(Templates.loginTemplate(model));
    }

    [HttpGet("register")]
    public async Task RegisterPage()
    {
        var model = new RegisterSignalModel
        {
            r_account_type = "",
            r_username = "",
            r_password = "",
            r_email = "",
            r_error = ""
        };
        await _sse.MergeFragmentsAsync(Templates.registerTemplate(model));
    }

    [HttpPost("login")]
    public async Task Login()
    {
        string signal = await _reader.ReadSignalsAsync();

        LoginSignalModel? user;
        try
        {
            user = JsonConvert.DeserializeObject<LoginSignalModel>(signal);
            if (user == null)
            {
                throw new Exception("Failed to convert signal to user");
            }
        }
        catch
        {
            await _sse.MergeSignalsAsync("{valid: 'fail'}");
            return;
        }

        using (
            DataAccess.Controllers.UserController controller = new DataAccess.Controllers.UserController(
                DbConnectionFactory.CreateDbConnection(_env)
            )
        )
        {
            string? loginKey;
            try
            {
                loginKey = await controller.CorrectLogin(
                    new LoginModel() { Username = user.username, Password = user.password }
                );
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                await _sse.MergeSignalsAsync("{valid: 'fail'}");
                return;
            }

            await _sse.MergeSignalsAsync($"{{valid: '{loginKey ?? "invalid"}'}}");
        }
    }

    public class LoginSignalModel
    {
        public required string username { get; set; }
        public required string password { get; set; }
        public required string valid { get; set; }
    }

    public class RegisterSignalModel
    {
        public required string r_account_type { get; set; }
        public required string r_username { get; set; }
        public required string r_password { get; set; }
        public required string r_email { get; set; }
        public required string r_error { get; set; }
    }

    public static class Templates
    {
        public static string loginTemplate(LoginSignalModel login)
        {
            return $@"
    <main id=""morph"">
    <div
      data-signals=""{{username: '{login.username}', password: '{login.password}', valid: '{login.valid}'}}""
      data-computed-error=""$valid == 'invalid' ? 'Invalid username or password' :
                           $valid == 'fail' ? 'Server error' : ''
                          "">
      
      <h4 class=""text-center mb-4"">Sign in</h4>
      <div class=""mb-3"">
        <label for=""username"" class=""form-label"">Username</label>
        <input type=""text"" data-bind=""username"" class=""form-control"" id=""username"" placeholder=""Enter username"" required>
      </div>

      <div class=""mb-3"">
        <label for=""password"" class=""form-label"">Password</label>
        <input type=""password"" data-bind=""password"" class=""form-control"" id=""password"" placeholder=""Enter password"" required>
      </div>

      <label id=""error-message"" class=""text-danger text-center"" data-text=""$error"" data-show=""$valid != ''""></label>
      <div class=""d-grid"">
        <button data-on-click=""@post('/user/login')""
                data-on-keydown__window=""evt.key == 'Enter' && @post('/user/login')""
                class=""btn btn-primary"">Login</button>
      </div>

      <div class=""text-center mt-3"">
        <a data-on-click=""@get('/user/register')"" class=""text-decoration-none"">Don't have an account yet?</a>
      </div>
    </div>
    </main>
           ";
        }

        public static string registerTemplate(RegisterSignalModel register)
        {
            return $@"
        <main id=""morph"">
        <div data-signals=""{{r_account_type: '{register.r_account_type}', r_username: '{register.r_username}', r_password: '{register.r_password}', r_email: '{register.r_email}', r_error: '{register.r_error}'}}"">
            <h4 class=""text-center mb-4"">Register</h4>
            <label class=""text-center mb-2"">Account Type</label>
            <div class=""btn-group d-flex"" role=""group"">
                <button class=""btn btn-outline-dark""
                        data-class-active=""$r_account_type == 'student'"" data-on-click=""$r_account_type = 'student';""
                >Student
                </button>
                <button class=""btn btn-outline-dark""
                        data-class-active=""$r_account_type == 'counselor'"" data-on-click=""$r_account_type = 'counselor';""
                >Counselor
                </button>
            </div>
            <br>
       
            <div class=""mb-3"">
                <label for=""username"" class=""form-label"">Username</label>
                <input type=""text"" data-bind=""r_username"" class=""form-control"" id=""username"" placeholder=""Enter username""
                       required>
            </div>

            <div class=""mb-3"">
                <label for=""password"" class=""form-label"">Password</label>
                <input type=""password"" data-bind=""r_password"" class=""form-control"" id=""password""
                       placeholder=""Enter password"" required>
            </div>
            <div class=""mb-3"">
                <label for=""email"" class=""form-label"">Email Address</label>
                <input type=""email"" data-bind=""r_email"" class=""form-control"" id=""email"" placeholder=""Enter email""
                       required>
            </div>
            <label id=""error-message""
                   class=""text-danger text-center""
                   data-text=""$r_error""
                   data-show=""$r_error != ''"">
            </label>
            <div class=""d-grid"">
                <button data-on-click=""@post('/user/register')""
                        data-on-keydown__window=""evt.key == 'Enter' && @post('/user/register')""
                        class=""btn btn-primary"">Sign up
                </button>
            </div>

            <div class=""text-center mt-3"">
                <a data-on-click=""@get('/user/login')"" class=""text-decoration-none"">Already have an account?</a>
        </div></div></main>";
        }
    }

    public async Task MorphSuccessAndRedirect(string url, int timeout = 1000)
    {
        // Render Big Checkmark and "Redirecting..."
        await _sse.MergeFragmentsAsync($@"
            <main id='morph'>
               <div class='display-1 text-center text-success mb-3'>
                    <i class='bi bi-check-circle-fill'></i>
                </div> 
                <h4 class='text-center mb-4'>Redirecting...</h4>
            </main>
        ");
        await Task.Delay(timeout);
        await _sse.ExecuteScriptAsync($"setTimeout(() => window.location.href = '{url}')");
    }
}