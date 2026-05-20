namespace TaskManagement.Application.DTOs;

public record CreateTaskRequest(
    string Title,
    string Description,
    string Priority = "Medium");

public record UpdateTaskStatusRequest(string Status);

public record TaskDto(
    Guid Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    bool IsProcessed,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    Guid UserId);
