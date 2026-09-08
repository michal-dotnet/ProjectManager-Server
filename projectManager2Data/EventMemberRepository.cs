using Microsoft.EntityFrameworkCore;
using projectManager2Core.Models;
using projectManager2Core.Repositories;

namespace projectManager2Data;

public class EventMemberRepository : IEventMemberRepository
{
    private readonly DataContext _context;

    public EventMemberRepository(DataContext context)
    {
        _context = context;
    }

    public Task<EventMember?> GetAsync(int eventId, int userId, CancellationToken cancellationToken = default)
        => _context.EventMembers.FirstOrDefaultAsync(m => m.EventId == eventId && m.UserId == userId, cancellationToken);

    public async Task<(List<EventMember> Items, int TotalCount)> GetByEventIdAsync(int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.EventMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.EventId == eventId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(m => m.JoinedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> IsMemberAsync(int eventId, int userId, CancellationToken cancellationToken = default)
        => _context.EventMembers.AnyAsync(m => m.EventId == eventId && m.UserId == userId, cancellationToken);

    public async System.Threading.Tasks.Task AddAsync(EventMember member, CancellationToken cancellationToken = default)
        => await _context.EventMembers.AddAsync(member, cancellationToken);

    public void Remove(EventMember member)
        => _context.EventMembers.Remove(member);

    public Task<bool> HasAssignedTaskAsync(int eventId, int userId, CancellationToken cancellationToken = default)
        => _context.Tasks.AnyAsync(
            t => t.EventId == eventId && t.AssignedToUserId == userId && t.Status == TaskStatusEnum.Assigned,
            cancellationToken);

    public System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
