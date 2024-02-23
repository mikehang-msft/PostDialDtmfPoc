using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Rooms;
using Azure.Messaging;
using JasonShave.AzureStorage.QueueService.Extensions;
using Microsoft.AspNetCore.Mvc;
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
    logger.LogInformation("Received callback {eventType}", eventBase.GetType());

    if (eventBase is CallConnected callConnected && eventBase.OperationContext == "inbound-call")
    {
        // place outbound PSTN call
        var target = new PhoneNumberIdentifier(callingConfiguration.Target);
        var callerId = new PhoneNumberIdentifier(callingConfiguration.CallerId);
        var callInvite = new CallInvite(target, callerId);

        logger.LogInformation("Adding participant {target}", target.PhoneNumber);

        await client.GetCallConnection(callConnected.CallConnectionId).AddParticipantAsync(callInvite);
    }

    // if (eventBase is AddParticipantSucceeded)
    // {
    //     // send DTMF tones to PSTN participant
    //     var target = new PhoneNumberIdentifier(callingConfiguration.Target);
    //     var tones = new List<DtmfTone>()
    //     {
    //         DtmfTone.One,
    //         DtmfTone.Two,
    //         DtmfTone.Three,
    //         DtmfTone.Four,
    //     };
    //     var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, target);

    //     logger.LogInformation("Sending DTMF tones to {target}", target.PhoneNumber);
        
    //     await client.GetCallConnection(eventBase.CallConnectionId).GetCallMedia().SendDtmfTonesAsync(sendDtmfTonesOptions);
    // }
});

app.MapPost("api/calls", async (CreateCallRequest request, CallAutomationClient client, CallingConfiguration callingConfiguration) =>
{
    CallInvite? callInvite = null;

    var target = CommunicationIdentifier.FromRawId(request.TargetIdentity);
    if (target is PhoneNumberIdentifier phoneNumber)
    {
        // need to set caller ID on PSTN scenario
        callInvite = new CallInvite(phoneNumber, new PhoneNumberIdentifier(callingConfiguration.CallerId));
    }
    
    if (target is CommunicationUserIdentifier userId)
    {
        callInvite = new CallInvite(userId);
    }

    if (target is MicrosoftTeamsUserIdentifier teamsUser)
    {
        callInvite = new CallInvite(teamsUser);
    }

    var createCallOptions = new CreateCallOptions(callInvite, callingConfiguration.CallbackUri)
    {
        OperationContext = "outbound-call"
    };
    var result = await client.CreateCallAsync(createCallOptions);

    return Results.Ok(result.Value);
});

app.MapPost("api/calls/{callConnectionId}/participant", async ([FromRoute] string callConnectionId, AddParticipantRequest request, CallAutomationClient client, CallingConfiguration callingConfiguration) =>
{
    CallInvite? callInvite = null;
    var target = CommunicationIdentifier.FromRawId(request.TargetIdentity);
    if (target is PhoneNumberIdentifier phoneNumber)
    {
        // need to set caller ID on PSTN scenario
        callInvite = new CallInvite(phoneNumber, new PhoneNumberIdentifier(callingConfiguration.CallerId));
    }
    
    if (target is CommunicationUserIdentifier userId)
    {
        callInvite = new CallInvite(userId);
    }

    await client.GetCallConnection(callConnectionId).AddParticipantAsync(callInvite);
});

app.MapPost("api/calls/{callConnectionId}/participant:sendDtmf", async ([FromRoute] string callConnectionId, SendDtmfTonesRequest request, CallAutomationClient client) =>
{
    var target = CommunicationIdentifier.FromRawId(request.TargetIdentity);
    var tones = new List<DtmfTone>();
    foreach (var item in request.DtmfTones)
    {
        tones.Add(item.ConvertToDtmfTone());
    }

    var sendDtmfTonesOptions = new SendDtmfTonesOptions(tones, target);
    await client.GetCallConnection(callConnectionId).GetCallMedia().SendDtmfTonesAsync(sendDtmfTonesOptions);
});

app.AddRoomsApiMappings();

app.Run();