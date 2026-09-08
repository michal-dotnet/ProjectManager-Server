using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using projectManager2Core.Enums;
using projectManager2Core.Models;
using projectManager2Data;

namespace projectManager2Service;

/// <summary>
/// Seed Data שרץ בכל עליית האפליקציה (ראו הקריאה ב-Program.cs), ולא דרך
/// EF Core Migrations HasData - כי HasData דורש ערכים סטטיים קבועים מראש,
/// ולא ניתן לחשב איתו Hash אמיתי של סיסמה (PasswordHasher מייצר Salt
/// אקראי בכל הרצה). כך משתמשי ה-Seed מקבלים Hash תקין שנוצר באותו קוד
/// בדיוק שמשמש בהרשמה/התחברות רגילה (AuthService), לא הדמיה.
///
/// משתמש דמו אחד לכל Role (Manager/Worker) - אין יותר משתמש "Admin", כי
/// אין ב-Role זה גישת-על במערכת.
///
/// אידמפוטנטי לגמרי: אם כבר יש ולו משתמש אחד ב-Database, לא עושה כלום -
/// כך שאפשר להריץ את האפליקציה שוב ושוב בלי ליצור כפילויות.
/// </summary>
public static class DataSeeder
{
    public const string ManagerEmail = "manager@projectmanager2.local";
    public const string ManagerPassword = "Manager@12345";

    public const string WorkerEmail = "worker@projectmanager2.local";
    public const string WorkerPassword = "Worker@12345";

    public static async System.Threading.Tasks.Task EnsureSeededAsync(DataContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var hasher = new PasswordHasher<User>();
        var now = DateTime.UtcNow;

        var manager = new User
        {
            Name = "מנהלת אירוע (Manager)",
            Email = ManagerEmail,
            Role = UserRole.Manager,
            IsActive = true,
            CreatedAt = now
        };
        manager.PasswordHash = hasher.HashPassword(manager, ManagerPassword);

        var worker = new User
        {
            Name = "עובד הדגמה (Worker)",
            Email = WorkerEmail,
            Role = UserRole.Worker,
            IsActive = true,
            CreatedAt = now
        };
        worker.PasswordHash = hasher.HashPassword(worker, WorkerPassword);

        context.Users.AddRange(manager, worker);
        await context.SaveChangesAsync();

        var demoEvent = new Event
        {
            Name = "כנס הדגמה",
            Description = "Event לדוגמה שנוצר אוטומטית ע\"י Seed Data, להדגמת המערכת מיד לאחר הקמת ה-Database.",
            CreatedByUserId = manager.Id,
            StartDate = now.AddDays(7),
            EndDate = now.AddDays(8),
            Status = EventStatus.Active,
            CreatedAt = now
        };

        context.Events.Add(demoEvent);
        await context.SaveChangesAsync();

        context.EventMembers.Add(new EventMember
        {
            EventId = demoEvent.Id,
            UserId = worker.Id,
            JoinedAt = now
        });

        context.Tasks.AddRange(
            new TaskEntity
            {
                EventId = demoEvent.Id,
                Title = "הכנת עמדת רישום",
                Description = "משימה פנויה - מיועדת להדגמת לקיחת Task (POST .../assignment).",
                Status = TaskStatusEnum.Available,
                CreatedAt = now
            },
            new TaskEntity
            {
                EventId = demoEvent.Id,
                Title = "סידור ציוד הגברה",
                Status = TaskStatusEnum.Assigned,
                AssignedToUserId = worker.Id,
                AssignedAt = now,
                CreatedAt = now
            });

        await context.SaveChangesAsync();
    }
}
