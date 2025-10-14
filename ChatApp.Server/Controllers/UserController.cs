namespace ChatApp.Server.Controllers;
using ChatApp.Server.Models;
using Microsoft.AspNetCore.Mvc;
using ChatApp.Server.Services;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;
    private readonly ChatHubService _chatHubService;
    public UserController(UserService userService, JwtService jwtService, ChatHubService chatHubService)
    {
        _userService = userService;
        _jwtService = jwtService;
        _chatHubService = chatHubService;
    }

    /// <summary>
    /// Register the user and attach refresh token to the HTTP cookie 
    /// </summary>
    /// <returns>The token used for authentication.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest user)
    {
        int newUserId = _userService.Register(user);

        var token = _jwtService.GenerateToken(newUserId, user.Nickname);

        RefreshToken refreshToken = _jwtService.CreateRefreshToken(newUserId);
        
        // set refreshToken Cookie
        Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = refreshToken.ExpiresAt,
            Path = "/"
        });

        await _chatHubService.NotifyNewRegister(newUserId, user.Nickname);
        return Ok(new { token });
    }

    /// <summary>
    /// Authenticate the user using the password and set the refresh token in the HTTP cookie
    /// </summary>
    /// <returns>Token</returns>
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginUser user)
    {
        UserDto? loggedUser = _userService.Authenticate(user.Username, user.Password);

        if (loggedUser == null)
            return Unauthorized(new { message = "Invalid username or password." });

        string token = _jwtService.GenerateToken(loggedUser.UserId, loggedUser.Nickname);
        RefreshToken refreshToken = _jwtService.UpdateRefreshToken(loggedUser.UserId);

        // set refreshToken Cookie
        Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = refreshToken.ExpiresAt,
            Path = "/"
        });

        return Ok(new { token });
    }

    /// <summary>
    /// Called on setTimeout from the app, before token expired.
    /// </summary>
    /// <returns>New Token</returns>
    [HttpPost("refresh")]
    public IActionResult RefreshToken()
    {
        string? authHeader = HttpContext.Request.Headers.Authorization.FirstOrDefault();
        UserDto? user = JwtService.GetUserDataFromHeader(authHeader);

        if (user == null) 
            return Unauthorized("Auth header is required for refresh");

        if (!Request.Cookies.TryGetValue("refreshToken", out string? refreshToken))
            return Unauthorized("Refresh token is missing.");

        if (_jwtService.ValidateRefreshToken(user.UserId, refreshToken))
        {
            string newToken = _jwtService.GenerateToken(user.UserId, user.Nickname);
            return Ok(new { Token = newToken });
        }
        return Unauthorized("Refresh token is not valid");
    }

}

