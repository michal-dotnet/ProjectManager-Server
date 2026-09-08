using AutoMapper;
using projectManager2Core.Common;
using projectManager2Core.DTOs;
using projectManager2Core.Enums;
using projectManager2Core.Exceptions;
using projectManager2Core.Models;
using projectManager2Core.Repositories;
using projectManager2Core.Services;

namespace projectManager2Service;

public class TaskService : ITaskService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventMemberRepository _memberRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public TaskService(
        IEventRepository eventRepository,
        IEventMemberRepository memberRepository,
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _memberRepository = memberRepository;
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<TaskDto> CreateAsync(int currentUserId, int eventId, CreateTaskDto dto, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        EventAccessGuard.EnsureCreator(@event, currentUserId);
        EventAccessGuard.EnsureNotCompleted(@event);

        var task = new TaskEntity
        {
            EventId = eventId,
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            Status = TaskStatusEnum.Available,
            AssignedToUserId = null,
            AssignedAt = null,
            CreatedAt = DateTime.UtcNow,
            SortOrder = dto.SortOrder
        };

        await _taskRepository.AddAsync(task, cancellationToken);
        await _taskRepository.SaveChangesAsync(cancellationToken);

        return ToDto(task, assignedToUser: null);
    }

    public async Task<TaskDto> GetByIdAsync(int currentUserId, int eventId, int taskId, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        await EventAccessGuard.EnsureCreatorOrMemberAsync(@event, currentUserId, _memberRepository, cancellationToken);

        var task = await _taskRepository.GetByIdAsync(eventId, taskId, cancellationToken)
            ?? throw NotFoundException.For("Task", taskId);

        var assignedToUser = task.AssignedToUserId.HasValue
            ? await _userRepository.GetByIdAsync(task.AssignedToUserId.Value, cancellationToken)
            : null;

        return ToDto(task, assignedToUser);
    }

    public async Task<PagedResultDto<TaskDto>> GetByEventIdAsync(int currentUserId, int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        await EventAccessGuard.EnsureCreatorOrMemberAsync(@event, currentUserId, _memberRepository, cancellationToken);

        var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

        var (tasks, totalCount) = await _taskRepository.GetByEventIdAsync(eventId, normalizedPageNumber, normalizedPageSize, cancellationToken);

        // AssignedToUser כבר נטען יחד עם המשימות בשאילתה אחת (Include) בתוך
        // TaskRepository.GetByEventIdAsync - אין כאן יותר לולאה עם שאילתה
        // נפרדת לכל משתמש (בעיית N+1 שתוקנה).
        var items = tasks
            .Select(t => ToDto(t, t.AssignedToUser))
            .ToList();

        return new PagedResultDto<TaskDto>
        {
            Items = items,
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async Task<TaskDto> UpdateAsync(int currentUserId, int eventId, int taskId, UpdateTaskDto dto, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        EventAccessGuard.EnsureCreator(@event, currentUserId);
        EventAccessGuard.EnsureNotCompleted(@event);

        var task = await _taskRepository.GetByIdAsync(eventId, taskId, cancellationToken)
            ?? throw NotFoundException.For("Task", taskId);

        // UpdateTaskDto במכוון אינו מכיל AssignedToUserId/AssignedAt/Status,
        // כך שגם אם ה-Task כבר Assigned, לא ניתן דרך עדכון זה לשנות את
        // ההקצאה או את המשתמש המחזיק בה - רק את השדות התיאוריים הבאים.
        task.Title = dto.Title.Trim();
        task.Description = dto.Description?.Trim();
        task.SortOrder = dto.SortOrder;

        await _taskRepository.SaveChangesAsync(cancellationToken);

        var assignedToUser = task.AssignedToUserId.HasValue
            ? await _userRepository.GetByIdAsync(task.AssignedToUserId.Value, cancellationToken)
            : null;

        return ToDto(task, assignedToUser);
    }

    public async Task<TaskDto> TakeAsync(int currentUserId, int eventId, int taskId, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        if (@event.Status != EventStatus.Active)
        {
            throw new ConflictException("ניתן לקחת Tasks רק כאשר ה-Event במצב Active.");
        }

        var isMember = await _memberRepository.IsMemberAsync(eventId, currentUserId, cancellationToken);
        if (!isMember)
        {
            throw new ForbiddenException("רק EventMember של ה-Event רשאי לקחת Task.");
        }

        var task = await _taskRepository.GetByIdAsync(eventId, taskId, cancellationToken)
            ?? throw NotFoundException.For("Task", taskId);

        if (task.Status != TaskStatusEnum.Available)
        {
            // בדיקה מוקדמת נוחה - מחזירה הודעה ברורה מיד במקרה הרגיל. היא
            // אינה ההגנה בפועל מפני שני משתמשים שלוקחים באותו הרגע ממש
            // (Race Condition): ההגנה האמיתית היא ה-Concurrency Token
            // (TaskEntity.ConcurrencyVersion) שנבדק בפועל ב-SaveChangesAsync
            // למטה, ותקפה גם אם שתי הבקשות עוברות את הבדיקה הזו יחד.
            throw new ConflictException("ה-Task כבר נלקחה על ידי משתמש אחר.");
        }

        task.Status = TaskStatusEnum.Assigned;
        task.AssignedToUserId = currentUserId;
        task.AssignedAt = DateTime.UtcNow;

        // אם בקשה מקבילה כבר עדכנה את אותה שורה בדיוק בין ה-GetByIdAsync
        // שלמעלה לכאן, ה-Concurrency Token כבר לא תואם ו-TaskRepository
        // זורק כאן ConflictException (ראו TaskRepository.SaveChangesAsync).
        await _taskRepository.SaveChangesAsync(cancellationToken);

        var currentUser = await _userRepository.GetByIdAsync(currentUserId, cancellationToken);

        return ToDto(task, currentUser);
    }

    private TaskDto ToDto(TaskEntity task, User? assignedToUser)
    {
        var dto = _mapper.Map<TaskDto>(task);
        dto.AssignedToUserName = assignedToUser?.Name;
        return dto;
    }
}
