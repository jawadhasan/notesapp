using NotesApp.Worker.Services;

namespace NotesApp.Worker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventConsumer _eventConsumer;

        public Worker(IEventConsumer eventConsumer, ILogger<Worker> logger)
        {
            _logger = logger;
            _eventConsumer = eventConsumer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            // Starting event consumer
            _eventConsumer.Start();

            if (stoppingToken.IsCancellationRequested)
            {
                //stop event consumer
                _eventConsumer.Stop();
            }
        }
    }
}