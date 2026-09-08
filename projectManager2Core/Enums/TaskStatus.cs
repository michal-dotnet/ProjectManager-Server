namespace projectManager2Core.Enums;

/// <summary>
/// מצב Task בתוך Event.
/// Available - פנויה, ניתן לקחת אותה.
/// Assigned - נלקחה על ידי EventMember ונעולה לו עד סיום ה-Event.
/// </summary>
public enum TaskStatus
{
    Available = 0,
    Assigned = 1
}
