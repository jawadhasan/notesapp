using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NotesApp.Worker.Settings;

namespace NotesApp.Worker.Services
{
    public class SQSEventConsumer : IEventConsumer
    {
        private readonly ILogger<SQSEventConsumer> _logger;
        private readonly AmazonSQSClient _sqsClient;
        private readonly SQSSettings _settings;
        private bool _running = false;

        //ctor
        public SQSEventConsumer(IOptions<SQSSettings> settings, 
            ILogger<SQSEventConsumer> logger)
        {
            _logger = logger;

            try
            {
                AmazonSQSConfig amazonSQSConfig = new AmazonSQSConfig();

                amazonSQSConfig.RegionEndpoint = RegionEndpoint
                    .GetBySystemName(settings.Value.AWSRegion);

                amazonSQSConfig.Validate();

                _sqsClient = new AmazonSQSClient(amazonSQSConfig);
                _settings = settings.Value;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Couldn't create an instance of SQSEventConsumer");
                throw;
            }
        }

        public void Start()
        {
            _logger.LogInformation("Starting event consumer");
            _running = true;

            _logger.LogInformation($"Collecting messages from queue '{_settings.QueueUrl}'");

            Task.Run(async () =>
            {
                while (_running)
                {
                    _logger.LogInformation("Polling for messages");

                    // Receive messages
                    try
                    {
                        var receiveMessageRequest = new ReceiveMessageRequest(_settings.QueueUrl);

                        receiveMessageRequest.MaxNumberOfMessages = _settings.MaxNumberOfMessagesPerRequest;

                        receiveMessageRequest.WaitTimeSeconds = _settings.ReceiveMessageWaitTimeInSeconds;

                        receiveMessageRequest.MessageAttributeNames.Add("Publisher");

                        ReceiveMessageResponse receiveMessageResponse = await _sqsClient.ReceiveMessageAsync(receiveMessageRequest);

                        // Process messages e.g. save to database, send email etc.
                        Parallel.ForEach(receiveMessageResponse.Messages, async (Message message) =>
                        {
                            string publisher = message.MessageAttributes["Publisher"].StringValue;

                            _logger.LogInformation(" [x] Received {0} from '{1}'", message.Body, publisher);
                            
                            try
                            {
                                // Delete the message after processing
                                DeleteMessageRequest deleteMessageRequest = new DeleteMessageRequest(
                                    _settings.QueueUrl,
                                    message.ReceiptHandle
                                );

                                await _sqsClient.DeleteMessageAsync(deleteMessageRequest);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogCritical(ex, "Couldn't delete an event from SQS");
                                throw;
                            }

                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Couldn't receive events from SQS");
                        throw;
                    }

                    // Wait before polling again
                    Thread.Sleep(TimeSpan.FromSeconds(_settings.PollingIntervalInSeconds));
                }
            });

            _logger.LogInformation("Event consumer started");
        }

        public void Stop()
        {
            _logger.LogInformation("Stopping event consumer");
            _running = false;
            _logger.LogInformation("Event consumer stopped");
        }
    }
}
