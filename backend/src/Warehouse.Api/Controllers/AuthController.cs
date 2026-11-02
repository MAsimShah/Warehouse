using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Warehouse.Api.Contracts;
using Warehouse.Application.Auth;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService auth) : ControllerBase
{
    private const string RefreshCookie = "wh_rt";
    private const string CookiePath = "/api/auth";

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequest? request, CancellationToken ct)
    {
        if (request is null) return MissingBody();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        return ToResponse(await auth.LoginAsync(request.ToCommand(), ct), StatusCodes.Status200OK);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!HasAjaxHeader) return BadRequest();

        var result = await auth.RefreshAsync(Request.Cookies[RefreshCookie], ct);
        if (!result.IsSuccess) Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = CookiePath });
        return ToResponse(result, StatusCodes.Status200OK);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!HasAjaxHeader) return BadRequest();

        await auth.LogoutAsync(Request.Cookies[RefreshCookie], ct);
        Response.Cookies.Delete(RefreshCookie, new CookieOptions { Path = CookiePath });
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirst("sub")?.Value,
        name = User.FindFirst("name")?.Value,
        email = User.FindFirst("email")?.Value,
        isSuperAdmin = User.FindFirst("is_super_admin")?.Value == "true"
    });

    private IActionResult MissingBody() => Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid request",
        detail: "A request body is required.");

    // Custom header forces a CORS preflight, so other sites cannot call cookie endpoints (CSRF defence).
    private bool HasAjaxHeader => Request.Headers["X-Requested-With"] == "XMLHttpRequest";

    private IActionResult ToResponse(Result<AuthTokens> result, int successStatus)
    {
        switch (result.Status)
        {
            case ResultStatus.Success:
                var tokens = result.Value!;
                Response.Cookies.Append(RefreshCookie, tokens.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = CookiePath,
                    IsEssential = true,
                    // Persistent cookie only when the user chose "Keep me signed in".
                    // Without Expires it is a session cookie and ends when the browser closes.
                    Expires = tokens.RefreshTokenIsPersistent
                        ? new DateTimeOffset(DateTime.SpecifyKind(tokens.RefreshTokenExpiresAtUtc, DateTimeKind.Utc))
                        : null
                });
                return StatusCode(successStatus,
                    new AuthResponse(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.User));

            case ResultStatus.ValidationFailed:
                return ValidationProblem(new ValidationProblemDetails(
                    result.Errors.ToDictionary(e => e.Key, e => e.Value)));

            case ResultStatus.Conflict:
                return Problem(statusCode: StatusCodes.Status409Conflict,
                    title: "Unable to create account",
                    detail: "An account with these details cannot be created.");

            default:
                return Problem(statusCode: StatusCodes.Status401Unauthorized,
                    title: "Authentication failed",
                    detail: "Invalid credentials.");
        }
    }
}
