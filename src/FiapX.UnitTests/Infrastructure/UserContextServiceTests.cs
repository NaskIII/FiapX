using FiapX.Infrastructure.Services;
using FluentAssertions;
using System.Security.Claims;

namespace FiapX.UnitTests.Infrastructure
{
    public class UserContextServiceTests
    {
        private readonly UserContextService _service;

        public UserContextServiceTests()
        {
            _service = new UserContextService();
        }

        [Fact]
        public void SetUser_Should_Not_Update_Properties_When_Principal_Is_Null()
        {
            _service.SetUser(null);

            _service.IsAuthenticated.Should().BeFalse();
            _service.UserId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void SetUser_Should_Set_IsAuthenticated_True_When_Identity_Is_Authenticated()
        {
            var identity = new ClaimsIdentity(new List<Claim>(), "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.IsAuthenticated.Should().BeTrue();
        }

        [Fact]
        public void SetUser_Should_Set_IsAuthenticated_False_When_Identity_IsNot_Authenticated()
        {
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public void SetUser_Should_Extract_UserId_From_Id_Claim()
        {
            var expectedId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim("id", expectedId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.UserId.Should().Be(expectedId);
        }

        [Fact]
        public void SetUser_Should_Extract_UserId_From_NameIdentifier_When_Id_Is_Missing()
        {
            var expectedId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, expectedId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.UserId.Should().Be(expectedId);
        }

        [Fact]
        public void SetUser_Should_Extract_UserId_From_Sub_When_Others_Are_Missing()
        {
            var expectedId = Guid.NewGuid();
            var claims = new List<Claim>
            {
                new Claim("sub", expectedId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.UserId.Should().Be(expectedId);
        }

        [Fact]
        public void SetUser_Should_Prioritize_Id_Claim_Over_Others()
        {
            var idGuid = Guid.NewGuid();
            var subGuid = Guid.NewGuid();

            var claims = new List<Claim>
            {
                new Claim("id", idGuid.ToString()),
                new Claim("sub", subGuid.ToString())
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.UserId.Should().Be(idGuid);
        }

        [Fact]
        public void SetUser_Should_Keep_UserId_Empty_When_Claim_Value_Is_Not_Guid()
        {
            var claims = new List<Claim>
            {
                new Claim("id", "valor-invalido-nao-guid")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.UserId.Should().Be(Guid.Empty);
        }

        [Fact]
        public void SetUser_Should_Keep_UserId_Empty_When_No_Matching_Claims_Exist()
        {
            var claims = new List<Claim>
            {
                new Claim("email", "teste@teste.com")
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);

            _service.SetUser(principal);

            _service.UserId.Should().Be(Guid.Empty);
            _service.IsAuthenticated.Should().BeTrue();
        }
    }
}
