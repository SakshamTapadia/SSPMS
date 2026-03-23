using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.DTOs.Classes;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/classes")]
[Authorize]
public class ClassesController : BaseController
{
    private readonly IClassService _classes;
    public ClassesController(IClassService classes) => _classes = classes;

    [HttpGet, Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetAll()
    {
        var trainerId = CurrentUserRole == "Trainer" ? CurrentUserId : (Guid?)null;
        return Ok(await _classes.GetClassesAsync(trainerId));
    }

    [HttpGet("me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyClass()
    {
        var result = await _classes.GetMyClassAsync(CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    [HttpGet("{id:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _classes.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var result = await _classes.CreateClassAsync(request);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClassRequest request)
    {
        var result = await _classes.UpdateClassAsync(id, request);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}/archive"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Archive(Guid id)
    {
        var result = await _classes.ArchiveClassAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _classes.DeleteClassAsync(id);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}/employees"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetEmployees(Guid id)
        => Ok(await _classes.GetEnrolledEmployeesAsync(id));

    [HttpPost("{id:guid}/enroll"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Enroll(Guid id, [FromBody] EnrollRequest request)
    {
        var result = await _classes.EnrollEmployeeAsync(id, request.EmployeeId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/transfer"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Transfer(Guid id, [FromBody] TransferRequest request)
    {
        var result = await _classes.TransferEmployeeAsync(request.EmployeeId, request.TargetClassId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}/employees/{employeeId:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> RemoveEmployee(Guid id, Guid employeeId)
    {
        var result = await _classes.RemoveEmployeeAsync(id, employeeId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
