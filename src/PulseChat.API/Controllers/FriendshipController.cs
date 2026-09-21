using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PulseChat.Application.Common.Interfaces;
using PulseChat.Application.Features.Friendships.Common;
using PulseChat.Application.Features.Friendships.GetFriends;
using PulseChat.Application.Features.Friendships.RespondRequest;
using PulseChat.Application.Features.Friendships.SendRequest;
using PulseChat.Domain.Common;

namespace PulseChat.API.Controllers;

public record SendFriendRequestDto(string TargetUsername);
public record RespondFriendRequestDto(Guid FriendshipId, bool Accept);

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class FriendshipController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public FriendshipController(IMediator mediator, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    [HttpPost("request")]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<Guid>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new SendFriendRequestCommand(userId.Value, dto.TargetUsername));
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("respond")]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RespondRequest([FromBody] RespondFriendRequestDto dto)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new RespondFriendRequestCommand(dto.FriendshipId, userId.Value, dto.Accept));
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<List<FriendDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFriends()
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetFriendsQuery(userId.Value));
        return Ok(result);
    }
}
