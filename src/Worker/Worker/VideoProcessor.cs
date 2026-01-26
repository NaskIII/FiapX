using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Infrastructure.Settings;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FiapX.Worker.Worker
{
    public class VideoProcessor
    {
        private readonly ILogger<VideoProcessor> _logger;
        private readonly IProcessVideoUseCase _useCase;
        private readonly FiapXSettings _settings;

        public VideoProcessor(ILogger<VideoProcessor> logger, IProcessVideoUseCase useCase, FiapXSettings settings)
        {
            _logger = logger;
            _useCase = useCase;
            _settings = settings;
        }

        [Function("ProcessVideoQueue")]
        public async Task Run(
            [ServiceBusTrigger(
                queueName: "%FiapXServiceBusQueueName%",
                Connection = "FiapXServiceBusConnection"
        )] string myQueueItem)
        {
            _logger.LogInformation($"Processing message: {myQueueItem}");

            try
            {
                var payload = JsonSerializer.Deserialize<ProcessVideoInput>(myQueueItem, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload != null)
                {
                    await _useCase.ExecuteAsync(payload);
                    _logger.LogInformation($"Video {payload.VideoId} processed successfully.");
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