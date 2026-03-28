using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.DTOs.Tasks;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/tasks")]
[Authorize]
public class TasksController : BaseController
{
    private readonly ITaskService _tasks;
    public TasksController(ITaskService tasks) => _tasks = tasks;

    [HttpGet, Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? classId = null)
        => Ok(await _tasks.GetTasksAsync(CurrentUserId, classId));

    [HttpGet("me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMyTasks()
        => Ok(await _tasks.GetMyTasksAsync(CurrentUserId));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _tasks.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    [HttpPost, Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var result = await _tasks.CreateTaskAsync(request, CurrentUserId);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var result = await _tasks.UpdateTaskAsync(id, request, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _tasks.DeleteTaskAsync(id, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{id:guid}/publish"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Publish(Guid id)
    {
        var result = await _tasks.PublishTaskAsync(id, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/duplicate"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var result = await _tasks.DuplicateTaskAsync(id, CurrentUserId);
        return result.Succeeded ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data) : BadRequest(new { message = result.Error });
    }

    // Questions sub-resource
    [HttpGet("{taskId:guid}/questions")]
    public async Task<IActionResult> GetQuestions(Guid taskId, [FromQuery] bool includeAnswers = false)
    {
        var isTrainer = CurrentUserRole is "Trainer" or "Admin";
        return Ok(await _tasks.GetQuestionsAsync(taskId, isTrainer || includeAnswers));
    }

    [HttpPost("{taskId:guid}/questions"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> AddQuestion(Guid taskId, [FromBody] CreateQuestionRequest request)
    {
        var result = await _tasks.AddQuestionAsync(taskId, request, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{taskId:guid}/questions/{questionId:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> UpdateQuestion(Guid taskId, Guid questionId, [FromBody] CreateQuestionRequest request)
    {
        var result = await _tasks.UpdateQuestionAsync(taskId, questionId, request, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{taskId:guid}/questions/{questionId:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> DeleteQuestion(Guid taskId, Guid questionId)
    {
        var result = await _tasks.DeleteQuestionAsync(taskId, questionId, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPatch("{taskId:guid}/questions/reorder"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> Reorder(Guid taskId, [FromBody] ReorderRequest request)
    {
        var result = await _tasks.ReorderQuestionsAsync(taskId, request.QuestionIds, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("{taskId:guid}/questions/import"), Authorize(Roles = "Admin,Trainer"), Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportQuestions(Guid taskId, IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file uploaded." });
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf" && ext != ".docx") return BadRequest(new { message = "Only PDF and DOCX files are supported." });
        using var stream = file.OpenReadStream();
        var result = await _tasks.ImportQuestionsFromDocumentAsync(taskId, stream, file.FileName, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }
}
