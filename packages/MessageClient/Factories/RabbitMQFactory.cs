using EasyNetQ;
using EasyNetQ.DI;
using EasyNetQ.Serialization.SystemTextJson;
using MessageClient.Adapters;
using MessageClient.Configuration;
using MessageClient.Interfaces;

namespace MessageClient.Factories;

public static class RabbitMqFactory
{
    private static RabbitMqAdapter CreateAdapter(IMessageClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
            throw new ArgumentException("Connection string is required.", nameof(options));

        IBus bus = RabbitHutch.CreateBus(
            options.ConnectionString,
            register => register.Register<ISerializer, SystemTextJsonSerializer>(Lifetime.Singleton));

        return new RabbitMqAdapter(bus);
    }

    public static IMessageClient CreateMessageClient(IMessageClientOptions options)
    {
        var adapter = CreateAdapter(options);
        return new Implementation.MessageClient(adapter);
    }
}
