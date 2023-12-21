namespace Aspire.Demo.Architecture.Messaging
{
    using System.Diagnostics;
    using OpenTelemetry.Trace;

    using RabbitMQ.Client;

    public static class RabbitMQExtensions
    {
        public const string DefaultExchangeName = "";

        public static ConnectionFactory Unbox(this IConnectionFactory connectionFactory) => (ConnectionFactory)connectionFactory;

        public static ConnectionFactory DispatchConsumersAsync(this ConnectionFactory connectionFactory, bool useAsync = true)
        {
            connectionFactory.DispatchConsumersAsync = useAsync;
            return connectionFactory;
        }

        public static void AddMessagingTags(Activity activity, string queue)
        {
            // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
            // See:
            //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#messaging-attributes
            //   * https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/rabbitmq.md
            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", DefaultExchangeName);
            activity?.SetTag("messaging.rabbitmq.routing_key", queue);
        }

        public static TracerProviderBuilder AddRabbitMQInstrumentation(this TracerProviderBuilder tracerProviderBuilder)
        {
            tracerProviderBuilder.AddSource(nameof(MessageSender));
            tracerProviderBuilder.AddSource(nameof(MessageReceiver));
            
            return tracerProviderBuilder;
        }
    }
}
