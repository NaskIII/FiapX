using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Entities;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;
using FiapX.Core.Interfaces.UnityOfWork;

namespace FiapX.Application.UseCases.Auth
{
    public class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _hasher;
        private readonly IUnitOfWork _uow;

        public RegisterUserUseCase(IUserRepository userRepository, IPasswordHasher hasher, IUnitOfWork uow)
        {
            _userRepository = userRepository;
            _hasher = hasher;
            _uow = uow;
        }

        public async Task ExecuteAsync(RegisterUserInput input)
        {
            if (await _userRepository.EmailExistsAsync(input.Email))
                throw new InvalidOperationException("Email já cadastrado.");

            var passwordHash = _hasher.Hash(input.Password);
            var user = new User(input.Username, input.Email, passwordHash);

            await _userRepository.AddAsync(user);
            await _uow.CommitAsync();
        }
    }
}
