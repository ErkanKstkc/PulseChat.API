using MediatR;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.Common;
using PulseChat.Domain.Common;

namespace PulseChat.Application.Features.Chat.GetMessages;

public record GetRoomMessagesQuery(
    string RoomId,
    Guid UserId,
    int Limit = 50,
    DateTime? Before = null
) : IRequest<Result<List<MessageDto>>>;

public class GetRoomMessagesQueryHandler : IRequestHandler<GetRoomMessagesQuery, Result<List<MessageDto>>>
{
    private readonly IMongoChatRepository _chatRepository;

    public GetRoomMessagesQueryHandler(IMongoChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<Result<List<MessageDto>>> Handle(GetRoomMessagesQuery request, CancellationToken cancellationToken)
    {
        var isMember = await _chatRepository.IsUserInRoomAsync(request.RoomId, request.UserId, cancellationToken);
        if (!isMember)
        {
            return Result.Failure<List<MessageDto>>(ErrorCodes.NotRoomMember, "Bu odadaki mesajları görüntüleme yetkiniz yok.");
        }

        var messages = await _chatRepository.GetRoomMessagesAsync(request.RoomId, request.Limit, request.Before, cancellationToken);

        var dtos = messages.Select(m => new MessageDto(
            m.Id,
            m.RoomId,
            m.SenderId,
            null,
            m.ClientMessageId,
            m.Type,
            m.Content,
            m.MediaUrl,
            m.CreatedAt
        )).ToList();

        return Result.Success(dtos);
    }
}
