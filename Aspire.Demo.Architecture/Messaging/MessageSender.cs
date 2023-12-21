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

public class MessageSender : IMessageSender
{
    private static readonly ActivitySource ActivitySource = new(nameof(MessageSender));
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;

    private readonly IModel _channel;
    private readonly ILogger<MessageSender> _logger;

    public MessageSender(IConnection connection, ILogger<MessageSender> logger)
    {    
        _channel = connection.CreateModel();
        _logger = logger;
    }

    public async Task SendAsync<T>(string queue, T message)
    {
        await Task.Run(() =>
        {
            _channel.QueueDeclare(queue, true, false, false);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = false;

            // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md#span-name
            var activityName = $"{queue} send";

            using var activity = ActivitySource.StartActivity(activityName, ActivityKind.Producer);

            // Depending on Sampling (and whether a listener is registered or not), the
            // activity above may not be created.
            // If it is created, then propagate its context.
            // If it is not created, the propagate the Current context,
            // if any.
            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            // Inject the ActivityContext into the message headers to propagate trace context to the receiving service.
            Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties, this.InjectTraceContextIntoBasicProperties);

            // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
            RabbitMQExtensions.AddMessagingTags(activity, queue);

            var output = JsonConvert.SerializeObject(message);
            _channel.BasicPublish(string.Empty, queue, properties, Encoding.UTF8.GetBytes(output));
        });
    }

    public async Task ReceiveAsync<T>(string queue, Action<T> onMessage)
    {
        _channel.QueueDeclare(queue, true, false, false);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (s, e) =>
        {
            var jsonSpecified = Encoding.UTF8.GetString(e.Body.Span);
            var item = JsonConvert.DeserializeObject<T>(jsonSpecified);
            onMessage(item);
            await Task.Yield();
        };
        _channel.BasicConsume(queue, true, consumer);
        await Task.Yield();
    }

    private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
    {
        try
        {
            if (props.Headers == null)
            {
                props.Headers = new Dictionary<string, object>();
            }

            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject trace context.");
        }
    }
}

