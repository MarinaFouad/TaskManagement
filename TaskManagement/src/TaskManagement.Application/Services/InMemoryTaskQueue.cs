using System.Collections.Concurrent;
using TaskManagement.Application.Interfaces;

public class InMemoryTaskQueue : ITaskQueue
{
    // Thread-safe queue for background task processing
    private readonly ConcurrentQueue<Guid> _queue = new();

    // Add task to queue
    public void Enqueue(Guid taskId) => _queue.Enqueue(taskId);

    // Try to get next task (returns false if empty)
    public bool TryDequeue(out Guid taskId) => _queue.TryDequeue(out taskId);
}