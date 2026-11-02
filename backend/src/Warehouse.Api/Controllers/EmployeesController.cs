using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Api.Contracts;
using Warehouse.Application.Auth;
using Warehouse.Application.Employees;

namespace Warehouse.Api.Controllers;

// Super Admin only (same policy as the admin area).
[ApiController]
[Route("api/employees")]
[Authorize(Policy = "AdminArea")]
public sealed class EmployeesController(IEmployeeService employees) : ControllerBase
{
    // Photos are small data URLs, so the global 64 KB body limit is raised for the endpoints that accept them.
    private const int MaxBodyBytes = 300_000;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct) =>
        Ok(await employees.ListAsync(search?.Length > 100 ? search[..100] : search, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        ToResponse(await employees.GetAsync(id, ct), StatusCodes.Status200OK);

    [HttpPost]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> Create(SaveEmployeeRequest? request, CancellationToken ct)
    {
        if (request is null) return MissingBody();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        return ToResponse(await employees.CreateAsync(request.ToCommand(), ct), StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(MaxBodyBytes)]
    public async Task<IActionResult> Update(Guid id, SaveEmployeeRequest? request, CancellationToken ct)
    {
        if (request is null) return MissingBody();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        return ToResponse(await employees.UpdateAsync(id, request.ToCommand(), ct), StatusCodes.Status200OK);
    }

    // With a login: the login becomes Inactive (record and history are kept). Record-only employee: the record is removed.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct) =>
        ToResponse(await employees.DeactivateAsync(id, ct), StatusCodes.Status200OK);

    private IActionResult MissingBody() => Problem(
        statusCode: StatusCodes.Status400BadRequest,
        title: "Invalid request",
        detail: "A request body is required.");

    private IActionResult ToResponse(Result<EmployeeDto> result, int successStatus) => result.Status switch
    {
        ResultStatus.Success => StatusCode(successStatus, result.Value),
        ResultStatus.ValidationFailed => ValidationProblem(new ValidationProblemDetails(
            result.Errors.ToDictionary(e => e.Key, e => e.Value))),
        ResultStatus.NotFound => Problem(statusCode: StatusCodes.Status404NotFound, title: "Not found"),
        _ => Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid request")
    };
}
