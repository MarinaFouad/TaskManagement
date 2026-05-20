using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Domain.Exceptions;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _uow;
    private readonly ICacheService _cache;
    private readonly ITaskQueue _taskQueue;

    public TaskService(IUnitOfWork uow, ICacheService cache, ITaskQueue taskQueue)
    {
        _uow = uow;
        _cache = cache;
        _taskQueue = taskQueue;
    }

    public async Task<TaskDto> CreateTaskAsync(Guid userId, CreateTaskRequest request)
    {
        // Prevent duplicate tasks with same title in the same day
        var today = DateTime.UtcNow.Date;
        var isDuplicate = await _uow.Tasks.ExistsDuplicateTodayAsync(userId, request.Title, today);
        if (isDuplicate)
            throw new ConflictException(
                $"A task with title '{request.Title}' already exists for today. Duplicate tasks are not allowed.");

        // Convert priority string ? enum safely
        if (!Enum.TryParse<TaskPriority>(request.Priority, true, out var priority))
            throw new ValidationException($"Invalid priority '{request.Priority}'.");

        var task = new UserTask
        {
            Title = request.Title,
            Description = request.Description,
            Priority = priority,
            UserId = userId,
            Status = TaskStatus.Pending // default state
        };

        await _uow.Tasks.AddAsync(task);
        await _uow.SaveChangesAsync();

        // Push task to background queue for processing
        _taskQueue.Enqueue(task.Id);

        return MapToDto(task);
    }

    public async Task<TaskDto> GetTaskByIdAsync(Guid taskId, Guid userId)
    {
        var cacheKey = $"task:{taskId}";

        // Try cache first (faster than DB)
        var cached = await _cache.GetAsync<TaskDto>(cacheKey);
        if (cached is not null)
        {
            // Still validate ownership (important!)
            if (cached.UserId != userId)
                throw new ForbiddenException("You do not have access to this task.");

            return cached;
        }

        var task = await _uow.Tasks.GetByIdAsync(taskId)
            ?? throw new NotFoundException("Task", taskId);

        // Security check
        if (task.UserId != userId)
            throw new ForbiddenException("You do not have access to this task.");

        var dto = MapToDto(task);

        // Cache result for future requests
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5));

        return dto;
    }

    public async Task<IReadOnlyList<TaskDto>> GetAllTasksAsync(Guid userId)
    {
        var tasks = await _uow.Tasks.GetByUserIdAsync(userId);

        // Sort: highest priority first, then oldest tasks
        return tasks
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(MapToDto)
            .ToList()
            .AsReadOnly();
    }

    public async Task<TaskDto> UpdateTaskStatusAsync(Guid taskId, Guid userId, UpdateTaskStatusRequest request)
    {
        var task = await _uow.Tasks.GetByIdAsync(taskId)
            ?? throw new NotFoundException("Task", taskId);

        // Make sure user owns the task
        if (task.UserId != userId)
            throw new ForbiddenException("You do not have access to this task.");

        // Validate status input
        if (!Enum.TryParse<TaskStatus>(request.Status, true, out var newStatus))
            throw new ValidationException($"Invalid status '{request.Status}'.");

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;

        await _uow.Tasks.UpdateAsync(task);
        await _uow.SaveChangesAsync();

        // Remove stale cache
        await _cache.RemoveAsync($"task:{taskId}");

        return MapToDto(task);
    }

    // Mapping helper
    private static TaskDto MapToDto(UserTask task) =>
        new(task.Id, task.Title, task.Description,
            task.Status.ToString(), task.Priority.ToString(),
            task.IsProcessed, task.CreatedAt, task.UpdatedAt, task.UserId);
}