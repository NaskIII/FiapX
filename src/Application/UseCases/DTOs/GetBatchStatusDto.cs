namespace FiapX.Application.UseCases.DTOs;

public record VideoStatusDto(
    Guid VideoId,
    string FileName,
    string Status,
    string? ErrorMessage,
    string? DownloadUrl
);

public record BatchStatusOutput(
    Guid BatchId,
    string Status,
    DateTime CreatedAt,
    List<VideoStatusDto> Videos
);