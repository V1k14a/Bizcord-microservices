using MessagingMicroservice.Domain;

namespace MessagingMicroservice.Domain.ValueObjects;

/// <summary>
/// Non-empty message body (immutable value object).
/// </summary>
public sealed class MessageContent : IEquatable<MessageContent>
{
    public string Value { get; }

    private MessageContent(string value) => Value = value;

    public static MessageContent Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Message content cannot be empty.");

        return new MessageContent(value.Trim());
    }

    public bool Equals(MessageContent? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is MessageContent other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);

    public override string ToString() => Value;
}
