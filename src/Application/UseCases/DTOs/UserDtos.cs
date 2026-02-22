namespace FiapX.Application.UseCases.DTOs
{
    public record RegisterUserInput(string Username, string Email, string Password);
    public record LoginInput(string Email, string Password);
    public record AuthOutput(string Token, string Username);
}
