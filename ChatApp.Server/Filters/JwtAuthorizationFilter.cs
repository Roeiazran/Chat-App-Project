using ChatApp.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Runs before each controller action and authorize the user using the token provided in the header.
/// Attaches user Id and nickname to the context items.
/// </summary>
public class JwtAuthorizationFilter : IAuthorizationFilter
{
    private readonly JwtService _jwtService;

    public JwtAuthorizationFilter(JwtService jwtService)
    {
        _jwtService = jwtService;
    }


    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader)) return;
        
        UserDto? user = _jwtService.IsAuthorize(authHeader);

        if (user != null)
        {
            context.HttpContext.Items["UserId"] = user.UserId;
            context.HttpContext.Items["Nickname"] = user.Nickname;
            return; // user is authenticated, continue to action
        }
        
        // invalid or missing token, short-circuit the pipeline
        context.Result = new JsonResult(new { message = "Unauthorized: invalid or missing token." })
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }
}
