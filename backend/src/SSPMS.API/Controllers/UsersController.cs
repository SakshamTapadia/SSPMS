using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.DTOs.Users;
using SSPMS.Application.Interfaces;
using SSPMS.Domain.Enums;

namespace SSPMS.API.Controllers;

public class ImportRequest { public IFormFile File { get; set; } = null!; }

[Route("api/v1/users")]
[Authorize]
public class UsersController : BaseController
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] UserRole? role = null, [FromQuery] bool? isActive = null)
        => Ok(await _users.GetUsersAsync(page, pageSize, search, role, isActive));

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _users.GetByIdAsync(CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : NotFound();
    }

    [HttpGet("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _users.GetByIdAsync(id);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var result = await _users.CreateUserAsync(request, CurrentUserId);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("trainer/employees"), Authorize(Roles = "Trainer")]
    public async Task<IActionResult> CreateByTrainer([FromBody] CreateUserRequest request)
    {
        var result = await _users.CreateEmployeeByTrainerAsync(request, CurrentUserId);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
    {
        var result = await _users.UpdateUserAsync(id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
    {
        var result = await _users.UpdateUserAsync(CurrentUserId, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}/role"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest request)
    {
        var result = await _users.ChangeRoleAsync(id, request.Role);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}/deactivate"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _users.DeactivateUserAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}/reactivate"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var result = await _users.ReactivateUserAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("import"), Authorize(Roles = "Admin,Trainer"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Import([FromForm] ImportRequest request, [FromQuery] Guid classId)
    {
        if (request.File == null || request.File.Length == 0) return BadRequest(new { message = "No file uploaded." });
        using var stream = request.File.OpenReadStream();
        var result = await _users.BulkImportAsync(stream, classId, CurrentUserId);
        return result.Succeeded ? Ok(new { result.Data.success, result.Data.errors }) : BadRequest(new { message = result.Error });
    }
}
