using System;
using System.Threading.Tasks;
using FiapX.Application.Interfaces;
using FiapX.Core.Interfaces.Notifications;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using Microsoft.Extensions.Logging;

namespace FiapX.Application.UseCases.Batch
{
    public class NotifyBatchProcessingResultUseCase : INotifyBatchProcessingResultUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IEmailTemplateService _templateService;
        private readonly IEmailNotificationService _emailService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<NotifyBatchProcessingResultUseCase> _logger;

        public NotifyBatchProcessingResultUseCase(
            IUserRepository userRepository,
            IEmailTemplateService templateService,
            IEmailNotificationService emailService,
            ITokenService tokenService,
            ILogger<NotifyBatchProcessingResultUseCase> logger)
        {
            _userRepository = userRepository;
            _templateService = templateService;
            _emailService = emailService;
            _tokenService = tokenService;
            _logger = logger;
        }

        public async Task ExecuteSuccessAsync(Guid userId, Guid batchId)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found. Cannot send success notification for Batch {BatchId}.", userId, batchId);

                return;
            }

            string generatedToken = _tokenService.GenerateToken(user);

            string subject = $"FiapX - Batch {batchId} Processed Successfully";
            string htmlBody = await _templateService.GetSuccessEmailTemplateAsync(user.Username, batchId.ToString(), generatedToken);

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
        }

        public async Task ExecuteErrorAsync(Guid userId, Guid batchId, string errorMessage)
        {
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found. Cannot send success notification for Batch {BatchId}.", userId, batchId);

                return;
            }

            string subject = $"FiapX - Error processing Batch {batchId}";
            string htmlBody = await _templateService.GetErrorEmailTemplateAsync(user.Username, batchId.ToString(), errorMessage);

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody);
        }
    }
}