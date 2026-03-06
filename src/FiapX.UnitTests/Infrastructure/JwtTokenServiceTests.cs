using FiapX.Core.Entities;
using FiapX.Infrastructure.Services;
using FiapX.Infrastructure.Settings;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;

namespace FiapX.UnitTests.Infrastructure
{
    public class JwtTokenServiceTests
    {
        private readonly JwtTokenService _service;
        private readonly FiapXSettings _settings;
        private readonly string _validSecret = "minha-chave-super-secreta-com-pelo-menos-32-chars-para-hmac256";

        public JwtTokenServiceTests()
        {
            _settings = new FiapXSettings
            {
                JwtSecret = _validSecret
            };

            _service = new JwtTokenService(_settings);
        }

        [Fact]
        public void GenerateToken_Should_Return_Valid_Jwt_String()
        {
            var user = new User("testeuser", "teste@email.com", "123456");

            var token = _service.GenerateToken(user);

            token.Should().NotBeNullOrWhiteSpace();
            token.Split('.').Should().HaveCount(3, "porque um JWT deve ter Header, Payload e Signature");
        }

        [Fact]
        public void GenerateToken_Should_Contain_Correct_Claims()
        {
            var user = new User("raphael", "raphael@fiap.com.br", "senha123");
            
            var expectedId = user.Id.ToString();

            var tokenString = _service.GenerateToken(user);

            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.Claims.First(c => c.Type == "unique_name").Value.Should().Be(user.Username);
            token.Claims.First(c => c.Type == "email").Value.Should().Be(user.Email);
            token.Claims.First(c => c.Type == "id").Value.Should().Be(expectedId);
        }

        [Fact]
        public void GenerateToken_Should_Have_Correct_Expiration()
        {
            var user = new User("user", "mail@mail.com", "pass");

            var tokenString = _service.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddHours(2), TimeSpan.FromSeconds(10));
        }

        [Fact]
        public void GenerateToken_Should_Use_HmacSha256_Algorithm()
        {
            var user = new User("user", "mail@mail.com", "pass");

            var tokenString = _service.GenerateToken(user);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenString);

            token.SignatureAlgorithm.Should().Be("HS256");

            token.Header.Alg.Should().Be("HS256");
        }
    }
}
