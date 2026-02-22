using FiapX.Application.UseCases.DTOs;

namespace FiapX.Application.Interfaces
{
    public interface IRegisterUserUseCase
    {
        public Task ExecuteAsync(RegisterUserInput input);
    }
}
