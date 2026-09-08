namespace projectManager2Core.DTOs;

/// <summary>
/// Wrapper גנרי לתוצאה מדופדפת (Pagination) - עוטף כל List של DTO יחד עם
/// מידע על העמוד הנוכחי, כדי שה-Client ידע כמה עמודים יש בסה"כ ולא יצטרך
/// לבקש הכל בבת אחת. משמש בכל endpoint שמחזיר רשימה (Events/Users/Tasks/
/// EventMembers).
/// </summary>
public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
