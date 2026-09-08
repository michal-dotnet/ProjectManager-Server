using AutoMapper;
using projectManager2Core.Common;
using projectManager2Core.DTOs;
using projectManager2Core.Exceptions;
using projectManager2Core.Models;
using projectManager2Core.Repositories;
using projectManager2Core.Services;

namespace projectManager2Service;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public UserService(IUserRepository userRepository, IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<UserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For(nameof(User), id);

        return ToDto(user);
    }

    public async Task<PagedResultDto<UserDto>> GetAllAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPageNumber, normalizedPageSize) = PaginationHelper.Normalize(pageNumber, pageSize);

        var (users, totalCount) = await _userRepository.GetAllAsync(normalizedPageNumber, normalizedPageSize, cancellationToken);

        return new PagedResultDto<UserDto>
        {
            Items = users.Select(ToDto).ToList(),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            TotalCount = totalCount
        };
    }

    private UserDto ToDto(User user) => _mapper.Map<UserDto>(user);
}
