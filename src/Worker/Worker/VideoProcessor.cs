using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace FiapX.Worker.Worker
{

    [ExcludeFromCodeCoverage]
    public class VideoProcessor
    {
        private readonly ILogger<VideoProcessor> _logger;
        private readonly IProcessVideoUseCase _useCase;
        private readonly JsonSerializerOptions _jsonOptions;

        public VideoProcessor(ILogger<VideoProcessor> logger, IProcessVideoUseCase useCase)
        {
            _logger = logger;
            _useCase = useCase;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        [Function("ProcessVideoQueue")]
        public async Task Run(
            [ServiceBusTrigger(
                queueName: "%FiapXServiceBusQueueName%",
                Connection = "FiapXServiceBusConnection"
        )] string myQueueItem)
        {
            _logger.LogInformation("Processing message: {MyQueueItem}", myQueueItem);

            try
            {
                var payload = JsonSerializer.Deserialize<ProcessVideoInput>(myQueueItem, _jsonOptions);

                if (payload != null)
                {
                    await _useCase.ExecuteAsync(payload);
                    _logger.LogInformation("Video {PayloadVideoId} processed successfully.", payload.VideoId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video message.");
                throw;
            }
        }
    }
}