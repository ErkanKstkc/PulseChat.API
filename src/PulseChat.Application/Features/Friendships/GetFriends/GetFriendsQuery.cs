using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Friendships.Common;
using PulseChat.Domain.Common;
using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Friendships.GetFriends;

public record GetFriendsQuery(Guid UserId) : IRequest<Result<List<FriendDto>>>;

public class GetFriendsQueryHandler : IRequestHandler<GetFriendsQuery, Result<List<FriendDto>>>
{
    private readonly IPulseChatDbContext _context;

    public GetFriendsQueryHandler(IPulseChatDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FriendDto>>> Handle(GetFriendsQuery request, CancellationToken cancellationToken)
    {
        var friendships = await _context.Friendships
            .AsNoTracking()
            .Include(f => f.Requester)
            .Include(f => f.Addressee)
            .Where(f => (f.RequesterId == request.UserId || f.AddresseeId == request.UserId) && f.Status == FriendshipStatus.Accepted)
            .ToListAsync(cancellationToken);

        var list = friendships.Select(f =>
        {
            var isRequester = f.RequesterId == request.UserId;
            var otherUser = isRequester ? f.Addressee : f.Requester;

            return new FriendDto(
                f.Id,
                otherUser.Id,
                otherUser.Username,
                otherUser.AvatarUrl,
                f.Status,
                isRequester,
                f.CreatedAt
            );
        }).ToList();

        return Result.Success(list);
    }
}
