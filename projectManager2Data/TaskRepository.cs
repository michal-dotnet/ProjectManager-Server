using Microsoft.EntityFrameworkCore;
using projectManager2Core.Exceptions;
using projectManager2Core.Repositories;

namespace projectManager2Data;

public class TaskRepository : ITaskRepository
{
    private readonly DataContext _context;

    public TaskRepository(DataContext context)
    {
        _context = context;
    }

    public Task<TaskEntity?> GetByIdAsync(int eventId, int taskId, CancellationToken cancellationToken = default)
        => _context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId && t.EventId == eventId, cancellationToken);

    public async Task<(List<TaskEntity> Items, int TotalCount)> GetByEventIdAsync(int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // Include(AssignedToUser) - טוען את המשתמש המוקצה יחד עם המשימות
        // בשאילתה אחת (JOIN), במקום שה-Service ילך בלולאה ויביא כל משתמש
        // בנפרד (בעיית N+1). אותו עיקרון כמו ב-EventMemberRepository.
        var query = _context.Tasks
            .AsNoTracking()
            .Include(t => t.AssignedToUser)
            .Where(t => t.EventId == eventId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(TaskEntity task, CancellationToken cancellationToken = default)
        => await _context.Tasks.AddAsync(task, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // מגדילים את ה-Concurrency Token של כל Task שבאמת עודכנה בקריאה
        // הזו, כדי שה-WHERE שה-EF Core יפיק ישווה מול הערך שנטען במקור.
        // ריכוז ההגדלה כאן (ולא ב-Service) מבטיח שכל עדכון עתידי על Task
        // מוגן אוטומטית, בלי להסתמך על כך שכל קורא יזכור להגדיל בעצמו.
        foreach (var entry in _context.ChangeTracker.Entries<TaskEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ConcurrencyVersion++;
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // בקשה מקבילה כבר עדכנה/לקחה את אותה Task בדיוק בין הקריאה
            // שלנו ל-SaveChangesAsync - ה-Concurrency Token שנשלח ב-WHERE
            // כבר לא תואם, ה-UPDATE לא פגע באף שורה, ו-EF Core זיהה זאת.
            throw new ConflictException("המשימה כבר עודכנה על ידי משתמש אחר בינתיים.");
        }
    }
}
