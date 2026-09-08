namespace projectManager2Core.Enums;

/// <summary>
/// מצב מחזור החיים של Event.
/// Draft -> Active -> Completed (מעבר חד-כיווני).
/// </summary>
public enum EventStatus
{
    Draft = 0,
    Active = 1,
    Completed = 2
}
