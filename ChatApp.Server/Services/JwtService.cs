using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ChatApp.Server.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using ChatApp.Server.DAL;

/// <summary>
/// Provide the methods for the jwt token.
/// </summary>
public class JwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SqlServerDAL _dal;
    public JwtService(IOptions<JwtSettings> options, SqlServerDAL sqlServerDAL)
    {
        _jwtSettings = options.Value;
        _dal = sqlServerDAL;
    }

    /// <summary>
    /// Generate JWT token for a user
    /// </summary>
    /// <returns>The token generated for the user.</returns>
    public string GenerateToken(int userId, string nickname)
    {
        // claims to be encoded into the payload
        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim("nickname", nickname)
        };

        // convert the secret key from app settings to a byte array
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        
        // creates the digital signature configuration for the JWT
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // create a JWT token
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenLifetimeMinutes),
            signingCredentials: creds
        );
        
        // convert the token to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Authorize the user using the authorization header.
    /// </summary>
    /// <returns>If the user authorize, it's data, else null</returns>
    public UserDto? IsAuthorize(string? header) 
    {
        string? token = ExtractTokenFromHeader(header);
        if (string.IsNullOrWhiteSpace(token)) return null;

        // create a handler to read and validate JWT tokens
        var handler = new JwtSecurityTokenHandler();

        // convert the secret key from app settings to a byte array
        var key = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);

        try
        {   
            // validate the token
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = false,             // don't check the token issuer
                ValidateAudience = false,           // don't check the token audience
                ValidateLifetime = true,            // check expiration time
                ValidateIssuerSigningKey = true,    // ensure signature is valid
                IssuerSigningKey = new SymmetricSecurityKey(key)    // key used to sign the token
            }, out _);
            
            var userIdClaim = principal.FindFirst("userId");
            var nicknameClaim = principal.FindFirst("nickname");

            if (userIdClaim == null || nicknameClaim == null)
                return null;

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return null;

            return new UserDto
            {
                UserId = userId,
                Nickname = nicknameClaim.Value
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extract the token string from the authorization header.
    /// </summary>
    /// <returns>The token as a string</returns>
    public static string? ExtractTokenFromHeader(string? header)
    {
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer "))
            return null;

        // extract the token from the header
        string token = header.Substring("Bearer ".Length).Trim();

        return token;
    }

    /// <summary>
    /// Get the user nickname and id from the token claims.
    /// </summary>
    public static UserDto? GetUserDataFromHeader(string? header) {

        string? token = ExtractTokenFromHeader(header);

        if (token == null) return null;

        try
        {
            // create handler
            var handler = new JwtSecurityTokenHandler();

            // read the token without validating it
            var jwtToken = handler.ReadJwtToken(token);

            // extract claims
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "userId");
            var nicknameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nickname");

            if (userIdClaim == null || nicknameClaim == null)
                return null;

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return null;

            return new UserDto
            {
                UserId = userId,
                Nickname = nicknameClaim.Value
            };
        }
        catch
        {
            // token format invalid
            return null;
        }
    }


    public RefreshToken CreateRefreshToken(int userId) {
        string newToken = GenerateRefreshToken();
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.RefreshTokenLifetimeMinutes);

        _dal.ExecCmdWithParams(
            SqlServerDAL.CommandType.StoredProcedure,
            "CreateRefreshToken",
            new Dictionary<string, object>
            {
                { "UserId", userId },
                { "Token", newToken },
                { "ExpiresAt", expiresAt }
            }
        );

        return new RefreshToken {
            Token = newToken,
            ExpiresAt = expiresAt,
            UserId = userId
        };
    }


    public RefreshToken UpdateRefreshToken(int userId) {
        string newToken = GenerateRefreshToken();
        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.RefreshTokenLifetimeMinutes);

            _dal.ExecCmdWithParams(
            SqlServerDAL.CommandType.StoredProcedure,
            "UpdateRefreshToken",
            new Dictionary<string, object>
            {
                { "UserId", userId },
                { "Token", newToken },
                { "ExpiresAt", expiresAt }
            }
        );

        return new RefreshToken {
            Token = newToken,
            ExpiresAt = expiresAt,
            UserId = userId
        };
    }

    /// <summary>
    /// Get all the tokens in the database.
    /// </summary>
    public List<RefreshToken> GetAllTokens()
    {
        return _dal.ExecCmdWithResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "GetAllRefreshTokens",
            reader => reader.ReadResultset(() => new RefreshToken
            {
                UserId = reader.GetInt32("UserId"),
                Token = reader.GetString("Token"),
                ExpiresAt = reader.GetDateTime("ExpiresAt")
            }).ToList()
        );
    }
    /// <summary>
    /// Validate the refresh token by searching it in the database.
    /// </summary>
    /// <param name="refreshToken">The token to validate.</param>
    /// <returns>True if the token is valid, else false.</returns>
    public bool ValidateRefreshToken(int userId, string refreshToken)
    {
        RefreshToken? token = GetAllTokens()
            .Where(t => 
            t.UserId == userId &&
            t.ExpiresAt > DateTime.UtcNow &&
            t.Token == refreshToken
        ).FirstOrDefault();

        return token != null;
    }

    /// <summary>
    /// Generate a 64 random bytes.
    /// </summary>
    /// <returns>Refresh token as a string.</returns>
    public static string GenerateRefreshToken()
    {
        // create a 64-byte array
        var randomBytes = new byte[64];
        
        // create an RNG disposable object 
        using (var rng = RandomNumberGenerator.Create())
        {
            // fill the randomBytes array with random values
            rng.GetBytes(randomBytes);
        }

        // convert to Base64 for easy storage
        return Convert.ToBase64String(randomBytes);
    }

}
