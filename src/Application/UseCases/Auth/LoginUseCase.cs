using FiapX.Application.Interfaces;
using FiapX.Application.UseCases.DTOs;
using FiapX.Core.Interfaces.Repositories;
using FiapX.Core.Interfaces.Security;

namespace FiapX.Application.UseCases.Auth
{
    public class LoginUseCase : ILoginUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _hasher;
        private readonly ITokenService _tokenService;

        public LoginUseCase(IUserRepository userRepository, IPasswordHasher hasher, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _hasher = hasher;
            _tokenService = tokenService;
        }

        public async Task<AuthOutput> ExecuteAsync(LoginInput input)
        {
            var user = await _userRepository.GetByEmailAsync(input.Email);

            if (user == null || !_hasher.Verify(input.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Credenciais inválidas.");

            var token = _tokenService.GenerateToken(user);

            return new AuthOutput(token, user.Username);
        }
    }
}
