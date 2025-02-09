using Confluent.Kafka;
namespace consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConsumer<Ignore, string> _consumer;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;

        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092", // Change to your Kafka broker
            GroupId = "postgresql-group", // Change to your consumer group
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        _consumer.Subscribe("mydbtopic.public.users"); // Change to your topic
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult != null)
                {
                    _logger.LogInformation($"Received message: {consumeResult.Message.Value}");
                    // Process message logic here...
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"Kafka consume error: {ex.Error.Reason}");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
