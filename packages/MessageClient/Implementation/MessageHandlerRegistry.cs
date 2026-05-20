using System.Linq.Expressions;
using MessageClient.Interfaces;
using MessageClient.Types;
using Microsoft.Extensions.DependencyInjection;

namespace MessageClient.Implementation;

public class MessageHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public MessageHandlerRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void RegisterAllHandlers(IMessageClient messageClient, string subscriptionPrefix = "")
    {
        // Get all concrete types in all loaded assemblies
        var allTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(t => t.IsClass && !t.IsAbstract).ToList();
        
        foreach (var type in allTypes)
        {
            // Check if it implements IMessageHandler<T>    
            var messageHandlerInterfaces = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>));

            foreach (var handlerInterface in messageHandlerInterfaces)
            {
                // handlerInterface is e.g. IMessageHandler<SomeMessageType>
                var messageType = handlerInterface.GetGenericArguments()[0];
                Console.WriteLine($"Found handler {type.Name} for message type {messageType.Name}");

                // Generate a dynamic subscription for the message type
                // Use reflection to call IMessagingClient.SubscribeAsync<T>(...)
                var subscribeAsyncMethods = typeof(IMessageClient).GetMethods()
                    .Where(m => m.Name == nameof(IMessageClient.SubscribeAsync)).ToList();

                // Pick the correct SubscribeAsync<T> method
                var subscribeAsyncMethod = subscribeAsyncMethods.FirstOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    var hasSubscriptionAndHandler = parameters.Length >= 2
                                                    && parameters[0].ParameterType == typeof(MessageSubscription)
                                                    && parameters[1].Name.StartsWith("handle", StringComparison.Ordinal);
                    var hasOptionalCancellation = parameters.Length == 2
                                                  || (parameters.Length == 3
                                                      && parameters[2].ParameterType == typeof(CancellationToken));
                    return hasSubscriptionAndHandler && hasOptionalCancellation;
                });
                if (subscribeAsyncMethod == null)
                {
                    Console.WriteLine(
                        $"Could not find matching SubscribeAsync<T> method for message type {messageType.Name}");
                    continue;
                }

                // Make the subscribe method generic for the message type
                var genericSubscribeMethod = subscribeAsyncMethod.MakeGenericMethod(messageType);

                // Build the delegate: (T message) => ???.Handler(message, CancellationToken)
                // Use reflection to create an instance of the handler
                var handlerInstance = ActivatorUtilities.CreateInstance(_serviceProvider, type);

                if (handlerInstance == null)
                {
                    Console.WriteLine($"Could not create instance of {type.Name}");
                    continue;
                }

                // Get the Handler method from the handler interface
                var methodInfo = handlerInterface.GetMethod("Handle");
                if (methodInfo == null)
                {
                    Console.WriteLine($"No Handler method found in {handlerInterface.Name}");
                    continue;
                }

                // Create a typed delegate for the handler method
                Func<object, Task> typedFunc = msg =>
                {
                    return (Task)methodInfo.Invoke(handlerInstance, new object[] { msg, CancellationToken.None });
                };

                // SubscribeAsync<T> expects a `Action<T>`
                // Using a reflection "trick" to create a lambda expression
                var param = Expression.Parameter(messageType, "message");
                var expr = Expression.Invoke(Expression.Constant(typedFunc), param);
                var lambdaType = typeof(Action<>).MakeGenericType(messageType);
                var lambda = Expression.Lambda(lambdaType, expr, param).Compile();

                // Build a unique subscriptionId
                MessageSubscription subscriptionId =
                    new MessageSubscription($"{subscriptionPrefix}_{type.FullName}_{messageType.Name}");

                var subscribeParams = genericSubscribeMethod.GetParameters().Length == 3
                    ? new object[] { subscriptionId, lambda, CancellationToken.None }
                    : new object[] { subscriptionId, lambda };

                genericSubscribeMethod.Invoke(obj: messageClient, parameters: subscribeParams);
            }
        }
    }
}