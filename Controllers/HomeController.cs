using Microsoft.AspNetCore.Mvc;

namespace SHAW.Controllers;

[Route("home")]
public class HomeController : ControllerBase
{
    [HttpGet("secret")]
    public IActionResult GetSecretMessage()
    {
        return Ok("A very secret message");
    }
}
