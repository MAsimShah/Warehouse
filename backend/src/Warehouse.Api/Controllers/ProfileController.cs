using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Warehouse.Api.Contracts;
using Warehouse.Application.Auth;
using Warehouse.Application.Profile;

namespace Warehouse.Api.Controllers;

// The signed-in user's own profile. Any logged-in user. The user id always comes from the token, never from the request,
// and there is no role or status here: only the Super Admin changes those (Employees page).
[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController(IProfileService profile) : ControllerBase
{
    // Photos are small data URLs, so the global 64 KB body limit is raised for the endpoint that accepts them.
    private const int MaxPhotoBodyBytes = 300_000;

    private Guid UserId => Guid.Parse(User.FindFirst("sub")!.Value);

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) =>
        ToResponse(await profile.GetAsync(UserId, ct));

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest? request, CancellationToken ct)
    {
        if (request is null) return MissingBody();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        return ToResponse(await profile.UpdateAsync(UserId, request.ToCommand(), ct));
    }

    [HttpPut("photo")]
    [RequestSizeLimit(MaxPhotoBodyBytes)]
    public async Task<IActionResult> SetPhoto(SetPhotoRequest? request, CancellationToken ct)
    {
        if (request is null) return MissingBody();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        return ToResponse(await profile.SetPhotoAsync(UserId, request.ProfilePhoto, ct));
    }

    // Rate limited like login, because it checks a password.
    [HttpPost("password")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest? request, CancellationToken ct)
    {
        if (request is null) return MissingBody();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var result = await profile.ChangePasswordAsync(UserId, request.CurrentPassword, request.NewPassword, ct);
        return result.Status switch
        {
            ResultStatus.Success => NoContent(),
            ResultStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
                result.Errors.ToDictionary(e => e.Key, e => e.Value))),
            _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found")
        };
    }

    private IActionResult MissingBody() => Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid request",
        detail: "A request body is required.");

    private IActionResult ToResponse(Result<ProfileDto> result) => result.Status switch
    {
        ResultStatus.Success => Ok(result.Value),
        ResultStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
            result.Errors.ToDictionary(e => e.Key, e => e.Value))),
        _ => Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found")
    };
}
