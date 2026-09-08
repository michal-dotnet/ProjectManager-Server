using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectManager2Core.DTOs;
using projectManager2Core.Services;

namespace projectManager2.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly ICurrentUserService _currentUserService;

    public EventsController(IEventService eventService, ICurrentUserService currentUserService)
    {
        _eventService = eventService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// יצירת Event מוגבלת ל-Role=Manager בלבד - זו ההרשאה השונה בפועל בין
    /// שני ה-Roles: משתמש Worker מקבל 403 כאן.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventDto dto, CancellationToken cancellationToken)
    {
        var @event = await _eventService.CreateAsync(_currentUserService.UserId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = @event.Id }, @event);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var @event = await _eventService.GetByIdAsync(_currentUserService.UserId, id, cancellationToken);
        return Ok(@event);
    }

    /// <summary>
    /// מדופדף - ?pageNumber=1&amp;pageSize=20 (ברירת מחדל). pageSize מכווץ
    /// למקסימום 100 ב-EventService (ראו PaginationHelper).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<EventDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var events = await _eventService.GetAllAsync(_currentUserService.UserId, pageNumber, pageSize, cancellationToken);
        return Ok(events);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<EventDto>> Update(int id, [FromBody] UpdateEventDto dto, CancellationToken cancellationToken)
    {
        var @event = await _eventService.UpdateAsync(_currentUserService.UserId, id, dto, cancellationToken);
        return Ok(@event);
    }

    // "Action Resource": יוצרים משאב "completion" (סיום ה-Event), במקום נתיב
    // עם שם-פעולה (/complete) - עדיין endpoint ייעודי ובטוח, לא PATCH גנרי.
    [HttpPost("{id:int}/completion")]
    public async Task<ActionResult<EventDto>> Complete(int id, CancellationToken cancellationToken)
    {
        var @event = await _eventService.CompleteAsync(_currentUserService.UserId, id, cancellationToken);
        return Ok(@event);
    }
}
