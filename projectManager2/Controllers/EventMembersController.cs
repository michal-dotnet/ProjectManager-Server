using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectManager2Core.DTOs;
using projectManager2Core.Services;

namespace projectManager2.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/members")]
[Authorize]
public class EventMembersController : ControllerBase
{
    private readonly IEventMemberService _memberService;
    private readonly ICurrentUserService _currentUserService;

    public EventMembersController(IEventMemberService memberService, ICurrentUserService currentUserService)
    {
        _memberService = memberService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<EventMemberDto>> Add(int eventId, [FromBody] AddEventMemberDto dto, CancellationToken cancellationToken)
    {
        var member = await _memberService.AddAsync(_currentUserService.UserId, eventId, dto, cancellationToken);
        return Created($"/api/events/{eventId}/members/{member.UserId}", member);
    }

    /// <summary>
    /// מדופדף - ?pageNumber=1&amp;pageSize=20 (ברירת מחדל). pageSize מכווץ
    /// למקסימום 100 ב-EventMemberService (ראו PaginationHelper).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<EventMemberDto>>> GetByEvent(
        int eventId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var members = await _memberService.GetByEventIdAsync(_currentUserService.UserId, eventId, pageNumber, pageSize, cancellationToken);
        return Ok(members);
    }

    [HttpDelete("{userId:int}")]
    public async Task<IActionResult> Remove(int eventId, int userId, CancellationToken cancellationToken)
    {
        await _memberService.RemoveAsync(_currentUserService.UserId, eventId, userId, cancellationToken);
        return NoContent();
    }
}
