using FiapX.Application.UseCases.Batch;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Notifications;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using Microsoft.Extensions.Logging;
using Moq;

namespace FiapX.UnitTests.Application
{
    public class NotifyBatchProcessingResultUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IEmailTemplateService> _templateServiceMock;
        private readonly Mock<IEmailNotificationService> _emailServiceMock;
        private readonly Mock<ILogger<NotifyBatchProcessingResultUseCase>> _loggerMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly NotifyBatchProcessingResultUseCase _useCase;

        public NotifyBatchProcessingResultUseCaseTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _templateServiceMock = new Mock<IEmailTemplateService>();
            _emailServiceMock = new Mock<IEmailNotificationService>();
            _loggerMock = new Mock<ILogger<NotifyBatchProcessingResultUseCase>>();
            _tokenServiceMock = new Mock<ITokenService>();

            _useCase = new NotifyBatchProcessingResultUseCase(
                _userRepoMock.Object,
                _templateServiceMock.Object,
                _emailServiceMock.Object,
                _tokenServiceMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task ExecuteSuccessAsync_Should_Send_Email_When_User_Exists()
        {
            var userId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            var user = new User("TestUser", "test@domain.com", "hash");
            var expectedTemplate = "<html>Success</html>";
            var expectedToken = "generatedtoken";

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            _tokenServiceMock.Setup(t => t.GenerateToken(user))
                             .Returns(expectedToken);

            _templateServiceMock.Setup(t => t.GetSuccessEmailTemplateAsync(user.Username, batchId.ToString(), expectedToken))
                                .ReturnsAsync(expectedTemplate);

            await _useCase.ExecuteSuccessAsync(userId, batchId);

            _emailServiceMock.Verify(e => e.SendEmailAsync(
                user.Email,
                $"FiapX - Batch {batchId} Processed Successfully",
                expectedTemplate), Times.Once);
        }

        [Fact]
        public async Task ExecuteSuccessAsync_Should_Not_Send_Email_When_User_Not_Found()
        {
            var userId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            var generatedToken = "generatedtoken";

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync((User?)null);

            _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<User>()))
                             .Returns(generatedToken);

            await _useCase.ExecuteSuccessAsync(userId, batchId);

            _templateServiceMock.Verify(t => t.GetSuccessEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), generatedToken), Times.Never);

            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExecuteErrorAsync_Should_Send_Email_When_User_Exists()
        {
            var userId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            var errorMessage = "Format exception";
            var user = new User("TestUser", "test@domain.com", "hash");
            var expectedTemplate = "<html>Error</html>";

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync(user);

            _templateServiceMock.Setup(t => t.GetErrorEmailTemplateAsync(user.Username, batchId.ToString(), errorMessage))
                                .ReturnsAsync(expectedTemplate);

            await _useCase.ExecuteErrorAsync(userId, batchId, errorMessage);

            _emailServiceMock.Verify(e => e.SendEmailAsync(
                user.Email,
                $"FiapX - Error processing Batch {batchId}",
                expectedTemplate), Times.Once);
        }

        [Fact]
        public async Task ExecuteErrorAsync_Should_Not_Send_Email_When_User_Not_Found()
        {
            var userId = Guid.NewGuid();
            var batchId = Guid.NewGuid();

            _userRepoMock.Setup(r => r.GetByIdAsync(userId))
                         .ReturnsAsync((User?)null);

            await _useCase.ExecuteErrorAsync(userId, batchId, "error");

            _templateServiceMock.Verify(t => t.GetErrorEmailTemplateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);

            _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
