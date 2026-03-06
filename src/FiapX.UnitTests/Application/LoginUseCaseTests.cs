using FiapX.Application.UseCases.Auth;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using FluentAssertions;
using Moq;

namespace FiapX.UnitTests.Application
{
    public class LoginUseCaseTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IPasswordHasher> _hasherMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly LoginUseCase _useCase;

        public LoginUseCaseTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _hasherMock = new Mock<IPasswordHasher>();
            _tokenServiceMock = new Mock<ITokenService>();

            _useCase = new LoginUseCase(
                _userRepositoryMock.Object,
                _hasherMock.Object,
                _tokenServiceMock.Object
            );
        }

        [Fact]
        public async Task ExecuteAsync_Should_Return_AuthOutput_When_Credentials_Are_Valid()
        {
            var input = new LoginInput("valid@email.com", "validPassword");
            var user = new User("validUser", input.Email, "hashedPassword");
            var expectedToken = "jwt_fake_token";

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(input.Email))
                .ReturnsAsync(user);

            _hasherMock.Setup(x => x.Verify(input.Password, user.PasswordHash))
                .Returns(true);

            _tokenServiceMock.Setup(x => x.GenerateToken(user))
                .Returns(expectedToken);

            var result = await _useCase.ExecuteAsync(input);

            result.Should().NotBeNull();
            result.Token.Should().Be(expectedToken);
            result.Username.Should().Be(user.Username);
        }

        [Fact]
        public async Task ExecuteAsync_Should_Throw_UnauthorizedAccessException_When_User_Not_Found()
        {
            var input = new LoginInput("notfound@email.com", "anyPassword");

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(input.Email))
                .ReturnsAsync((User)null!);

            Func<Task> act = async () => await _useCase.ExecuteAsync(input);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Credenciais inválidas.");
        }

        [Fact]
        public async Task ExecuteAsync_Should_Throw_UnauthorizedAccessException_When_Password_Is_Invalid()
        {
            var input = new LoginInput("valid@email.com", "wrongPassword");
            var user = new User("validUser", input.Email, "hashedPassword");

            _userRepositoryMock.Setup(x => x.GetByEmailAsync(input.Email))
                .ReturnsAsync(user);

            _hasherMock.Setup(x => x.Verify(input.Password, user.PasswordHash))
                .Returns(false);

            Func<Task> act = async () => await _useCase.ExecuteAsync(input);

            await act.Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("Credenciais inválidas.");
        }
    }
}
