using FiapX.Core.Interfaces.Security;
using FiapX.Infrastructure.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace FiapX.API.Middleware
{
    public class AuthenticationMiddleware : IFunctionsWorkerMiddleware
    {
        private readonly IConfiguration _configuration;

        public AuthenticationMiddleware(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
        {
            var req = await context.GetHttpRequestDataAsync();

            if (req != null && IsPublicEndpoint(req.Url.AbsolutePath))
            {
                await next(context);
                return;
            }

            string? token = null;

            if (req != null)
            {
                if (req.Headers.TryGetValues("Authorization", out var values))
                {
                    token = values.FirstOrDefault()?.Replace("Bearer ", "");
                }
                else if (req.Url.AbsolutePath.EndsWith("/download", StringComparison.OrdinalIgnoreCase))
                {
                    var query = HttpUtility.ParseQueryString(req.Url.Query);
                    token = query["access_token"];
                }
            }

            var principal = ValidateToken(token);

            if (principal != null)
            {
                var userContext = context.InstanceServices.GetService<IUserContext>() as UserContextService;
                userContext?.SetUser(principal);

                context.Items["User"] = principal;
            }
            else
            {
                await WriteUnauthorized(context, req);
                return;
            }

            await next(context);
        }

        private bool IsPublicEndpoint(string path)
        {
            var p = path.ToLowerInvariant();

            return p.Contains("/auth/login") ||
                   p.Contains("/auth/register") ||
                   p.Contains("/swagger") ||
                   p.Contains("/openapi") ||
                   p.Contains("/scalar");
        }

        private ClaimsPrincipal? ValidateToken(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            var jwtSecret = _configuration["FiapX:JwtSecret"];
            if (string.IsNullOrEmpty(jwtSecret)) return null;

            var secretBytes = Encoding.ASCII.GetBytes(jwtSecret);
            var handler = new JwtSecurityTokenHandler();

            try
            {
                handler.InboundClaimTypeMap.Clear();

                return handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(secretBytes), // NOSONAR
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out _);
            }
            catch
            {
                return null;
            }
        }

        private async Task WriteUnauthorized(FunctionContext context, HttpRequestData? req)
        {
            if (req != null)
            {
                var res = req.CreateResponse(HttpStatusCode.Unauthorized);
                await res.WriteStringAsync("Unauthorized");
                context.GetInvocationResult().Value = res;
            }
        }
    }
}