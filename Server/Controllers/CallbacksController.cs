using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CallbacksController : ControllerBase
    {
        private readonly ILogger<CallbacksController> _logger;

        public CallbacksController(ILogger<CallbacksController> logger)
        {
            _logger = logger;
        }

        [HttpPost("callInitiatedCallback")]
        public ActionResult CallInitiatedCallback()
        {
            _logger.LogInformation($"{Request.Path.Value} requested.");

            return new OkResult();
        }
    }
}
