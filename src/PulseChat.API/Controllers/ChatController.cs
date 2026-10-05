using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Chat.Common;
using PulseChat.Application.Features.Chat.CreateRoom;
using PulseChat.Application.Features.Chat.GetMessages;
using PulseChat.Application.Features.Chat.GetRooms;
using PulseChat.Application.Features.Chat.MarkAsRead;
using PulseChat.Domain.Common;
using PulseChat.Domain.Enums;

namespace PulseChat.API.Controllers;

public record CreateRoomRequest(
    RoomType Type,
    string Title,
    string? AvatarUrl,
    List<Guid> MemberIds
);

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public ChatController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost("rooms")]
    [ProducesResponseType(typeof(Result<RoomDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<RoomDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var command = new CreateRoomCommand(
            userId.Value,
            request.Type,
            request.Title,
            request.AvatarUrl,
            request.MemberIds
        );

        var result = await _mediator.Send(command);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("rooms")]
    [ProducesResponseType(typeof(Result<List<RoomDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRooms()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetUserRoomsQuery(userId.Value));
        return Ok(result);
    }

    [HttpGet("rooms/{roomId}/messages")]
    [ProducesResponseType(typeof(Result<List<MessageDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<List<MessageDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRoomMessages(string roomId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetRoomMessagesQuery(roomId, userId.Value, limit, before));
        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }
        return Ok(result);
    }

    [HttpPost("rooms/{roomId}/read")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkRoomAsRead(string roomId)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new MarkRoomAsReadCommand(roomId, userId.Value));
        if (!result.IsSuccess)
        {
            return result.ErrorCode == ErrorCodes.NotRoomMember
                ? StatusCode(StatusCodes.Status403Forbidden, result)
                : BadRequest(result);
        }
        return Ok(result);
    }
}
