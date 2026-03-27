using DotNet.Testcontainers.Builders;
using MessageClient.Configuration;
using MessageClient.Factories;
using MessageClient.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.RabbitMq;
using Xunit;

namespace SampleMicroservice.Tests.Fixture;

public class SampleMicroserviceFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .WithWaitStrategy(
            Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
        .Build();

    public IMessageClient MessageClient { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMessageClient));

            if (descriptor is not null)
                services.Remove(descriptor);

            var connectionString = BuildConnectionString();
            var options = new RabbitMqClientOptions { ConnectionString = connectionString };

            MessageClient = RabbitMqFactory.CreateMessageClient(options);
            services.AddSingleton(MessageClient);
        });
    }

    private string BuildConnectionString() =>
        $"host={_rabbitMq.Hostname};port={_rabbitMq.GetMappedPublicPort(5672)};" +
        $"username=guest;password=guest";

    public async Task InitializeAsync()
    {
        await _rabbitMq.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _rabbitMq.DisposeAsync();
    }
}