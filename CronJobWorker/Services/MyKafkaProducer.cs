using Confluent.Kafka;
using System.Text.Json;
using CronJobWorker.Models.POCOs;
namespace CronJobWorker.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<string, string> _producer;
        private const string Topic = "email-notify";

        public KafkaProducerService()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092",
                AllowAutoCreateTopics = true,// Auto-create the topic if it doesn't exist
                Acks = Acks.All, // Ensure message is fully replicated before acknowledging
                LingerMs = 5, // Small delay to allow batching
                BatchSize = 16384 // Size of batches in bytes
            };
            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendMessageAsync(EmailBody emailBody)
        {
            string messageValue = JsonSerializer.Serialize(emailBody);
            try
            {
                var result = await _producer.ProduceAsync(Topic, new Message<string, string>
                {
                    Key = emailBody.Email, // Ensures ordering per recipient email
                    Value = messageValue
                });
                Console.WriteLine($"Message sent to {result.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Error sending message: {e.Error.Reason}");
                throw; // Re-throw to allow handling by caller
            }
        }

        public async Task SendBatchMessagesAsync(List<EmailBody> emailBodies)
        {
            if (emailBodies == null || !emailBodies.Any())
            {
                return;
            }

            var sendTasks = new List<Task>();

            try
            {
                // Create tasks for each message
                foreach (var emailBody in emailBodies)
                {
                    sendTasks.Add(SendMessageAsync(emailBody));
                }

                // Wait for all tasks to complete
                await Task.WhenAll(sendTasks);
                Console.WriteLine($"Batch of {emailBodies.Count} messages sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending batch messages: {ex.Message}");
                throw; // Re-throw to allow handling by caller
            }
            finally
            {
                // Ensure all messages are flushed
                _producer.Flush(TimeSpan.FromSeconds(10));
            }
        }

        // Method to dispose the producer when service is no longer needed
        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}