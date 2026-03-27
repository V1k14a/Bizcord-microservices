namespace MessageClient.Types;

public record MessageHandler<T> (Action<T> Handler);