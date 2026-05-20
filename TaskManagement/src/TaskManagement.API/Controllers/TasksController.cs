using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.API.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>Creates a new task for the authenticated user.</summary>
    /// <remarks>
    /// Business rules:
    /// - Duplicate task titles are rejected for the same user on the same day.
    /// - Task is queued for background processing automatically.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.CreateTaskAsync(userId, request);
        _logger.LogInformation("Task {TaskId} created by user {UserId}", task.Id, userId);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ApiResponse<TaskDto>.Ok(task, "Task created."));
    }

    /// <summary>Returns a specific task by ID. Results are cached in Redis for 5 minutes.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.GetTaskByIdAsync(id, userId);
        return Ok(ApiResponse<TaskDto>.Ok(task));
    }

    /// <summary>Returns all tasks belonging to the authenticated user, sorted by priority then date.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TaskDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        var tasks = await _taskService.GetAllTasksAsync(userId);
        return Ok(ApiResponse<IReadOnlyList<TaskDto>>.Ok(tasks));
    }

    /// <summary>Updates the status of a task. Invalidates the Redis cache entry.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusRequest request)
    {
        var userId = GetCurrentUserId();
        var task = await _taskService.UpdateTaskStatusAsync(id, userId, request);
        _logger.LogInformation("Task {TaskId} status updated to {Status} by user {UserId}", id, request.Status, userId);
        return Ok(ApiResponse<TaskDto>.Ok(task, "Task status updated."));
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? throw new UnauthorizedAccessException("User ID claim not found.");
        return Guid.Parse(sub);
    }
}
