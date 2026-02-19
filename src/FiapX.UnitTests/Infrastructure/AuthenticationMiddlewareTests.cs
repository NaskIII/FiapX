using FiapX.API.Middleware;
using FiapX.Core.Interfaces.Security;
using FiapX.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;

namespace FiapX.UnitTests.Infrastructure
{
    public class AuthenticationMiddlewareTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthenticationMiddleware _middleware;
        private readonly string _validSecret = "minha-chave-secreta-muito-segura-123456";

        public AuthenticationMiddlewareTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["FiapX:JwtSecret"]).Returns(_validSecret);
            _middleware = new AuthenticationMiddleware(_configurationMock.Object);
        }

        [Fact]
        public async Task Invoke_Should_Call_Next_When_Endpoint_Is_Public()
        {
            var contextMock = CreateContextMock("http://localhost/api/auth/login");
            bool nextCalled = false;
            FunctionExecutionDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            await _middleware.Invoke(contextMock.Object, next);

            Assert.True(nextCalled, "O next deveria ser chamado para endpoints públicos");
        }

        [Fact]
        public async Task Invoke_Should_Return_401_When_Token_Is_Missing()
        {
            var contextMock = CreateContextMock("http://localhost/api/videos");
            bool nextCalled = false;
            FunctionExecutionDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            try
            {
                await _middleware.Invoke(contextMock.Object, next);
            }
            catch (InvalidOperationException)
            {

            }

            Assert.False(nextCalled, "O next NÃO deveria ser chamado sem token");
            VerifyResponseStatusCode(contextMock, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Invoke_Should_Return_401_When_Token_Is_Invalid()
        {
            var contextMock = CreateContextMock("http://localhost/api/videos", "Bearer token-invalido-123");
            bool nextCalled = false;
            FunctionExecutionDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            try
            {
                await _middleware.Invoke(contextMock.Object, next);
            }
            catch (InvalidOperationException)
            {

            }

            Assert.False(nextCalled);
            VerifyResponseStatusCode(contextMock, HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Invoke_Should_Authenticate_And_Set_User_When_Token_Is_Valid()
        {
            var token = GenerateValidToken();
            var contextMock = CreateContextMock("http://localhost/api/videos", $"Bearer {token}");

            var userContextService = new UserContextService();
            var serviceProvider = new ServiceCollection()
                .AddSingleton<IUserContext>(userContextService)
                .BuildServiceProvider();

            contextMock.Setup(c => c.InstanceServices).Returns(serviceProvider);
            contextMock.SetupProperty(c => c.Items, new Dictionary<object, object>());

            bool nextCalled = false;
            FunctionExecutionDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };

            await _middleware.Invoke(contextMock.Object, next);

            Assert.True(nextCalled, "O next deveria ser chamado com token válido");

            Assert.True(contextMock.Object.Items.ContainsKey("User"));
            var principal = contextMock.Object.Items["User"] as ClaimsPrincipal;
            Assert.NotNull(principal);

            var nameClaim = principal.FindFirst("unique_name")?.Value ?? principal.Identity?.Name;
            Assert.Equal("testeuser", nameClaim);

            Assert.NotEqual(Guid.Empty, userContextService.UserId);
        }

        private Mock<FunctionContext> CreateContextMock(string url, string? authHeader = null)
        {
            var contextMock = new Mock<FunctionContext>();
            var requestMock = new Mock<HttpRequestData>(contextMock.Object);
            var responseMock = new Mock<HttpResponseData>(contextMock.Object);

            var headers = new HttpHeadersCollection();
            if (authHeader != null)
            {
                headers.Add("Authorization", authHeader);
            }

            requestMock.Setup(r => r.Headers).Returns(headers);
            requestMock.Setup(r => r.Url).Returns(new Uri(url));

            responseMock.SetupProperty(r => r.StatusCode);
            responseMock.Setup(r => r.Headers).Returns(new HttpHeadersCollection());

            var responseBody = new MemoryStream();
            responseMock.Setup(r => r.Body).Returns(responseBody);

            requestMock.Setup(r => r.CreateResponse()).Returns(responseMock.Object);

            var featuresMock = new Mock<IInvocationFeatures>();

            var httpFeatureMock = new Mock<IHttpRequestDataFeature>();
            httpFeatureMock.Setup(f => f.GetHttpRequestDataAsync(It.IsAny<FunctionContext>()))
                           .Returns(new ValueTask<HttpRequestData?>(requestMock.Object));

            featuresMock.Setup(f => f.Get<IHttpRequestDataFeature>()).Returns(httpFeatureMock.Object);

            contextMock.Setup(c => c.Features).Returns(featuresMock.Object);
            contextMock.Setup(c => c.Items).Returns(new Dictionary<object, object>());

            return contextMock;
        }

        private string GenerateValidToken()
        {
            var key = Encoding.ASCII.GetBytes(_validSecret);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, "testeuser"),
                    new Claim("id", Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            return tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        }

        private void VerifyResponseStatusCode(Mock<FunctionContext> contextMock, HttpStatusCode expectedCode)
        {
            var request = contextMock.Object.GetHttpRequestDataAsync().Result;

            var requestMock = Mock.Get(request);
            requestMock.Verify(r => r.CreateResponse(), Times.AtLeastOnce);

            var response = request.CreateResponse();
            Assert.Equal(expectedCode, response.StatusCode);
        }
    }
}