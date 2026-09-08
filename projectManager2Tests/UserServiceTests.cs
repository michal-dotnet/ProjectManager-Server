using AutoMapper;
using Moq;
using projectManager2Core.Exceptions;
using projectManager2Core.Repositories;
using projectManager2Service;
using projectManager2Service.Mapping;
using Xunit;
using User = projectManager2Core.Models.User;

namespace projectManager2Tests;

/// <summary>
/// בדיקות יחידה (Unit) ל-UserService עם Moq - ראו הסבר כללי ב-
/// EventServiceTests.
/// </summary>
public class UserServiceTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

    [Fact]
    public async Task GetByIdAsync_WhenUserNotFound_ThrowsNotFoundException()
    {
        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var service = new UserService(repo.Object, Mapper);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(99));
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsMappedDto()
    {
        var user = new User
        {
            Id = 1,
            Name = "מיכל",
            Email = "michal@test.local",
            IsActive = true,
            Role = projectManager2Core.Enums.UserRole.Worker,
            CreatedAt = DateTime.UtcNow
        };

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = new UserService(repo.Object, Mapper);

        var dto = await service.GetByIdAsync(1);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Name, dto.Name);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.Role, dto.Role);
    }

    [Fact]
    public async Task GetAllAsync_NormalizesPagingAndReturnsMappedItems()
    {
        var users = new List<User>
        {
            new() { Id = 1, Name = "A", Email = "a@test.local", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "B", Email = "b@test.local", IsActive = true, CreatedAt = DateTime.UtcNow }
        };

        var repo = new Mock<IUserRepository>();
        repo.Setup(r => r.GetAllAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, users.Count));

        var service = new UserService(repo.Object, Mapper);

        // pageNumber/pageSize לא תקינים (0 ושלילי) - PaginationHelper אמור
        // לנרמל אותם ל-1/20 לפני שהם מגיעים ל-Repository.
        var result = await service.GetAllAsync(pageNumber: 0, pageSize: -5);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(2, result.TotalCount);
    }
}
