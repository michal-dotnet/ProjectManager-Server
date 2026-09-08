using AutoMapper;
using Moq;
using projectManager2Core.DTOs;
using projectManager2Core.Enums;
using projectManager2Core.Exceptions;
using projectManager2Core.Repositories;
using projectManager2Service;
using projectManager2Service.Mapping;
using Xunit;
using Event = projectManager2Core.Models.Event;

namespace projectManager2Tests;

/// <summary>
/// בדיקות יחידה (Unit) עם Moq לחוקי הרשאה/מצב פשוטים ב-TaskService
/// (CreateAsync/UpdateAsync) - לא קשור ל-Race Condition. בדיקות ה-
/// Concurrency האמיתיות (TakeAsync מול שני משתמשים בו-זמנית) נמצאות ב-
/// TaskServiceConcurrencyTests מול Sqlite אמיתי: שם Moq לא מתאים כי צריך
/// Database אמיתי כדי לבדוק שה-Concurrency Token אכן עובד ברמת ה-SQL בפועל.
/// </summary>
public class TaskServiceTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    private static Event CreateEvent(int id, int creatorId, EventStatus status = EventStatus.Active) => new()
    {
        Id = id,
        Name = "Test Event",
        CreatedByUserId = creatorId,
        Status = status,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_WhenCurrentUserIsNotCreator_ThrowsForbiddenException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new TaskService(
            eventRepo.Object,
            new Mock<IEventMemberRepository>().Object,
            new Mock<ITaskRepository>().Object,
            new Mock<IUserRepository>().Object,
            Mapper);

        var dto = new CreateTaskDto { Title = "משימה" };

        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(currentUserId: 2, eventId: 1, dto));
    }

    [Fact]
    public async Task CreateAsync_WhenEventCompleted_ThrowsConflictException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1, status: EventStatus.Completed);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new TaskService(
            eventRepo.Object,
            new Mock<IEventMemberRepository>().Object,
            new Mock<ITaskRepository>().Object,
            new Mock<IUserRepository>().Object,
            Mapper);

        var dto = new CreateTaskDto { Title = "משימה" };

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(currentUserId: 1, eventId: 1, dto));
    }

    [Fact]
    public async Task CreateAsync_WhenValid_AddsTaskAsAvailable()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var taskRepo = new Mock<ITaskRepository>();
        TaskEntity? added = null;
        taskRepo.Setup(r => r.AddAsync(It.IsAny<TaskEntity>(), It.IsAny<CancellationToken>()))
            .Callback<TaskEntity, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);

        var service = new TaskService(
            eventRepo.Object,
            new Mock<IEventMemberRepository>().Object,
            taskRepo.Object,
            new Mock<IUserRepository>().Object,
            Mapper);

        var dto = new CreateTaskDto { Title = "הכנת עמדה" };
        var result = await service.CreateAsync(currentUserId: 1, eventId: 1, dto);

        Assert.Equal(TaskStatusEnum.Available, result.Status);
        Assert.NotNull(added);
        Assert.Equal(TaskStatusEnum.Available, added!.Status);
        taskRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenCurrentUserIsNotCreator_ThrowsForbiddenException_AndDoesNotSave()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);
        var task = new TaskEntity { Id = 1, EventId = 1, Title = "ישן", Status = TaskStatusEnum.Available, CreatedAt = DateTime.UtcNow };

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var service = new TaskService(
            eventRepo.Object,
            new Mock<IEventMemberRepository>().Object,
            taskRepo.Object,
            new Mock<IUserRepository>().Object,
            Mapper);

        var dto = new UpdateTaskDto { Title = "חדש" };

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(currentUserId: 2, eventId: 1, taskId: 1, dto));

        taskRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEventCompleted_ThrowsConflictException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1, status: EventStatus.Completed);
        var task = new TaskEntity { Id = 1, EventId = 1, Title = "ישן", Status = TaskStatusEnum.Available, CreatedAt = DateTime.UtcNow };

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.GetByIdAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(task);

        var service = new TaskService(
            eventRepo.Object,
            new Mock<IEventMemberRepository>().Object,
            taskRepo.Object,
            new Mock<IUserRepository>().Object,
            Mapper);

        var dto = new UpdateTaskDto { Title = "חדש" };

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(currentUserId: 1, eventId: 1, taskId: 1, dto));
    }

    [Fact]
    public async Task UpdateAsync_WhenTaskNotFound_ThrowsNotFoundException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var taskRepo = new Mock<ITaskRepository>();
        taskRepo.Setup(r => r.GetByIdAsync(1, 999, It.IsAny<CancellationToken>())).ReturnsAsync((TaskEntity?)null);

        var service = new TaskService(
            eventRepo.Object,
            new Mock<IEventMemberRepository>().Object,
            taskRepo.Object,
            new Mock<IUserRepository>().Object,
            Mapper);

        var dto = new UpdateTaskDto { Title = "חדש" };

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(currentUserId: 1, eventId: 1, taskId: 999, dto));
    }
}
