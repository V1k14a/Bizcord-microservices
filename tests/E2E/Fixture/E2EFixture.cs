extern alias SampleMicroservice;
extern alias PongMicroservice;
using DotNet.Testcontainers.Builders;
using MessageClient.Configuration;
using MessageClient.Factories;
using MessageClient.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.RabbitMq;
using Xunit;

namespace E2E.Tests.Fixture;

public class E2EFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .WithWaitStrategy(
            Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
        .Build();

    public HttpClient SampleMicroserviceClient { get; private set; } = null!;
    public HttpClient PongServiceClient { get; private set; } = null!;

    public IMessageClient MessageClient { get; private set; } = null!;

    private WebApplicationFactory<SampleMicroservice::Program>? _sampleFactory;
    private WebApplicationFactory<PongMicroservice::Program>? _pongFactory;

    public async Task InitializeAsync()
    {
        // Start broker first — both services connect on startup
        await _rabbitMq.StartAsync();

        var connectionString = $"host={_rabbitMq.Hostname};" +
                               $"port={_rabbitMq.GetMappedPublicPort(5672)};" +
                               $"username=guest;password=guest";

        // Standalone client for test-side subscriptions (spy on the bus)
        MessageClient = RabbitMqFactory.CreateMessageClient(
            new RabbitMqClientOptions { ConnectionString = connectionString });

        // Boot SampleMicroservice against the shared broker
        _sampleFactory = new WebApplicationFactory<SampleMicroservice::Program>()
            .WithWebHostBuilder(builder => ReplaceMessageClient(builder, connectionString));

        // Boot PongService against the same broker
        _pongFactory = new WebApplicationFactory<PongMicroservice::Program>()
            .WithWebHostBuilder(builder => ReplaceMessageClient(builder, connectionString));

        SampleMicroserviceClient = _sampleFactory.CreateClient();
        PongServiceClient = _pongFactory.CreateClient();
    }

    private static void ReplaceMessageClient(IWebHostBuilder builder, string connectionString)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMessageClient));
            if (descriptor is not null) services.Remove(descriptor);

            var client = RabbitMqFactory.CreateMessageClient(
                new RabbitMqClientOptions { ConnectionString = connectionString });
            services.AddSingleton(client);
        });
    }

    public async Task DisposeAsync()
    {
        if (_sampleFactory is not null) await _sampleFactory.DisposeAsync();
        if (_pongFactory is not null) await _pongFactory.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}