using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace FiapX.API.Endpoints
{
    [ExcludeFromCodeCoverage]
    public class VideoBatchFunctions
    {
        private readonly ILogger<VideoBatchFunctions> _logger;
        private readonly IUploadVideoUseCase _uploadUseCase;
        private readonly IGetBatchStatusUseCase _statusUseCase;

        public VideoBatchFunctions(
            ILogger<VideoBatchFunctions> logger,
            IUploadVideoUseCase uploadUseCase,
            IGetBatchStatusUseCase statusUseCase)
        {
            _logger = logger;
            _uploadUseCase = uploadUseCase;
            _statusUseCase = statusUseCase;
        }

        [Function("UploadVideo")]
        public async Task<IActionResult> Upload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "batches")] HttpRequest req)
        {
            _logger.LogInformation("Recebendo upload de vídeo...");

            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult("Requisicao deve ser multipart/form-data");
            }

            var form = await req.ReadFormAsync();
            var files = form.Files;

            if (files.Count == 0)
                return new BadRequestObjectResult("Nenhum arquivo enviado.");

            var fileInputs = new List<FileInput>();
            foreach (var file in files)
            {
                var stream = file.OpenReadStream();
                fileInputs.Add(new FileInput(file.FileName, stream, file.ContentType));
            }

            var input = new UploadBatchInput(fileInputs);
            var output = await _uploadUseCase.ExecuteAsync(input);

            return new OkObjectResult(output);
        }

        [Function("GetBatchStatus")]
        public async Task<IActionResult> GetStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "batches/{id}")] HttpRequest req,
            string id)
        {
            if (!Guid.TryParse(id, out var batchGuid))
                return new BadRequestObjectResult("ID inválido.");

            var result = await _statusUseCase.ExecuteAsync(batchGuid);

            if (result == null)
                return new NotFoundObjectResult("Batch não encontrado.");

            return new OkObjectResult(result);
        }
    }
}
