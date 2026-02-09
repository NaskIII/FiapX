using FiapX.Application.UseCases.DTOs;

namespace FiapX.Application.Interfaces
{
    public interface ILoginUseCase
    {
        public Task<AuthOutput> ExecuteAsync(LoginInput input);
    }
}
