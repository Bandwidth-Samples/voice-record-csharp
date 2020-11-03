using Bandwidth.Standard.Voice.Bxml;
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

            var unavailableSpeakSentence = new SpeakSentence
            {
                Sentence = "Vandelay Industries, Kal Varnsen is unavailable at this time."
            };

            var messageSpeakSentence = new SpeakSentence
            {
                Sentence = "At the tone, please record your message, when you have finished recording, you may hang up."
            };

            var playAudio = new PlayAudio
            {
                Url = "/files/tone"
            };

            var record = new Record
            {
                RecordCompleteUrl = "/callbacks/recordCompleteCallback",
                RecordingAvailableUrl = "/callbacks/recordingAvailableCallback"
            };

            var response = new Response(
                unavailableSpeakSentence,
                messageSpeakSentence,
                playAudio,
                record
            );

            return new OkObjectResult(response.ToBXML());
        }

        [HttpPost("callAnsweredCallback")]
        public ActionResult CallAnsweredCallback()
        {
            _logger.LogInformation($"{Request.Path.Value} requested.");

            return new OkResult();
        }

        [HttpPost("recordCompleteCallback")]
        public ActionResult RecordCompleteCallback()
        {
            _logger.LogInformation($"{Request.Path.Value} requested.");

            return new OkResult();
        }

        [HttpPost("recordingAvailableCallback")]
        public ActionResult RecordingAvailableCallback()
        {
            _logger.LogInformation($"{Request.Path.Value} requested.");

            return new OkResult();
        }
    }
}
