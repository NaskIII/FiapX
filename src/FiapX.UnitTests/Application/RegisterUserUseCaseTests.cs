using FiapX.Application.UseCases.Auth;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using FiapX.Core.Interfaces.UnityOfWork;
using FluentAssertions;
using Moq;

namespace FiapX.UnitTests.Application
{
    public class RegisterUserUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly RegisterUserUseCase _useCase;

        public RegisterUserUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _uowMock = new Mock<IUnitOfWork>();

            _useCase = new RegisterUserUseCase(
                _userRepositoryMock.Object,
                _hasherMock.Object,
                _uowMock.Object
            );
        }

        [Fact]
        public async Task ExecuteAsync_Should_Create_User_And_Commit_When_Email_Is_Unique()
        {
            var input = new RegisterUserInput("usuario", "teste@email.com", "senha123");
            var hashedPassword = "hashed_senha123";

            _userRepositoryMock.Setup(x => x.EmailExistsAsync(input.Email))
                .ReturnsAsync(false);

            _hasherMock.Setup(x => x.Hash(input.Password))
                .Returns(hashedPassword);

            await _useCase.ExecuteAsync(input);

            _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u =>
                u.Username == input.Username &&
                u.Email == input.Email
            )), Times.Once);

            _uowMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Throw_InvalidOperationException_When_Email_Already_Exists()
        {
            var input = new RegisterUserInput("usuario", "existente@email.com", "senha123");

            _userRepositoryMock.Setup(x => x.EmailExistsAsync(input.Email))
                .ReturnsAsync(true);

            Func<Task> act = async () => await _useCase.ExecuteAsync(input);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Email já cadastrado.");

            _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);

            _uowMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
