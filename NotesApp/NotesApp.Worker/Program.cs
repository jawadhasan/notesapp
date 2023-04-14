using NotesApp.Worker;
using NotesApp.Worker.Services;
using NotesApp.Worker.Settings;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext,services) =>
    {
        // Configuring dependency injection
        services.Configure<SQSSettings>(
            hostContext.Configuration.GetSection("Queueing"));

        services.AddSingleton<IEventConsumer, SQSEventConsumer>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
