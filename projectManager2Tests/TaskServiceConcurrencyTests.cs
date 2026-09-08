using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using projectManager2Core.DTOs;
using projectManager2Core.Enums;
using projectManager2Core.Exceptions;
using projectManager2Core.Models;
using projectManager2Data;
using projectManager2Service;
using projectManager2Service.Mapping;
using Xunit;

namespace projectManager2Tests;

/// <summary>
/// בדיקות ללוגיקה הקריטית ביותר במערכת: לקיחת Task (TaskService.TakeAsync),
/// ובמיוחד לתרחיש שבו שני משתמשים מנסים לקחת את אותה Task בו-זמנית.
///
/// ההגנה מפני התרחיש הזה (נקודה 6) מבוססת על Concurrency Token אמיתי של
/// EF Core (TaskEntity.ConcurrencyVersion, ראו DataContext) - עדכון על
/// שורה שכבר השתנתה מאז שנטענה נכשל (0 שורות מושפעות) וזורק
/// DbUpdateConcurrencyException, שמתורגם ל-ConflictException ב-
/// TaskRepository.SaveChangesAsync.
///
/// משתמשים ב-EF Core עם ספק Sqlite In-Memory במצב Shared Cache (ולא ב-
/// InMemory Provider של EF Core) כדי שהבדיקות ירוצו מול Database יחסי
/// אמיתי (פקודות UPDATE עם WHERE אמיתי, נעילות אמיתיות תחת גישה מקבילה) -
/// קרוב הרבה יותר להתנהגות בפועל מול SQL Server בפרודקשן. כל DbContext
/// מקבל חיבור Sqlite נפרד (לא אותו אובייקט Connection) כדי לאפשר שימוש
/// מקביל אמיתי משני "Threads"/"מופעי API" בו-זמנית, בעוד ש-cache=shared
/// מבטיח ששני החיבורים רואים את אותו מסד נתונים In-Memory.
/// </summary>
public class TaskServiceConcurrencyTests : IDisposable
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private readonly string _connectionString;
    private readonly SqliteConnection _keepAliveConnection;

    private readonly int _creatorId = 1;
    private readonly int _member1Id = 2;
    private readonly int _member2Id = 3;
    private readonly int _eventId = 1;
    private readonly int _taskId = 1;

    public TaskServiceConcurrencyTests()
    {
        var dbName = $"pm2tests_{Guid.NewGuid():N}";
        _connectionString = $"DataSource=file:{dbName}?mode=memory&cache=shared";

        // חיבור "משמורת" שנשאר פתוח לאורך כל הבדיקה - כל עוד הוא פתוח, מסד
        // ה-Sqlite המשותף (cache=shared) לא נהרס בין פתיחה/סגירה של
        // חיבורים אחרים שמצביעים לאותו שם.
        _keepAliveConnection = new SqliteConnection(_connectionString);
        _keepAliveConnection.Open();

        using var context = new DataContext(BuildOptions());
        context.Database.EnsureCreated();
        Seed(context);
    }

    private DbContextOptions<DataContext> BuildOptions()
        => new DbContextOptionsBuilder<DataContext>().UseSqlite(_connectionString).Options;

    private void Seed(DataContext context)
    {
        var now = DateTime.UtcNow;

        context.Users.AddRange(
            new User { Id = _creatorId, Name = "Creator", Email = "creator@test.local", IsActive = true, CreatedAt = now },
            new User { Id = _member1Id, Name = "Member1", Email = "member1@test.local", IsActive = true, CreatedAt = now },
            new User { Id = _member2Id, Name = "Member2", Email = "member2@test.local", IsActive = true, CreatedAt = now });

        context.Events.Add(new Event
        {
            Id = _eventId,
            Name = "Test Event",
            CreatedByUserId = _creatorId,
            Status = EventStatus.Active,
            CreatedAt = now
        });

        context.EventMembers.AddRange(
            new EventMember { Id = 1, EventId = _eventId, UserId = _member1Id, JoinedAt = now },
            new EventMember { Id = 2, EventId = _eventId, UserId = _member2Id, JoinedAt = now });

        context.Tasks.Add(new TaskEntity
        {
            Id = _taskId,
            EventId = _eventId,
            Title = "Test Task",
            Status = TaskStatusEnum.Available,
            CreatedAt = now
        });

        context.SaveChanges();
    }

    private (DataContext Context, TaskService Service) CreateScope()
    {
        var context = new DataContext(BuildOptions());
        var service = new TaskService(
            new EventRepository(context),
            new EventMemberRepository(context),
            new TaskRepository(context),
            new UserRepository(context),
            Mapper);

        return (context, service);
    }

    [Fact]
    public async System.Threading.Tasks.Task TakeAsync_WhenTaskIsAvailable_AssignsItToCurrentUser()
    {
        var (context, service) = CreateScope();
        using var _ = context;

        var result = await service.TakeAsync(_member1Id, _eventId, _taskId, CancellationToken.None);

        Assert.Equal(TaskStatusEnum.Assigned, result.Status);
        Assert.Equal(_member1Id, result.AssignedToUserId);
        Assert.NotNull(result.AssignedAt);
    }

    [Fact]
    public async System.Threading.Tasks.Task TakeAsync_WhenAlreadyAssigned_ThrowsConflictException()
    {
        var (context1, service1) = CreateScope();
        using (context1)
        {
            await service1.TakeAsync(_member1Id, _eventId, _taskId, CancellationToken.None);
        }

        var (context2, service2) = CreateScope();
        using var _2 = context2;

        await Assert.ThrowsAsync<ConflictException>(
            () => service2.TakeAsync(_member2Id, _eventId, _taskId, CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task TakeAsync_WhenUserIsNotEventMember_ThrowsForbiddenException()
    {
        var (context, service) = CreateScope();
        using var _ = context;

        var strangerId = 999;

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.TakeAsync(strangerId, _eventId, _taskId, CancellationToken.None));
    }

    [Fact]
    public async System.Threading.Tasks.Task TakeAsync_WhenEventIsNotActive_ThrowsConflictException()
    {
        using (var setupContext = new DataContext(BuildOptions()))
        {
            var @event = await setupContext.Events.SingleAsync(e => e.Id == _eventId);
            @event.Status = EventStatus.Draft;
            await setupContext.SaveChangesAsync();
        }

        var (context, service) = CreateScope();
        using var _ = context;

        await Assert.ThrowsAsync<ConflictException>(
            () => service.TakeAsync(_member1Id, _eventId, _taskId, CancellationToken.None));
    }

    /// <summary>
    /// הבדיקה הקריטית ביותר: שני משתמשים מנסים לקחת את אותה Task ממש
    /// באותו הזמן (שני DbContext/Service נפרדים על שני חיבורים נפרדים,
    /// מדמים שני Threads/מופעי API שונים שמריצים Task.WhenAll במקביל).
    /// חייב להצליח בדיוק אחד; השני חייב לקבל ConflictException. אין נעילה
    /// In-Memory כלשהי בקוד - ההגנה מגיעה אך ורק מפקודת ה-UPDATE האטומית
    /// עם ה-WHERE ב-TaskRepository.TryAssignAsync.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task TakeAsync_TwoUsersConcurrently_OnlyOneSucceeds()
    {
        var (context1, service1) = CreateScope();
        var (context2, service2) = CreateScope();
        using var _1 = context1;
        using var _2 = context2;

        var attempt1 = SafeTakeAsync(service1, _member1Id, _eventId, _taskId);
        var attempt2 = SafeTakeAsync(service2, _member2Id, _eventId, _taskId);

        var results = await System.Threading.Tasks.Task.WhenAll(attempt1, attempt2);

        var successes = results.Count(r => r.Succeeded);
        var conflicts = results.Count(r => r.Exception is ConflictException);

        Assert.Equal(1, successes);
        Assert.Equal(1, conflicts);

        // ולידציה ישירות מול ה-Database: בדיוק שורה אחת, משויכת לזוכה בלבד.
        using var verifyContext = new DataContext(BuildOptions());
        var finalTask = await verifyContext.Tasks.SingleAsync(t => t.Id == _taskId);

        Assert.Equal(TaskStatusEnum.Assigned, finalTask.Status);
        Assert.NotNull(finalTask.AssignedToUserId);

        var winnerId = results.Single(r => r.Succeeded).Result!.AssignedToUserId;
        Assert.Equal(winnerId, finalTask.AssignedToUserId);
    }

    private static async Task<(bool Succeeded, TaskDto? Result, Exception? Exception)> SafeTakeAsync(
        TaskService service, int userId, int eventId, int taskId)
    {
        try
        {
            var result = await service.TakeAsync(userId, eventId, taskId, CancellationToken.None);
            return (true, result, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex);
        }
    }

    public void Dispose()
    {
        _keepAliveConnection.Dispose();
    }
}
