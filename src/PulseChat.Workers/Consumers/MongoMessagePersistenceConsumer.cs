using MassTransit;
using Microsoft.Extensions.Logging;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Events;
using PulseChat.Domain.Documents;

namespace PulseChat.Workers.Consumers;

public class MongoMessagePersistenceConsumer : IConsumer<MessageCreatedEvent>
{
    private readonly IMongoChatRepository _chatRepository;
    private readonly ILogger<MongoMessagePersistenceConsumer> _logger;

    public MongoMessagePersistenceConsumer(
        IMongoChatRepository chatRepository,
        ILogger<MongoMessagePersistenceConsumer> logger)
    {
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<MessageCreatedEvent> context)
    {
        var messageEvent = context.Message;
        _logger.LogInformation("Processing message persistence for clientMessageId: {ClientMessageId}", messageEvent.ClientMessageId);

        var messageDocument = new MessageDocument
        {
            RoomId = messageEvent.RoomId,
            SenderId = messageEvent.SenderId,
            ClientMessageId = messageEvent.ClientMessageId,
            Type = messageEvent.Type,
            Content = messageEvent.Content,
            MediaUrl = messageEvent.MediaUrl,
            CreatedAt = messageEvent.CreatedAt
        };

        try
        {
            await _chatRepository.SaveMessageAsync(messageDocument, context.CancellationToken);
            _logger.LogInformation("Successfully persisted message {ClientMessageId} to MongoDB", messageEvent.ClientMessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist message {ClientMessageId} to MongoDB", messageEvent.ClientMessageId);
            throw; // Re-throw to allow MassTransit retry / dead-letter
        }
    }
}
