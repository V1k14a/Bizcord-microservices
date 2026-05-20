using FluentAssertions;
using MessageClient.Interfaces;
using MessageClient.Types;
using MessagingMicroservice.Handlers;
using MessagingMicroservice.Infrastructure;
using MessagingMicroservice.Saga;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts.Events;
using Shared.Contracts.Sagas;
using Xunit;

namespace MessagingMicroservice.Tests.Unit;

public class MessagePostSagaOrchestratorTests
{
    private sealed class TestMessageClient : IMessageClient
    {
        public List<object> Published { get; } = new();
        public Func<object, bool>? ThrowOnPublishPredicate { get; init; }

        public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        {
            if (ThrowOnPublishPredicate?.Invoke(message!) == true)
                throw new InvalidOperationException("broker unavailable");

            Published.Add(message!);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync<TMessage>(Action<TMessage> handler, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SubscribeAsync<TMessage>(
            MessageSubscription subscriptionId,
            Action<TMessage> handler,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UnsubscribeAsync<TMessage>(
            MessageSubscription subscriptionId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Dispose() { }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Happy_path_persists_then_publishes_integration_event()
    {
        var bus = new TestMessageClient();
        var sagas = new InMemorySagaStateRepository();
        var messages = new InMemoryMessageRepository();
        var orchestrator = new MessagePostSagaOrchestrator(
            bus,
            sagas,
            messages,
            NullLogger<MessagePostSagaOrchestrator>.Instance);

        var sagaId = Guid.NewGuid();
        await orchestrator.Handle(
            new InitiateMessagePost
            {
                SagaId = sagaId,
                ChannelId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Content = "hello"
            },
            CancellationToken.None);

        var persistedMsg = bus.Published.OfType<MessagePersisted>().Single();
        persistedMsg.SagaId.Should().Be(sagaId);

        await orchestrator.Handle(persistedMsg, CancellationToken.None);

        bus.Published.OfType<MessagePostedEvent>().Should().ContainSingle();
        bus.Published.OfType<MessagePostSagaCompleted>().Should().ContainSingle();
        (await messages.GetByIdAsync(persistedMsg.MessageId, CancellationToken.None)).Should().NotBeNull();
    }

    [Fact]
    public async Task When_event_publish_fails_message_is_deleted()
    {
        var bus = new TestMessageClient
        {
            ThrowOnPublishPredicate = m => m is MessagePostedEvent
        };
        var sagas = new InMemorySagaStateRepository();
        var messages = new InMemoryMessageRepository();
        var orchestrator = new MessagePostSagaOrchestrator(
            bus,
            sagas,
            messages,
            NullLogger<MessagePostSagaOrchestrator>.Instance);

        var sagaId = Guid.NewGuid();
        await orchestrator.Handle(
            new InitiateMessagePost
            {
                SagaId = sagaId,
                ChannelId = Guid.NewGuid(),
                AuthorId = Guid.NewGuid(),
                Content = "fail publish"
            },
            CancellationToken.None);

        var persistedMsg = bus.Published.OfType<MessagePersisted>().Single();
        await orchestrator.Handle(persistedMsg, CancellationToken.None);

        var failure = bus.Published.OfType<MessagePostedPublishFailed>().Single();
        failure.SagaId.Should().Be(sagaId);

        await orchestrator.Handle(failure, CancellationToken.None);

        (await messages.GetByIdAsync(persistedMsg.MessageId, CancellationToken.None)).Should().BeNull();
    }
}
