namespace Aspire.Demo.Architecture.Messaging;

using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Newtonsoft.Json;
using Google.Protobuf;

public class MessageReceiver : IMessageReceiver
{
    private static readonly ActivitySource ActivitySource = new(nameof(MessageReceiver));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private readonly IModel _channel;
    private readonly ILogger<MessageReceiver> _logger;

    public MessageReceiver(IConnection connection, ILogger<MessageReceiver> logger)
    {    
        _channel = connection.CreateModel();
        _logger = logger;
    }
    
    public async Task ReceiveAsync<T>(string queue, Action<T> onMessage)
    {
        _channel.QueueDeclare(queue, true, false, false);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (s, e) =>
        {
            // Extract the PropagationContext of the upstream parent from the message headers.
            var parentContext = Propagator.Extract(default, e.BasicProperties, this.ExtractTraceContextFromBasicProperties);
            Baggage.Current = parentContext.Baggage;

            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
            var activityName = $"{e.RoutingKey} receive";

            using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Consumer, parentContext.ActivityContext);

            var jsonSpecified = Encoding.UTF8.GetString(e.Body.Span);

            activity?.SetTag("message", jsonSpecified);

            // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
            RabbitMQExtensions.AddMessagingTags(activity, queue);

            var item = JsonConvert.DeserializeObject<T>(jsonSpecified);
            onMessage(item);
            await Task.Yield();
        };
        _channel.BasicConsume(queue, true, consumer);
        await Task.Yield();
    }

    private IEnumerable<string> ExtractTraceContextFromBasicProperties(IBasicProperties props, string key)
    {
        try
        {
            if (props.Headers.TryGetValue(key, out var value))
            {
                var bytes = value as byte[];
                return new[] { Encoding.UTF8.GetString(bytes) };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract trace context.");
        }

        return Enumerable.Empty<string>();
    }
}

