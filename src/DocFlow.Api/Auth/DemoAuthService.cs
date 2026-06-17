namespace DocFlow.Api.Auth;

public interface IAuthService
{
    LoginResponse? Login(LoginRequest request);
}

public sealed class DemoAuthService : IAuthService
{
    private readonly IJwtTokenService _tokens;

    public DemoAuthService(IJwtTokenService tokens)
    {
        _tokens = tokens;
    }

    public LoginResponse? Login(LoginRequest request)
    {
        if (string.Equals(request.UserName, "operator", StringComparison.OrdinalIgnoreCase)
            && request.Password == "Operator123!")
        {
            return _tokens.CreateToken("operator", DocFlowRoles.Operator);
        }

        if (string.Equals(request.UserName, "admin", StringComparison.OrdinalIgnoreCase)
            && request.Password == "Admin123!")
        {
            return _tokens.CreateToken("admin", DocFlowRoles.Admin);
        }

        return null;
    }
}
