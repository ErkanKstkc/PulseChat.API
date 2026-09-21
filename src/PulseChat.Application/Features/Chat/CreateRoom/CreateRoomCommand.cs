using FluentValidation;
using MediatR;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.Common;
using PulseChat.Domain.Common;
using PulseChat.Domain.Documents;
using PulseChat.Domain.Enums;

namespace PulseChat.Application.Features.Chat.CreateRoom;

public record CreateRoomCommand(
    Guid CreatorId,
    RoomType Type,
    string Title,
    string? AvatarUrl,
    List<Guid> MemberIds
) : IRequest<Result<RoomDto>>;

public class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Oda başlığı boş bırakılamaz.")
            .MaximumLength(100).WithMessage("Oda başlığı en fazla 100 karakter olabilir.");

        RuleFor(x => x.MemberIds)
            .NotEmpty().WithMessage("Odaya en az bir üye eklenmelidir.");
    }
}

public class CreateRoomCommandHandler : IRequestHandler<CreateRoomCommand, Result<RoomDto>>
{
    private readonly IMongoChatRepository _chatRepository;

    public CreateRoomCommandHandler(IMongoChatRepository chatRepository)
    {
        _chatRepository = chatRepository;
    }

    public async Task<Result<RoomDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var members = new List<RoomMemberDocument>
        {
            new()
            {
                UserId = request.CreatorId,
                Role = RoomRole.Admin,
                JoinedAt = DateTime.UtcNow
            }
        };

        foreach (var memberId in request.MemberIds.Distinct())
        {
            if (memberId != request.CreatorId)
            {
                members.Add(new RoomMemberDocument
                {
                    UserId = memberId,
                    Role = RoomRole.Member,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        var room = new RoomDocument
        {
            Type = request.Type,
            Title = request.Title,
            AvatarUrl = request.AvatarUrl,
            CreatedBy = request.CreatorId,
            CreatedAt = DateTime.UtcNow,
            Members = members
        };

        await _chatRepository.CreateRoomAsync(room, cancellationToken);

        var memberDtos = room.Members.Select(m => new RoomMemberDto(
            m.UserId,
            null,
            null,
            m.Role,
            m.JoinedAt,
            m.LastReadAt
        )).ToList();

        var roomDto = new RoomDto(
            room.Id,
            room.Type,
            room.Title,
            room.AvatarUrl,
            room.CreatedBy,
            room.CreatedAt,
            memberDtos
        );

        return Result.Success(roomDto);
    }
}
