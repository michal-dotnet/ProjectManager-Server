using projectManager2Core.Enums;
using projectManager2Core.Exceptions;
using projectManager2Core.Models;
using projectManager2Core.Repositories;

namespace projectManager2Service;

/// <summary>
/// בדיקות הרשאה/מצב משותפות בין EventService, EventMemberService ו-
/// TaskService (רק Event Creator מנהל, Event Completed הוא Read Only,
/// גישה מותרת ל-Creator/EventMember בלבד) - כדי לא לשכפל את חוקי ה-Business
/// האלה בכל שכבת שירות בנפרד.
/// </summary>
internal static class EventAccessGuard
{
    public static void EnsureCreator(Event @event, int currentUserId)
    {
        if (@event.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException("רק Event Creator רשאי לבצע פעולה זו.");
        }
    }

    public static void EnsureNotCompleted(Event @event)
    {
        if (@event.Status == EventStatus.Completed)
        {
            throw new ConflictException("ה-Event הסתיים (Completed) והוא Read Only.");
        }
    }

    public static async System.Threading.Tasks.Task EnsureCreatorOrMemberAsync(
        Event @event,
        int currentUserId,
        IEventMemberRepository memberRepository,
        CancellationToken cancellationToken)
    {
        if (@event.CreatedByUserId == currentUserId)
        {
            return;
        }

        var isMember = await memberRepository.IsMemberAsync(@event.Id, currentUserId, cancellationToken);
        if (!isMember)
        {
            throw new ForbiddenException("רק Event Creator או EventMember רשאים לגשת ל-Event זה.");
        }
    }
}
