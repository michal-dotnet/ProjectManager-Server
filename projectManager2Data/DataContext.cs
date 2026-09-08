using Microsoft.EntityFrameworkCore;
using projectManager2Core.Models;

namespace projectManager2Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventMember> EventMembers => Set<EventMember>();

    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ---------- User ----------
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(200);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(320);

            // Unique Index על User.Email.
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // ---------- Event ----------
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(2000);

            // User 1:N Event דרך CreatedByUserId. אין לאפשר מחיקת User
            // שקיימים לו Events - Restrict ולא Cascade.
            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.CreatedEvents)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- EventMember ----------
        modelBuilder.Entity<EventMember>(entity =>
        {
            entity.HasKey(m => m.Id);

            // Unique Index על EventMember(EventId, UserId) - משתמש לא יכול
            // להיות חבר פעמיים באותו Event.
            entity.HasIndex(m => new { m.EventId, m.UserId }).IsUnique();

            // Event 1:N EventMember. מחיקת Event שלם אמורה לנקות גם את
            // חברי ה-Event (לא נוגעת ב-User עצמו) - Cascade כאן בטוח.
            entity.HasOne(m => m.Event)
                .WithMany(e => e.Members)
                .HasForeignKey(m => m.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // User 1:N EventMember. נמנעים מ-Cascade כדי לא לאפשר מחיקת
            // User "בשקט" דרך מחיקת שורות חברות התלויות בו.
            entity.HasOne(m => m.User)
                .WithMany(u => u.EventMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Task ----------
        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
            entity.Property(t => t.Description).HasMaxLength(2000);

            // Concurrency Token (נקודה 6) - ראו הסבר מלא ב-Models/Task.cs.
            // בכוונה בלי ValueGeneratedOnAddOrUpdate/IsRowVersion - העמודה
            // מוגדלת ידנית באפליקציה (TaskRepository.SaveChangesAsync) ולא
            // ע"י ה-Database, כדי לעבוד זהה מול SQL Server ומול Sqlite.
            entity.Property(t => t.ConcurrencyVersion).IsConcurrencyToken();

            // Index על Task(EventId, Status) - שאילתות נפוצות כמו "כל
            // ה-Tasks הפנויות של Event מסוים".
            entity.HasIndex(t => new { t.EventId, t.Status });

            // Index על Task(AssignedToUserId) - "אילו Tasks המשתמש מחזיק".
            entity.HasIndex(t => t.AssignedToUserId);

            // Event 1:N Task. מחיקת Event שלם מנקה גם את המשימות שלו.
            entity.HasOne(t => t.Event)
                .WithMany(e => e.Tasks)
                .HasForeignKey(t => t.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            // User 1:N Task דרך AssignedToUserId. אין לאפשר מחיקת User
            // שיש לו Tasks משויכות - Restrict/NoAction ולא Cascade, כדי
            // להימנע ממחיקות שרשרת לא רצויות.
            entity.HasOne(t => t.AssignedToUser)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
