using FluentValidation;
using MediatR;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Domain.Common;

namespace PulseChat.Application.Features.Chat.MarkAsRead;

public record MarkRoomAsReadCommand(
    string RoomId,
    Guid UserId
) : IRequest<Result>;

public class MarkRoomAsReadCommandValidator : AbstractValidator<MarkRoomAsReadCommand>
{
    public MarkRoomAsReadCommandValidator()
    {
        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Oda kimliği boş olamaz.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı kimliği boş olamaz.");
    }
}

public class MarkRoomAsReadCommandHandler : IRequestHandler<MarkRoomAsReadCommand, Result>
{
    private readonly IMongoChatRepository _chatRepository;

    public MarkRoomAsReadCommandHandler(IMongoChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<Result> Handle(MarkRoomAsReadCommand request, CancellationToken cancellationToken)
    {
        var isMember = await _chatRepository.IsUserInRoomAsync(request.RoomId, request.UserId, cancellationToken);
        if (!isMember)
        {
            return Result.Failure(ErrorCodes.NotRoomMember, "Bu odaya erişim yetkiniz yok.");
        }

        await _chatRepository.UpdateMemberLastReadAsync(request.RoomId, request.UserId, DateTime.UtcNow, cancellationToken);
        return Result.Success();
    }
}
