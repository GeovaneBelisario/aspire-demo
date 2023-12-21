var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedisContainer("cache");

var rabbitmq = builder.AddRabbitMQContainer("rabbitmq");

var apiservice = builder.AddProject<Projects.Aspire_Demo_ApiService>("backend")
    .WithReference(rabbitmq);

var workerservice = builder.AddProject<Projects.Aspire_Demo_WorkerService>("workerservice")
    .WithReference(rabbitmq);

builder.AddProject<Projects.Aspire_Demo_Web>("frontend")
    .WithReference(cache)
    .WithReference(apiservice);

builder.Build().Run();
