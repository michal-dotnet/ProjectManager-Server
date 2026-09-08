using projectManager2Core.Models;

namespace projectManager2Core.Repositories;

public interface IEventMemberRepository
{
    Task<EventMember?> GetAsync(int eventId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// מדופדף - מחזיר גם את הסה"כ (TotalCount) לצורך חישוב TotalPages.
    /// </summary>
    Task<(List<EventMember> Items, int TotalCount)> GetByEventIdAsync(int eventId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<bool> IsMemberAsync(int eventId, int userId, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task AddAsync(EventMember member, CancellationToken cancellationToken = default);

    void Remove(EventMember member);

    /// <summary>
    /// האם למשתמש יש Task כלשהי שהוא מחזיק (Assigned) בתוך ה-Event הנתון.
    /// משמש למניעת הסרת EventMember שמחזיק Task (409 Conflict).
    /// </summary>
    Task<bool> HasAssignedTaskAsync(int eventId, int userId, CancellationToken cancellationToken = default);

    System.Threading.Tasks.Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
