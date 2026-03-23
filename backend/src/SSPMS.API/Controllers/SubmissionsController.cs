using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SSPMS.Application.DTOs.Submissions;
using SSPMS.Application.Interfaces;

namespace SSPMS.API.Controllers;

[Route("api/v1/submissions")]
[Authorize]
public class SubmissionsController : BaseController
{
    private readonly ISubmissionService _submissions;
    public SubmissionsController(ISubmissionService submissions) => _submissions = submissions;

    [HttpPost, Authorize(Roles = "Employee")]
    public async Task<IActionResult> Start([FromBody] StartSubmissionRequest request)
    {
        var result = await _submissions.StartSubmissionAsync(request.TaskId, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _submissions.GetByIdAsync(id, CurrentUserId, CurrentUserRole);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    [HttpPut("{id:guid}/draft"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> SaveDraft(Guid id, [FromBody] SaveDraftRequest request)
    {
        var result = await _submissions.SaveDraftAsync(id, request, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/submit"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> Submit(Guid id)
    {
        var result = await _submissions.SubmitAsync(id, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:guid}/malpractice-submit"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> MalpracticeSubmit(Guid id, [FromBody] MalpracticeSubmitRequest request)
    {
        var result = await _submissions.MalpracticeSubmitAsync(id, CurrentUserId, request.TabSwitchCount);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpGet("task/{taskId:guid}"), Authorize(Roles = "Admin,Trainer")]
    public async Task<IActionResult> GetTaskSubmissions(Guid taskId)
        => Ok(await _submissions.GetTaskSubmissionsAsync(taskId, CurrentUserId));

    [HttpGet("task/{taskId:guid}/me"), Authorize(Roles = "Employee")]
    public async Task<IActionResult> GetMySubmission(Guid taskId)
    {
        var result = await _submissions.GetMySubmissionAsync(taskId, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : NotFound(new { message = result.Error });
    }

    [HttpPut("{id:guid}/evaluate"), Authorize(Roles = "Trainer")]
    public async Task<IActionResult> Evaluate(Guid id, [FromBody] EvaluateSubmissionRequest request)
    {
        var result = await _submissions.EvaluateAsync(id, request, CurrentUserId);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("answers/{answerId:guid}/plagiarism"), Authorize(Roles = "Trainer")]
    public async Task<IActionResult> SetPlagiarism(Guid answerId, [FromBody] SetPlagiarismRequest request)
    {
        var result = await _submissions.SetPlagiarismFlagAsync(answerId, request.Flag, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }

    [HttpPost("task/{taskId:guid}/bulk-complete"), Authorize(Roles = "Trainer")]
    public async Task<IActionResult> BulkComplete(Guid taskId)
    {
        var result = await _submissions.BulkCompleteEvaluationAsync(taskId, CurrentUserId);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
