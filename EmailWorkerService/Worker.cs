using Confluent.Kafka;
using System.Text.Json;
using EmailWorkerService.Models;
using EmailWorkerService.Services; // Import your EmailBody model

namespace EmailWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly EmailSender _emailSender;

        public Worker(ILogger<Worker> logger , EmailSender emailSender)
        {
            _logger = logger;
            _emailSender = emailSender;
            // Kafka Consumer Configuration
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092", // Address of the Kafka server
                GroupId = "email-consumer-group", // Consumer group name (helps in scaling)
                AutoOffsetReset = AutoOffsetReset.Earliest, // Start reading from the beginning if no offset is found
                EnableAutoCommit = false
            };

            // Initialize Kafka Consumer
            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();

            // Subscribe to the Kafka topic
            _consumer.Subscribe("email-notify");
        }

        /// <summary>
        /// This method runs continuously in the background and listens for Kafka messages.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Kafka Consumer Service Started.");

            while (!stoppingToken.IsCancellationRequested) // Keep running until the service is stopped
            {
                try
                {
                    // Consume (read) a message from Kafka
                    var consumeResult = _consumer.Consume(stoppingToken);

                    if (consumeResult != null)
                    {
                        var message = consumeResult.Message.Value; // Extract the message string

                        // Convert JSON string to EmailBody object
                        var emailBody = JsonSerializer.Deserialize<EmailBody>(message);

                        _logger.LogInformation($"Received Kafka Message: {message}");

                        // Process the email (we will implement actual email sending logic later)
                        // Send the email
                        await _emailSender.SendEmailAsync(emailBody);

                        // Manually commit the offset only if email was sent successfully
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in Kafka Consumer: {ex.Message}");
                }

                // Wait before the next iteration to prevent high CPU usage
                await Task.Delay(1000, stoppingToken);
            }
        }

        /// <summary>
        /// Clean up resources when the service stops.
        /// </summary>
        public override void Dispose()
        {
            _consumer.Close(); // Gracefully close the Kafka consumer
            _consumer.Dispose(); // Release resources
            base.Dispose();
        }
    }
}
