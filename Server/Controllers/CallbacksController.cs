using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bandwidth.Standard;
using Bandwidth.Standard.Voice.Bxml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CallbacksController : ControllerBase
    {
        private readonly ILogger<CallbacksController> _logger;

        // Bandwidth provided username. This is used to download voice recordings.
        private static readonly string Username = System.Environment.GetEnvironmentVariable("BANDWIDTH_API_USER");

        // Bandwidth provided password. This is used to download voice recordings.
        private static readonly string Password = System.Environment.GetEnvironmentVariable("BANDWIDTH_API_PASSWORD");

        // Bandwidth provided account id. This is used to download voice recordings.
        private static readonly string AccountId = System.Environment.GetEnvironmentVariable("BANDWIDTH_ACCOUNT_ID");

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
                Sentence = "You have reached Vandelay Industries, Kal Varnsen is unavailable at this time."
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
        public async Task<ActionResult> RecordingAvailableCallback()
        {
            _logger.LogInformation($"{Request.Path.Value} requested.");

            // Read in the json body.
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var json = JObject.Parse(body);

            // Grab the event type from the json response.
            var eventType = (string)json["eventType"];

            // We're only interested if the event type is "recordingAvailable".
            if (eventType == "recordingAvailable")
            {
                var client = new BandwidthClient
                    .Builder()
                    .Environment(Bandwidth.Standard.Environment.Production)
                    .VoiceBasicAuthCredentials(Username, Password)
                    .Build();

                // We're using the file format to create the local file's extension.
                var fileFormat = (string)json["fileFormat"];

                // Create a local path for the recording file to be saved.
                var path = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    Path.ChangeExtension("MyRecording", fileFormat)
                );
                using var fileStream = new FileStream(path, FileMode.Create);
                
                var callId = (string)json["callId"];
                var recordingId = (string)json["recordingId"];

                // Get the recording's stream and copy it to the local file's stream.
                var response = await client.Voice.APIController.GetStreamRecordingMediaAsync(AccountId, callId, recordingId);
                await response.Data.CopyToAsync(fileStream);
            }

            return new OkResult();
        }
    }
}
