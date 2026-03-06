using FiapX.Application.Interfaces;
using FiapX.Core.Interfaces.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Security.Claims;

namespace FiapX.API.Endpoints
{
    public class DownloadBatchHttpTrigger
    {
        private readonly IDownloadBatchZipUseCase _useCase;
        private readonly IUserContext _userContext;

        public DownloadBatchHttpTrigger(IDownloadBatchZipUseCase useCase, IUserContext userContext)
        {
            _useCase = useCase;
            _userContext = userContext;
        }

        [Function("DownloadBatchZip")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "batches/{batchId}/download")] HttpRequestData req,
            Guid batchId)
        {
            var userId = _userContext.UserId;

            if (userId == Guid.Empty)
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            try
            {
                using var memoryStream = new MemoryStream();
                await _useCase.ExecuteAsync(batchId, userId, memoryStream);

                memoryStream.Position = 0;

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/zip");
                response.Headers.Add("Content-Disposition", $"attachment; filename=\"{batchId}.zip\"");

                await response.Body.WriteAsync(memoryStream.ToArray());

                return response;
            }
            catch (UnauthorizedAccessException)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }
            catch (Exception)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
        }
    }
}
