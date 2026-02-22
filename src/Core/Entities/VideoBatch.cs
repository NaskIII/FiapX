using FiapX.Core.Entities.Base;
using FiapX.Core.Enums;

namespace FiapX.Core.Entities;

public class VideoBatch : Entity
{
    public Guid BatchId { get; private set; }
    public Guid UserId { get; private set; }
    public BatchStatus Status { get; private set; }

    private readonly List<Video> _videos = new();
    public IReadOnlyCollection<Video> Videos => _videos.AsReadOnly();

    protected VideoBatch() { }

    public VideoBatch(Guid userId) : base()
    {
        BatchId = Id;
        UserId = userId;
        Status = BatchStatus.Pending;
    }

    public void AddVideo(string fileName, string filePath)
    {
        var video = new Video(Id, fileName, filePath);
        _videos.Add(video);
    }

    public void UpdateStatus()
    {
        if (_videos.All(v => v.Status == VideoStatus.Done))
        {
            Status = BatchStatus.Completed;
            RegisterUpdate();
            return;
        }

        if (_videos.Any(v => v.Status == VideoStatus.Processing || v.Status == VideoStatus.Pending))
        {
            var anyProcessing = _videos.Any(v => v.Status == VideoStatus.Processing || v.Status == VideoStatus.Done);

            Status = anyProcessing ? BatchStatus.Processing : BatchStatus.Pending;

            RegisterUpdate();
            return;
        }

        if (_videos.All(v => v.Status == VideoStatus.Error))
        {
            Status = BatchStatus.Error;
        }
        else if (_videos.Any(v => v.Status == VideoStatus.Error))
        {
            Status = BatchStatus.CompletedWithErrors; 
        }

        RegisterUpdate();
    }
}