using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using projectManager2Core.DTOs;
using projectManager2Core.Services;

namespace projectManager2.Controllers;

/// <summary>
/// יצירת משתמשים עוברת אך ורק דרך AuthController.Register (עם סיסמה, Hash
/// ובחירת Role עצמית) - אין כאן endpoint ליצירה ישירה יותר. שאר הפעולות
/// (GetById/GetAll) פתוחות לכל משתמש מאומת - אין Role בעל גישת-על שרואה
/// דברים שאסורים לאחרים.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(id, cancellationToken);
        return Ok(user);
    }

    /// <summary>
    /// מדופדף - ?pageNumber=1&amp;pageSize=20 (ברירת מחדל). pageSize מכווץ
    /// למקסימום 100 ב-UserService (ראו PaginationHelper).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<UserDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var users = await _userService.GetAllAsync(pageNumber, pageSize, cancellationToken);
        return Ok(users);
    }
}
