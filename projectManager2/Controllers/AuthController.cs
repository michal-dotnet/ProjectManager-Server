using Microsoft.AspNetCore.Mvc;
using projectManager2Core.DTOs;
using projectManager2Core.Services;

namespace projectManager2.Controllers;

/// <summary>
/// נקודות הכניסה היחידות ליצירת משתמש חדש ולהתחברות - בכוונה בלי
/// [Authorize] (אין עדיין Token בשלב הזה). כל שאר ה-Controllers במערכת
/// דורשים JWT תקין.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(dto, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(dto, cancellationToken);
        return Ok(result);
    }
}
