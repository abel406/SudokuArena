namespace SudokuArena.Server.Security;

public static class RoleAuthorization
{
    public static bool HasAnyRole(HttpContext httpContext, params string[] allowedRoles)
    {
        var role = httpContext.Request.Headers["X-Role"].ToString();
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return allowedRoles.Any(x => string.Equals(x, role, StringComparison.OrdinalIgnoreCase));
    }
}
