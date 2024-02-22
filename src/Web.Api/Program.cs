using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Rooms;
using Azure.Messaging;
using JasonShave.AzureStorage.QueueService.Extensions;
using Web.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<AnswerCallWorker>();
builder.Services.AddSingleton(new CallAutomationClient(builder.Configuration["Acs:ConnectionString"]));
builder.Services.AddSingleton(new RoomsClient(builder.Configuration["Acs:ConnectionString"]));

var callbackHost = $"{builder.Configuration["Acs:CallbackUri"] ?? builder.Configuration["VS_TUNNEL_URL"]}" + "/api/callbacks";
builder.Services.AddSingleton(new CallingConfiguration()
{
    CallbackUri = new Uri(callbackHost),
    Target = builder.Configuration["PhoneNumbers:Target"],
    CallerId = builder.Configuration["PhoneNumbers:CallerId"],
});

builder.Services.AddAzureStorageQueueClient(x => x.AddDefaultClient(y => 
{
    y.ConnectionString = builder.Configuration["Storage:ConnectionString"];
    y.QueueName = "events-incomingcall";
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("api/callbacks", async (CloudEvent[] events, CallAutomationClient client, CallingConfiguration callingConfiguration, ILogger<Program> logger) =>
{
    CallAutomationEventBase eventBase = CallAutomationEventParser.Parse(events.FirstOrDefault());

    if (eventBase is CallConnected)
    {
        // place outbound PSTN call
        var target = new PhoneNumberIdentifier(callingConfiguration.Target);
        var callerId = new PhoneNumberIdentifier(callingConfiguration.CallerId);
        var callInvite = new CallInvite(target, callerId);

        logger.LogInformation("Adding participant {target}", target.PhoneNumber);

        await client.GetCallConnection(eventBase.CallConnectionId).AddParticipantAsync(callInvite);
    }

    if (eventBase is AddParticipantSucceeded)
    {
        // send DTMF tones to PSTN participant
        var target = new PhoneNumberIdentifier(callingConfiguration.Target);
        var tones = new List<DtmfTone>()
        {
            DtmfTone.One,
            DtmfTone.Two,
            DtmfTone.Three,
            DtmfTone.Four,
        };
        var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, target);

        logger.LogInformation("Sending DTMF tones to {target}", target.PhoneNumber);
        
        await client.GetCallConnection(eventBase.CallConnectionId).GetCallMedia().SendDtmfTonesAsync(sendDtmfTonesOptions);
    }
});

app.AddRoomsApiMappings();

app.Run();