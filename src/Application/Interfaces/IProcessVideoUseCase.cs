using FiapX.Application.UseCases.DTOs;

namespace FiapX.Application.Interfaces
{
    public interface IProcessVideoUseCase
    {
        public Task ExecuteAsync(ProcessVideoInput input);
    }
}
