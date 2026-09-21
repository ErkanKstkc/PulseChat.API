using MediatR;
using Microsoft.EntityFrameworkCore;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Domain.Common;
using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Friendships.RespondRequest;

public record RespondFriendRequestCommand(
    Guid FriendshipId,
    Guid UserId,
    bool Accept
) : IRequest<Result>;

public class RespondFriendRequestCommandHandler : IRequestHandler<RespondFriendRequestCommand, Result>
{
    private readonly IPulseChatDbContext _context;

    public RespondFriendRequestCommandHandler(IPulseChatDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(RespondFriendRequestCommand request, CancellationToken cancellationToken)
    {
        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f => f.Id == request.FriendshipId && f.AddresseeId == request.UserId, cancellationToken);

        if (friendship == null)
        {
            return Result.Failure(ErrorCodes.FriendshipNotFound, "Arkadaşlık isteği bulunamadı.");
        }

        if (friendship.Status != FriendshipStatus.Pending)
        {
            return Result.Failure(ErrorCodes.Forbidden, "Bu istek daha önce yanıtlanmış.");
        }

        if (request.Accept)
        {
            friendship.Status = FriendshipStatus.Accepted;
        }
        else
        {
            _context.Friendships.Remove(friendship);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
