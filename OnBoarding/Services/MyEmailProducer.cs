using Confluent.Kafka;
using System.Text.Json;
using OnBoarding.Models.POCOs;

namespace OnBoarding.Services
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
                Acks = Acks.All // Ensure message is fully replicated before acknowledging
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

                Console.WriteLine($" Message sent to {result.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> e)
            {
                Console.WriteLine($"Error sending message: {e.Error.Reason}");
            }
            finally
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
            }
        }
    }
}
