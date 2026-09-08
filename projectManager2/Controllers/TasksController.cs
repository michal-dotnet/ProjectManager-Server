using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectManager2Core.DTOs;
using projectManager2Core.Services;

namespace projectManager2.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUserService _currentUserService;

    public TasksController(ITaskService taskService, ICurrentUserService currentUserService)
    {
        _taskService = taskService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create(int eventId, [FromBody] CreateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateAsync(_currentUserService.UserId, eventId, dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { eventId, taskId = task.Id }, task);
    }

    [HttpGet("{taskId:int}")]
    public async Task<ActionResult<TaskDto>> GetById(int eventId, int taskId, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(_currentUserService.UserId, eventId, taskId, cancellationToken);
        return Ok(task);
    }

    /// <summary>
    /// מדופדף - ?pageNumber=1&amp;pageSize=20 (ברירת מחדל). pageSize מכווץ
    /// למקסימום 100 ב-TaskService (ראו PaginationHelper).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<TaskDto>>> GetByEvent(
        int eventId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tasks = await _taskService.GetByEventIdAsync(_currentUserService.UserId, eventId, pageNumber, pageSize, cancellationToken);
        return Ok(tasks);
    }

    [HttpPut("{taskId:int}")]
    public async Task<ActionResult<TaskDto>> Update(int eventId, int taskId, [FromBody] UpdateTaskDto dto, CancellationToken cancellationToken)
    {
        var task = await _taskService.UpdateAsync(_currentUserService.UserId, eventId, taskId, dto, cancellationToken);
        return Ok(task);
    }

    // "Action Resource": יוצרים משאב "assignment" (השיוך של המשתמש הנוכחי ל-Task
    // הזו), במקום נתיב עם שם-פעולה (/take) - עדיין endpoint ייעודי ובטוח, לא
    // PATCH גנרי שמאפשר לשנות לכל סטטוס.
    [HttpPost("{taskId:int}/assignment")]
    public async Task<ActionResult<TaskDto>> Take(int eventId, int taskId, CancellationToken cancellationToken)
    {
        var task = await _taskService.TakeAsync(_currentUserService.UserId, eventId, taskId, cancellationToken);
        return Ok(task);
    }
}
