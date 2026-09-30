using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Friendships.Common;
using PulseChat.Domain.Common;
using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Friendships.GetPending;

public record GetPendingFriendRequestsQuery(Guid UserId) : IRequest<Result<List<FriendDto>>>;

public class GetPendingFriendRequestsQueryHandler : IRequestHandler<GetPendingFriendRequestsQuery, Result<List<FriendDto>>>
{
    private readonly IPulseChatDbContext _context;

    public GetPendingFriendRequestsQueryHandler(IPulseChatDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<FriendDto>>> Handle(GetPendingFriendRequestsQuery request, CancellationToken cancellationToken)
    {
        var pendingFriendships = await _context.Friendships
            .AsNoTracking()
            .Include(f => f.Requester)
            .Where(f => f.AddresseeId == request.UserId && f.Status == FriendshipStatus.Pending)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);

        var list = pendingFriendships.Select(f => new FriendDto(
            f.Id,
            f.Requester.Id,
            f.Requester.Username,
            f.Requester.AvatarUrl,
            f.Status,
            false,
            f.CreatedAt
        )).ToList();

        return Result.Success(list);
    }
}
