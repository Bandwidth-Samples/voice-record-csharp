using System.Reflection;
using Bandwidth.Standard.Api;
using Bandwidth.Standard.Client;
using Bandwidth.Standard.Model;
using Bandwidth.Standard.Model.Bxml;
using Bandwidth.Standard.Model.Bxml.Verbs;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string BW_USERNAME;
string BW_PASSWORD;
string BW_ACCOUNT_ID;

//Setting up environment variables
try
{
    BW_USERNAME = Environment.GetEnvironmentVariable("BW_USERNAME");
    BW_PASSWORD = Environment.GetEnvironmentVariable("BW_PASSWORD");
    BW_ACCOUNT_ID = Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");
}
catch (Exception)
{
    Console.WriteLine("Please set the environmental variables defined in the README");
    throw;
}

Configuration configuration = new Configuration();
configuration.Username = BW_USERNAME;
configuration.Password = BW_PASSWORD;

app.MapPost("/callbacks/callInitiatedCallback", async (HttpContext context) =>
    {
        SpeakSentence unavailableSpeakSentence = new SpeakSentence()
        {
            Text = "You have reached Vandelay Industries, Kal Varnsen is unavailable at this time."
        };
        SpeakSentence messageSpeakSentence = new SpeakSentence()
        {
            Text = "At the tone, please record your message, when you have finished recording, you may hang up."
        };
        PlayAudio playAudio = new PlayAudio()
        {
            AudioUri = "Tone.mp3"
        };
        Record record = new Record()
        {
            RecordingAvailableUrl = "/callbacks/recordingAvailableCallback"
        };


        IVerb[] verbs = new IVerb[]
        {
            unavailableSpeakSentence,
            messageSpeakSentence,
            playAudio,
            record
        };
        Response response = new Response(verbs);
        string bxml = response.ToBXML();

        return bxml;
    }
);

app.MapGet("callbacks/Tone.mp3", async (HttpContext context) =>
{
    var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Tone.mp3");
    using var fileStream = new FileStream(path, FileMode.Open);
    await fileStream.CopyToAsync(context.Response.Body);
});

app.MapPost("/callbacks/recordingAvailableCallback", async (HttpContext context) =>
{
    var requestBody = new Dictionary<string, string>();
    using(var streamReader = new StreamReader(context.Request.Body))
    {
        var body = await streamReader.ReadToEndAsync();
        requestBody = JsonConvert.DeserializeObject<Dictionary<string,string>>(body);
    }

    var eventType = requestBody["eventType"];

    if (eventType == "recordingAvailable")
    {
        RecordingsApi recordingsApi = new RecordingsApi(configuration);

        var fileFormat = requestBody["fileFormat"];
        var callId = requestBody["callId"];
        var recordingId = requestBody["recordingId"];

        var recording = recordingsApi.DownloadCallRecording(BW_ACCOUNT_ID, callId, recordingId);
        using (var fileStream = File.Create("MyRecording." + fileFormat))
        {
            recording.CopyTo(fileStream);
        }
    }
});

app.Run();
