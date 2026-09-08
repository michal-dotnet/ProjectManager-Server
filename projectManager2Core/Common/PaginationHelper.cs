namespace projectManager2Core.Common;

/// <summary>
/// מכווץ פרמטרים של Pagination שהגיעו מה-Client לטווח הגיוני - מונע גם
/// pageNumber שלילי/אפס וגם pageSize ענק שיחזיר יותר מדי שורות בבקשה אחת.
/// משמש בכל ה-Services שחושפים GetAll/List מדופדפים, כדי שהכיווץ יהיה
/// עקבי בכל המערכת ולא יוגדר בנפרד בכל Service.
/// </summary>
public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int PageNumber, int PageSize) Normalize(int pageNumber, int pageSize)
    {
        var normalizedPageNumber = pageNumber < 1 ? 1 : pageNumber;
        var normalizedPageSize = pageSize < 1 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
        return (normalizedPageNumber, normalizedPageSize);
    }
}
