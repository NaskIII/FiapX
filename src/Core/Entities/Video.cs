using FiapX.Core.Entities.Base;
using FiapX.Core.Enums;

namespace FiapX.Core.Entities;

public class Video : Entity
{
    public Guid BatchId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string FilePath { get; private set; } = string.Empty;
    public string? OutputPath { get; private set; }
    public VideoStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    protected Video() { }

    public Video(Guid batchId, string fileName, string filePath) : base()
    {
        BatchId = batchId;
        FileName = fileName;
        FilePath = filePath;
        Status = VideoStatus.Pending;
    }

    public void MarkAsProcessing()
    {
        Status = VideoStatus.Processing;
        RegisterUpdate();
    }

    public void MarkAsDone(string zipPath)
    {
        Status = VideoStatus.Done;
        OutputPath = zipPath;
        RegisterUpdate();
    }

    public void MarkAsError(string error)
    {
        Status = VideoStatus.Error;
        ErrorMessage = error;
        RegisterUpdate();
    }
}