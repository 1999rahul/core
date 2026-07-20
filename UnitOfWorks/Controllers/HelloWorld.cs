using Microsoft.AspNetCore.Mvc;

namespace UnitOfWorks.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HelloWorld : ControllerBase
    {

        [HttpGet(Name = "Hello")]
        public IActionResult Get()
        {
            return Ok("Hello World");
        }
    }
}
