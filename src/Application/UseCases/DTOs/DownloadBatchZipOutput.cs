namespace FiapX.Application.UseCases.DTOs
{
    public class DownloadBatchZipOutput
    {
        public Stream FileStream { get; set; } = default!;
        public string ContentType { get; set; } = default!;
        public string FileName { get; set; } = default!;
    }
}
