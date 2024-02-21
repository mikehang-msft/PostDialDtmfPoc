
using Azure.Messaging;
using JasonShave.AzureStorage.QueueService.Interfaces;
using JasonShave.AzureStorage.QueueService.Services;

namespace Web.Api;

public class AnswerCallWorker : BackgroundService
{
    private readonly AzureStorageQueueClient _azureStorageQueueClient;

    public AnswerCallWorker(IQueueClientFactory queueClientFactory)
    {
        _azureStorageQueueClient = queueClientFactory.GetQueueClient();
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

    }

    private async ValueTask HandleException(Exception exception)
    {
        
    }
}
