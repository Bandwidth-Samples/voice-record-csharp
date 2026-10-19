using Bandwidth.Standard.Api;
using Bandwidth.Standard.Client;
using Bandwidth.Standard.Model.Bxml;
using Bandwidth.Standard.Model.Bxml.Verbs;
using Newtonsoft.Json;

//Setting up environment variables
string? BW_ACCOUNT_ID = Environment.GetEnvironmentVariable("BW_ACCOUNT_ID");
string? BW_CLIENT_ID = Environment.GetEnvironmentVariable("BW_CLIENT_ID");
string? BW_CLIENT_SECRET = Environment.GetEnvironmentVariable("BW_CLIENT_SECRET");
string? LOCAL_PORT = Environment.GetEnvironmentVariable("LOCAL_PORT");

if (BW_ACCOUNT_ID == null || BW_CLIENT_ID == null || BW_CLIENT_SECRET == null || !int.TryParse(LOCAL_PORT, out int port))
{
    Console.WriteLine("Please set the environmental variables defined in the README");
    Environment.Exit(1);
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://localhost:{port}");
var app = builder.Build();

Configuration configuration = new Configuration();
configuration.OAuthClientId = BW_CLIENT_ID;
configuration.OAuthClientSecret = BW_CLIENT_SECRET;

async Task<Dictionary<string, object>?> ReadCallback(HttpContext context, string expectedEventType)
{
    try
    {
        using var streamReader = new StreamReader(context.Request.Body);
        var body = JsonConvert.DeserializeObject<Dictionary<string, object>>(await streamReader.ReadToEndAsync());
        if (body != null && body.TryGetValue("eventType", out var eventType) && eventType?.ToString() == expectedEventType)
        {
            return body;
        }
    }
    catch (JsonException)
    {
    }
    return null;
}

app.MapPost("/callbacks/inbound/voice", async (HttpContext context) =>
    {
        if (await ReadCallback(context, "initiate") == null)
        {
            return Results.BadRequest();
        }

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
            AudioUri = "/tone.mp3"
        };
        Record record = new Record()
        {
            RecordingAvailableUrl = "/callbacks/inbound/voice/recordingAvailable",
            MaxDuration = 30
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

        return Results.Content(bxml, "application/xml");
    }
);

app.MapGet("/tone.mp3", () => Results.File(Path.Combine(AppContext.BaseDirectory, "tone.mp3"), "audio/mpeg"));

app.MapPost("/callbacks/inbound/voice/recordingAvailable", async (HttpContext context) =>
{
    var requestBody = await ReadCallback(context, "recordingAvailable");
    if (requestBody == null)
    {
        return Results.BadRequest();
    }

    try
    {
        RecordingsApi recordingsApi = new RecordingsApi(configuration);

        var fileFormat = requestBody["fileFormat"];
        var callId = requestBody["callId"].ToString();
        var recordingId = requestBody["recordingId"];

        var recording = recordingsApi.DownloadCallRecording(BW_ACCOUNT_ID, callId, recordingId.ToString());
        using (var fileStream = File.Create($"{recordingId}.{fileFormat}"))
        {
            recording.CopyTo(fileStream);
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Failed to download recording: {e.Message}");
        return Results.StatusCode(500);
    }

    return Results.NoContent();
});

app.Run();
