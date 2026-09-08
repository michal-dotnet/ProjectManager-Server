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
using User = projectManager2Core.Models.User;

namespace projectManager2Tests;

/// <summary>
/// בדיקות יחידה (Unit) ל-EventMemberService עם Moq - ראו הסבר כללי ב-
/// EventServiceTests. שימו לב ל-Times.Never על SaveChangesAsync בכל
/// בדיקת כישלון: מוודאים לא רק שנזרקת החריגה הנכונה, אלא גם שלא נשמר
/// שום שינוי חלקי/שגוי ל-Database.
/// </summary>
public class EventMemberServiceTests
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

    private static User CreateUser(int id) => new()
    {
        Id = id,
        Name = $"User{id}",
        Email = $"user{id}@test.local",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task AddAsync_WhenCurrentUserIsNotCreator_ThrowsForbiddenException_AndDoesNotSave()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var memberRepo = new Mock<IEventMemberRepository>();
        var service = new EventMemberService(eventRepo.Object, memberRepo.Object, new Mock<IUserRepository>().Object, Mapper);

        var dto = new AddEventMemberDto { UserId = 5 };

        await Assert.ThrowsAsync<ForbiddenException>(() => service.AddAsync(currentUserId: 2, eventId: 1, dto));

        memberRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenUserAlreadyMember_ThrowsConflictException_AndDoesNotSave()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);
        var targetUser = CreateUser(5);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var memberRepo = new Mock<IEventMemberRepository>();
        memberRepo.Setup(r => r.IsMemberAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);

        var service = new EventMemberService(eventRepo.Object, memberRepo.Object, userRepo.Object, Mapper);

        var dto = new AddEventMemberDto { UserId = 5 };

        await Assert.ThrowsAsync<ConflictException>(() => service.AddAsync(currentUserId: 1, eventId: 1, dto));

        memberRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAsync_WhenTargetUserDoesNotExist_ThrowsNotFoundException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = new EventMemberService(eventRepo.Object, new Mock<IEventMemberRepository>().Object, userRepo.Object, Mapper);

        var dto = new AddEventMemberDto { UserId = 5 };

        await Assert.ThrowsAsync<NotFoundException>(() => service.AddAsync(currentUserId: 1, eventId: 1, dto));
    }

    [Fact]
    public async Task AddAsync_WhenEventCompleted_ThrowsConflictException()
    {
        var @event = CreateEvent(id: 1, creatorId: 1, status: EventStatus.Completed);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var service = new EventMemberService(eventRepo.Object, new Mock<IEventMemberRepository>().Object, new Mock<IUserRepository>().Object, Mapper);

        var dto = new AddEventMemberDto { UserId = 5 };

        await Assert.ThrowsAsync<ConflictException>(() => service.AddAsync(currentUserId: 1, eventId: 1, dto));
    }

    [Fact]
    public async Task AddAsync_WhenValid_AddsMember_AndReturnsMappedDto()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);
        var targetUser = CreateUser(5);

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var memberRepo = new Mock<IEventMemberRepository>();
        memberRepo.Setup(r => r.IsMemberAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(targetUser);

        var service = new EventMemberService(eventRepo.Object, memberRepo.Object, userRepo.Object, Mapper);

        var dto = new AddEventMemberDto { UserId = 5 };
        var result = await service.AddAsync(currentUserId: 1, eventId: 1, dto);

        Assert.Equal(5, result.UserId);
        Assert.Equal(targetUser.Name, result.UserName);
        Assert.Equal(targetUser.Email, result.UserEmail);
        memberRepo.Verify(r => r.AddAsync(It.IsAny<EventMember>(), It.IsAny<CancellationToken>()), Times.Once);
        memberRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_WhenMemberHasAssignedTask_ThrowsConflictException_AndDoesNotRemove()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);
        var member = new EventMember { Id = 1, EventId = 1, UserId = 5, JoinedAt = DateTime.UtcNow };

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var memberRepo = new Mock<IEventMemberRepository>();
        memberRepo.Setup(r => r.GetAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        memberRepo.Setup(r => r.HasAssignedTaskAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var service = new EventMemberService(eventRepo.Object, memberRepo.Object, new Mock<IUserRepository>().Object, Mapper);

        await Assert.ThrowsAsync<ConflictException>(() => service.RemoveAsync(currentUserId: 1, eventId: 1, userId: 5));

        memberRepo.Verify(r => r.Remove(It.IsAny<EventMember>()), Times.Never);
        memberRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAsync_WhenValid_RemovesMember()
    {
        var @event = CreateEvent(id: 1, creatorId: 1);
        var member = new EventMember { Id = 1, EventId = 1, UserId = 5, JoinedAt = DateTime.UtcNow };

        var eventRepo = new Mock<IEventRepository>();
        eventRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var memberRepo = new Mock<IEventMemberRepository>();
        memberRepo.Setup(r => r.GetAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        memberRepo.Setup(r => r.HasAssignedTaskAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var service = new EventMemberService(eventRepo.Object, memberRepo.Object, new Mock<IUserRepository>().Object, Mapper);

        await service.RemoveAsync(currentUserId: 1, eventId: 1, userId: 5);

        memberRepo.Verify(r => r.Remove(member), Times.Once);
        memberRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
