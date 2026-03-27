using MessageClient.Configuration;
using MessageClient.Factories;
using MessageClient.Implementation;
using MessageClient.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace MessageClient.Extension;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// Registers <see cref="IMessageClient"/> as a singleton (EasyNetQ over RabbitMQ).
    /// </summary>
    public static IServiceCollection AddRabbitMqMessageClient(this IServiceCollection services, IMessageClientOptions options)
    {
        services.AddSingleton<IMessageClient>(_ => RabbitMqFactory.CreateMessageClient(options));
        return services;
    }

    /// <summary>
    /// Configures connection options with a delegate (e.g. from <c>builder.Configuration</c>).
    /// </summary>
    public static IServiceCollection AddRabbitMqMessageClient(
        this IServiceCollection services,
        Action<RabbitMqClientOptions> configure)
    {
        var options = new RabbitMqClientOptions { ConnectionString = string.Empty };
        configure(options);
        return services.AddRabbitMqMessageClient(options);
    }

    public static IServiceCollection AddRabbitMqMessageClient(
        this IServiceCollection services,
        IMessageClientOptions options,
        MessageHandlerOptions handlerOptions)
    {
        services.AddRabbitMqMessageClient(options);
        services.AddSingleton<MessageHandlerRegistry>();
        services.AddHostedService<MessageBackgroundService>();

        return services;
    }
}
