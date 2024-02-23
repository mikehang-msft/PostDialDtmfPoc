
using System.Text.Json;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using CallAutomation.Contracts;
using JasonShave.AzureStorage.QueueService.Interfaces;
using JasonShave.AzureStorage.QueueService.Services;

namespace Web.Api;

public class AnswerCallWorker : BackgroundService
{
    private readonly AzureStorageQueueClient _azureStorageQueueClient;
    private readonly CallAutomationClient _callAutomationClient;
    private readonly CallingConfiguration _callingConfiguration;
    private readonly ILogger<AnswerCallWorker> _logger;

    public AnswerCallWorker(
        IQueueClientFactory queueClientFactory,
        CallAutomationClient callAutomationClient,
        CallingConfiguration callingConfiguration,
        ILogger<AnswerCallWorker> logger)
    {
        _azureStorageQueueClient = queueClientFactory.GetQueueClient();
        _callAutomationClient = callAutomationClient;
        _callingConfiguration = callingConfiguration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting answer call worker...");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await _azureStorageQueueClient.ReceiveMessagesAsync<CloudEvent>(HandleMessage, HandleException);
            await Task.Delay(1000);
        }
    }

    private async ValueTask HandleMessage(CloudEvent? cloudEvent)
    {
        var incomingCall = JsonSerializer.Deserialize<IncomingCall>(cloudEvent?.Data);
        var answerCallOptions = new AnswerCallOptions(incomingCall?.IncomingCallContext, _callingConfiguration.CallbackUri)
        {
            OperationContext = "inbound-call",
            CallIntelligenceOptions = new CallIntelligenceOptions()
            {
                CognitiveServicesEndpoint = _callingConfiguration.CognitiveServicesUri
            }
        };
        
        _logger.LogInformation("Answering call with callback URI {callbackUri} and correlationID: {correlationId}", answerCallOptions.CallbackUri.AbsoluteUri, incomingCall?.CorrelationId);
        
        await _callAutomationClient.AnswerCallAsync(answerCallOptions);
    }

    private ValueTask HandleException(Exception exception)
    {
        return ValueTask.CompletedTask;
    }
}
