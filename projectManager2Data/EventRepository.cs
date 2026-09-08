using Microsoft.EntityFrameworkCore;
using projectManager2Core.Models;
using projectManager2Core.Repositories;

namespace projectManager2Data;

public class EventRepository : IEventRepository
{
    private readonly DataContext _context;

    public EventRepository(DataContext context)
    {
        _context = context;
    }

    public Task<Event?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<Event?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        => _context.Events
            .AsNoTracking()
            .Include(e => e.Members)
            .Include(e => e.Tasks)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<(List<Event> Items, int TotalCount)> GetAllForUserAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Events
            .AsNoTracking()
            .Where(e => e.CreatedByUserId == userId || e.Members.Any(m => m.UserId == userId));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async System.Threading.Tasks.Task AddAsync(Event @event, CancellationToken cancellationToken = default)
        => await _context.Events.AddAsync(@event, cancellationToken);

    public System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
