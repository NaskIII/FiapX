namespace FiapX.Core.Interfaces.VideoFramerExtractor
{
    public interface IVideoFrameExtractorService
    {
        /// <summary>
        /// Extrai todos os frames de um vídeo para o diretório especificado.
        /// Lança InvalidDataException se o vídeo for inválido.
        /// </summary>
        public Task ExtractFramesAsync(string inputFilePath, string outputDirectoryPath);
    }
}
