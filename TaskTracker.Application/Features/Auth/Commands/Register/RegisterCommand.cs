using MediatR;

namespace TaskTracker.Application.Features.Auth.Commands.Register;

public class RegisterCommand : IRequest<RegisterResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
