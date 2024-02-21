using Azure.Communication.CallAutomation;
using Azure.Communication.Rooms;
using Azure.Messaging;
using Web.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<AnswerCallWorker>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(new CallAutomationClient(builder.Configuration["Acs:ConnectionString"]));
builder.Services.AddSingleton(new RoomsClient(builder.Configuration["Acs:ConnectionString"]));

var callbackHost = $"{builder.Configuration["Acs:CallbackUri"] ?? builder.Configuration["VS_TUNNEL_URL"]} + /api/callbacks";
builder.Services.AddSingleton(new CallbackConfiguration()
{
    CallbackUri = new Uri(callbackHost),
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("api/callbacks", async (CloudEvent[] events, CallAutomationClient client) =>
{
    CallAutomationEventBase eventBase = CallAutomationEventParser.Parse(events.FirstOrDefault());

    if (eventBase is CallConnected callConnected)
    {
        // place outbound PSTN call
    }
});

app.MapPost("api/room", async (RoomsClient roomsClient) =>
{
    CommunicationRoom room = await roomsClient.CreateRoomAsync();
    return room;
});

app.MapPost("api/room/participants", async (string roomId, IEnumerable<RoomParticipant> participants, RoomsClient roomsClient) =>
{
    var response = await roomsClient.AddOrUpdateParticipantsAsync(roomId, participants);
    return response;
});

app.Run();
