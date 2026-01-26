namespace FiapX.Application.UseCases.DTOs;

public record FileInput(
    string FileName,
    Stream FileStream,
    string ContentType
);

public record UploadBatchInput(
    string UserOwner,
    List<FileInput> Files
);

public record UploadBatchOutput(
    Guid BatchId,
    int TotalVideos,
    string Status
);