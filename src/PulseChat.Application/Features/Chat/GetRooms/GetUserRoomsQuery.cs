using MediatR;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.Common;
using PulseChat.Domain.Common;

namespace PulseChat.Application.Features.Chat.GetRooms;

public record GetUserRoomsQuery(Guid UserId) : IRequest<Result<List<RoomDto>>>;

public class GetUserRoomsQueryHandler : IRequestHandler<GetUserRoomsQuery, Result<List<RoomDto>>>
{
    private readonly IMongoChatRepository _chatRepository;

    public GetUserRoomsQueryHandler(IMongoChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<Result<List<RoomDto>>> Handle(GetUserRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await _chatRepository.GetUserRoomsAsync(request.UserId, cancellationToken);

        var roomDtos = new List<RoomDto>(rooms.Count);

        foreach (var r in rooms)
        {
            var userMember = r.Members.FirstOrDefault(m => m.UserId == request.UserId);
            var lastReadAt = userMember?.LastReadAt;
            var unreadCount = await _chatRepository.GetUnreadCountAsync(r.Id, request.UserId, lastReadAt, cancellationToken);
            var latestMessage = await _chatRepository.GetLatestMessageAsync(r.Id, cancellationToken);

            var memberDtos = r.Members.Select(m => new RoomMemberDto(
                m.UserId,
                null,
                null,
                m.Role,
                m.JoinedAt,
                m.LastReadAt
            )).ToList();

            roomDtos.Add(new RoomDto(
                r.Id,
                r.Type,
                r.Title,
                r.AvatarUrl,
                r.CreatedBy,
                r.CreatedAt,
                memberDtos,
                unreadCount,
                latestMessage?.Content,
                latestMessage?.CreatedAt
            ));
        }

        return Result.Success(roomDtos);
    }
}
