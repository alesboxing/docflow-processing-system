using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocFlow.Api.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { code = "Auth.InvalidRequest", message = "UserName and Password are required." });

        var response = _auth.Login(request);

        if (response is null)
            return Unauthorized(new { code = "Auth.InvalidCredentials", message = "Invalid username or password." });

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize(Policy = DocFlowPolicies.DocumentUser)]
    public IActionResult Me()
    {
        return Ok(new
        {
            userName = User.Identity?.Name,
            role = User.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.Role)?.Value
        });
    }
}
