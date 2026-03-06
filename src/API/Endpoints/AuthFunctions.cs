using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.Auth;
using FiapX.Application.UseCases.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace FiapX.API.Endpoints
{
    public class AuthFunctions
    {
        private readonly IRegisterUserUseCase _registerUseCase;
        private readonly ILoginUseCase _loginUseCase;

        public AuthFunctions(IRegisterUserUseCase registerUseCase, ILoginUseCase loginUseCase)
        {
            _registerUseCase = registerUseCase;
            _loginUseCase = loginUseCase;
        }

        [Function("RegisterUser")]
        public async Task<IActionResult> Register(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/register")] HttpRequest req)
        {
            var input = await req.ReadFromJsonAsync<RegisterUserInput>();
            try
            {
                await _registerUseCase.ExecuteAsync(input);
                return new OkObjectResult(new { message = "Usuário criado com sucesso" });
            }
            catch (InvalidOperationException ex)
            {
                return new BadRequestObjectResult(new { error = ex.Message });
            }
        }

        [Function("LoginUser")]
        public async Task<IActionResult> Login(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth/login")] HttpRequest req)
        {
            var input = await req.ReadFromJsonAsync<LoginInput>();
            try
            {
                var output = await _loginUseCase.ExecuteAsync(input);
                return new OkObjectResult(output);
            }
            catch (UnauthorizedAccessException)
            {
                return new UnauthorizedResult();
            }
        }
    }
}
