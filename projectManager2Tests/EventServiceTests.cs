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
using EventMember = projectManager2Core.Models.EventMember;

namespace projectManager2Tests;

/// <summary>
/// בדיקות יחידה (Unit) ל-EventService עם Moq: מדמים את IEventRepository
/// (בלי Database אמיתי בכלל) ובודקים רק את הלוגיקה העסקית הטהורה של
/// ה-Service (הרשאות/מצב/מיפוי). בניגוד ל-TaskServiceConcurrencyTests
/// (מריץ מול Sqlite אמיתי לבדיקת Race Condition) - כאן מהירות ובידוד מלאים.
/// </summary>
public class EventServiceTests
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
    public async Task CreateAsync_SetsStatusToActive()
    {
        var repo = new Mock<IEventRepository>();
        Event? added = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Callback<Event, CancellationToken>((e, _) => added = e)
            .Returns(Task.CompletedTask);

        var service = new EventService(repo.Object, Mapper);

        var dto = await service.CreateAsync(currentUserId: 1, new CreateEventDto { Name = "כנס" });

        Assert.Equal(EventStatus.Active, dto.Status);
        Assert.NotNull(added);
        Assert.Equal(EventStatus.Active, added!.Status);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        var service = new EventService(repo.Object, Mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(currentUserId: 1, eventId: 99));
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsNeitherCreatorNorMember_ThrowsForbiddenException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var service = new EventService(repo.Object, Mapper);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(currentUserId: 999, eventId: 1));
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserIsMember_Succeeds()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);
        @event.Members.Add(new EventMember { Id = 1, EventId = 1, UserId = 5, JoinedAt = DateTime.UtcNow });

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdWithDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var service = new EventService(repo.Object, Mapper);

        var dto = await service.GetByIdAsync(currentUserId: 5, eventId: 1);

        Assert.Equal(1, dto.Id);
    }

    [Fact]
    public async Task UpdateAsync_WhenCurrentUserIsNotCreator_ThrowsForbiddenException_AndDoesNotSave()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new EventService(repo.Object, Mapper);

        var dto = new UpdateEventDto { Name = "שם חדש" };

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(currentUserId: 2, eventId: 1, dto));

        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEventCompleted_ThrowsConflictException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1, status: EventStatus.Completed);

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new EventService(repo.Object, Mapper);

        var dto = new UpdateEventDto { Name = "שם חדש" };

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(currentUserId: 1, eventId: 1, dto));
    }

    [Fact]
    public async Task CompleteAsync_WhenAlreadyCompleted_ThrowsConflictException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1, status: EventStatus.Completed);

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new EventService(repo.Object, Mapper);

        await Assert.ThrowsAsync<ConflictException>(() => service.CompleteAsync(currentUserId: 1, eventId: 1));
    }

    [Fact]
    public async Task CompleteAsync_WhenCreatorAndActive_SetsStatusCompleted()
    {
        var @event = CreateEvent(id: 1, creatorId: 1, status: EventStatus.Active);

        var repo = new Mock<IEventRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new EventService(repo.Object, Mapper);

        var dto = await service.CompleteAsync(currentUserId: 1, eventId: 1);

        Assert.Equal(EventStatus.Completed, dto.Status);
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
