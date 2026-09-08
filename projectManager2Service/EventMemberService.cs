using AutoMapper;
using projectManager2Core.Common;
using projectManager2Core.DTOs;
using projectManager2Core.Exceptions;
using projectManager2Core.Models;
using projectManager2Core.Repositories;
using projectManager2Core.Services;

namespace projectManager2Service;

public class EventMemberService : IEventMemberService
{
    private readonly IEventRepository _eventRepository;
    private readonly IEventMemberRepository _memberRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public EventMemberService(
        IEventRepository eventRepository,
        IEventMemberRepository memberRepository,
        IUserRepository userRepository,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _memberRepository = memberRepository;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<EventMemberDto> AddAsync(int currentUserId, int eventId, AddEventMemberDto dto, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        EventAccessGuard.EnsureCreator(@event, currentUserId);
        EventAccessGuard.EnsureNotCompleted(@event);

        var targetUser = await _userRepository.GetByIdAsync(dto.UserId, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), dto.UserId);

        var alreadyMember = await _memberRepository.IsMemberAsync(eventId, dto.UserId, cancellationToken);
        if (alreadyMember)
        {
            throw new ConflictException("המשתמש כבר חבר ב-Event זה.");
        }

        var member = new EventMember
        {
            EventId = eventId,
            UserId = dto.UserId,
            JoinedAt = DateTime.UtcNow
        };

        await _memberRepository.AddAsync(member, cancellationToken);
        await _memberRepository.SaveChangesAsync(cancellationToken);

        return ToDto(member, targetUser);
    }

    public async Task<PagedResultDto<EventMemberDto>> GetByEventIdAsync(int currentUserId, int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        await EventAccessGuard.EnsureCreatorOrMemberAsync(@event, currentUserId, _memberRepository, cancellationToken);

        var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

        var (members, totalCount) = await _memberRepository.GetByEventIdAsync(eventId, normalizedPageNumber, normalizedPageSize, cancellationToken);

        return new PagedResultDto<EventMemberDto>
        {
            Items = members.Select(m => ToDto(m, m.User)).ToList(),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    public async System.Threading.Tasks.Task RemoveAsync(int currentUserId, int eventId, int userId, CancellationToken cancellationToken = default)
    {
        var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken)
            ?? throw NotFoundException.For(nameof(Event), eventId);

        EventAccessGuard.EnsureCreator(@event, currentUserId);
        EventAccessGuard.EnsureNotCompleted(@event);

        var member = await _memberRepository.GetAsync(eventId, userId, cancellationToken)
            ?? throw NotFoundException.For(nameof(EventMember), userId);

        var hasAssignedTask = await _memberRepository.HasAssignedTaskAsync(eventId, userId, cancellationToken);
        if (hasAssignedTask)
        {
            throw new ConflictException("לא ניתן להסיר משתמש שמחזיק Task פעילה ב-Event.");
        }

        _memberRepository.Remove(member);
        await _memberRepository.SaveChangesAsync(cancellationToken);
    }

    private EventMemberDto ToDto(EventMember member, User? user)
    {
        var dto = _mapper.Map<EventMemberDto>(member);
        dto.UserName = user?.Name ?? string.Empty;
        dto.UserEmail = user?.Email ?? string.Empty;
        return dto;
    }
}
