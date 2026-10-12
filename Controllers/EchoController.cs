using Microsoft.AspNetCore.Mvc;

namespace Notificator.Controllers;

[ApiController]
[Route("api/echo")]
public class EchoController : ControllerBase
{
    // Ничего сложного, просто ответ на запрос с параметром
    [HttpGet]
    public IActionResult Echo([FromQuery]string echo)
    {
        return Ok(new { status = "ok", message = echo} );
    }
}