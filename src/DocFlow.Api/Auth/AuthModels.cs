namespace DocFlow.Api.Auth;

public static class DocFlowRoles
{
    public const string Operator = "Operator";
    public const string Admin = "Admin";
}

public static class DocFlowPolicies
{
    public const string DocumentUser = "DocumentUser";
}

public sealed record LoginRequest(string UserName, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    string UserName,
    string Role);
