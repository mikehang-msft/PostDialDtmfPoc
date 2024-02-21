
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
    private readonly CallbackConfiguration _callbackConfiguration;
    private readonly ILogger<AnswerCallWorker> _logger;

    public AnswerCallWorker(IQueueClientFactory queueClientFactory, CallAutomationClient callAutomationClient, CallbackConfiguration callbackConfiguration, ILogger<AnswerCallWorker> logger)
    {
        _azureStorageQueueClient = queueClientFactory.GetQueueClient();
        _callAutomationClient = callAutomationClient;
        _callbackConfiguration = callbackConfiguration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _azureStorageQueueClient.ReceiveMessagesAsync<CloudEvent>(HandleMessage, HandleException);
            await Task.Delay(1000);
        }
    }

    private async ValueTask HandleMessage(CloudEvent? cloudEvent)
    {
        var incomingCall = JsonSerializer.Deserialize<IncomingCall>(cloudEvent?.Data);
        var answerCallOptions = new AnswerCallOptions(incomingCall?.IncomingCallContext, _callbackConfiguration.CallbackUri);
        
        _logger.LogInformation("Answering call with callback URI {callbackUri}", answerCallOptions.CallbackUri.AbsoluteUri);
        
        await _callAutomationClient.AnswerCallAsync(answerCallOptions);
    }

    private ValueTask HandleException(Exception exception)
    {
        return ValueTask.CompletedTask;
    }
}
