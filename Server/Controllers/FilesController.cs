using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly ILogger<FilesController> _logger;

        public FilesController(ILogger<FilesController> logger)
        {
            _logger = logger;
        }

        [HttpGet("tone")]
        public ActionResult Tone()
        {
            _logger.LogInformation($"{Request.Path.Value} requested.");

            var fileStream = new FileStream("Tone.mp3", FileMode.Open);

            return new FileStreamResult(fileStream, new MediaTypeHeaderValue("audio/mp3"));
        }
    }
}