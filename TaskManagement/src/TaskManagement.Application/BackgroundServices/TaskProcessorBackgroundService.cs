using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.BackgroundServices;

/// <summary>
/// Dequeues newly created tasks and marks them as processed (simulates background processing).
/// In a real-world system, this could trigger notifications, AI classification, etc.
/// </summary>
public class TaskProcessorBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITaskQueue _taskQueue;
    private readonly ILogger<TaskProcessorBackgroundService> _logger;

    public TaskProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ITaskQueue taskQueue,
        ILogger<TaskProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _taskQueue = taskQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Processor Background Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_taskQueue.TryDequeue(out var taskId))
                {
                    await ProcessTaskAsync(taskId, stoppingToken);
                }
                else
                {
                    // No tasks — wait a bit before polling again
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing task from queue.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Task Processor Background Service stopped.");
    }

    private async Task ProcessTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing task {TaskId}...", taskId);

        // Simulate processing time
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

        using var scope = _scopeFactory.CreateScope();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var task = await uow.Tasks.GetByIdAsync(taskId);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found during processing.", taskId);
            return;
        }

        task.IsProcessed = true;
        task.UpdatedAt = DateTime.UtcNow;

        await uow.Tasks.UpdateAsync(task);
        await uow.SaveChangesAsync(cancellationToken);

        // Invalidate cache so next GET reflects updated state
        await cache.RemoveAsync($"task:{taskId}");

        _logger.LogInformation("Task {TaskId} processed successfully.", taskId);
    }
}
