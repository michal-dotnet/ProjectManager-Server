using AutoMapper;
using projectManager2Core.Common;
using projectManager2Core.DTOs;
using projectManager2Core.Enums;
using projectManager2Core.Exceptions;
using projectManager2Core.Models;
using projectManager2Core.Repositories;
using projectManager2Core.Services;

namespace projectManager2Service;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public EventService(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<EventDto> CreateAsync(int currentUserId, CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        var @event = new Event
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CreatedByUserId = currentUserId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            // מרגע היצירה ה-Event פעיל (Active) - כדי שניתן יהיה מיד להוסיף
            // חברים ולקחת Tasks, בלי שלב Draft ידני נפרד (אין כרגע endpoint
            // למעבר Draft->Active, אז Draft היה נשאר "תקוע").
            Status = EventStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _eventRepository.AddAsync(@event, cancellationToken);
        await _eventRepository.SaveChangesAsync(cancellationToken);

        return ToDto(@event);
    }

    public async Task<EventDto> GetByIdAsync(int currentUserId, int eventId, CancellationToken cancellationToken = default)
    {
        var @event = await LoadWithAccessCheckAsync(currentUserId, eventId, cancellationToken);
        return ToDto(@event);
    }

    public async Task<PagedResultDto<EventDto>> GetAllAsync(int currentUserId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

        var (events, totalCount) = await _eventRepository.GetAllForUserAsync(currentUserId, normalizedPageNumber, normalizedPageSize, cancellationToken);

        return new PagedResultDto<EventDto>
        {
            Items = events.Select(ToDto).ToList(),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async Task<EventDto> UpdateAsync(int currentUserId, int eventId, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        EventAccessGuard.EnsureCreator(@event, currentUserId);
        EventAccessGuard.EnsureNotCompleted(@event);

        @event.Name = dto.Name.Trim();
        @event.Description = dto.Description?.Trim();
        @event.StartDate = dto.StartDate;
        @event.EndDate = dto.EndDate;

        await _eventRepository.SaveChangesAsync(cancellationToken);

        return ToDto(@event);
    }

    public async Task<EventDto> CompleteAsync(int currentUserId, int eventId, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        EventAccessGuard.EnsureCreator(@event, currentUserId);

        if (@event.Status == EventStatus.Completed)
        {
            throw new ConflictException("ה-Event כבר מסומן כ-Completed.");
        }

        @event.Status = EventStatus.Completed;

        await _eventRepository.SaveChangesAsync(cancellationToken);

        return ToDto(@event);
    }

    private async Task<Event> LoadWithAccessCheckAsync(int currentUserId, int eventId, CancellationToken cancellationToken)
    {
        var @event = await _eventRepository.GetByIdWithDetailsAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        var isCreator = @event.CreatedByUserId == currentUserId;
        var isMember = @event.Members.Any(m => m.UserId == currentUserId);

        if (!isCreator && !isMember)
        {
            throw new ForbiddenException("רק Event Creator או EventMember רשאים לגשת ל-Event זה.");
        }

        return @event;
    }

    private EventDto ToDto(Event @event) => _mapper.Map<EventDto>(@event);
}
