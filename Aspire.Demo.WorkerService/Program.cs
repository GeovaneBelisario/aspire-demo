using Aspire.Demo.Architecture.Messaging;
using Aspire.Demo.WorkerService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQ("rabbitmq", null, cf => cf.Unbox().DispatchConsumersAsync());
builder.Services.AddSingleton<IMessageReceiver, MessageReceiver>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
