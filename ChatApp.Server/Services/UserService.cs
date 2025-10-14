namespace ChatApp.Server.Services;
using Microsoft.AspNetCore.Identity; 
using ChatApp.Server.DAL;
using ChatApp.Server.Models;

public class UserService
{
    private readonly SqlServerDAL _dal;
    private readonly PasswordHasher<RegisterRequest> _passwordHasher = new();

    private readonly ConnectionManager _connectionManager;
    public UserService(SqlServerDAL dal, ConnectionManager connectionManager)
    {
        _dal = dal;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Get the users table without the passwords
    /// </summary>
    public List<UserDto> GetAllUsers()
    {
        return _dal.ExecCmdWithResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "GetAllUsers",
            reader => reader.ReadResultset(() => new UserDto
            {
                UserId = reader.GetInt32("UserId"),
                Username = reader.GetString("Username"),
                Nickname = reader.GetString("Nickname")
            }).ToList()
        );
    }

    /// <summary>
    /// Authenticate the user using the provided password
    /// </summary>
    /// <returns>Logged-in user DTO if the user information correct, else null.</returns>
    public UserDto? Authenticate(string username, string password)
    {
        // find the user using his username.
        var user = _dal.ExecCmdWithParamsAndResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "LoginUser",
            new Dictionary<string, object>
            {
                { "Username", username }
            },
            reader => reader.ReadBestResultset(() => new User {
                UserId = reader.GetInt32("UserId"),
                Nickname = reader.GetString("Nickname"),
                Password = reader.GetString("Password"),
            })
        );

        // user wasn't not found, return null
        if (user == null) return null;

        var result =  _passwordHasher.VerifyHashedPassword(new RegisterRequest(), user.Password, password);
        if (result == PasswordVerificationResult.Failed)
            return null;

        return new UserDto
        {
            UserId = user.UserId,
            Nickname = user.Nickname
        };
    }

    /// <summary>
    /// Insert a record into the Users table
    /// </summary>
    /// <param name="user">User data: username, nickname and password</param>
    /// <returns>Id of the new user</returns>
    public int Register(RegisterRequest user) 
    {
        // Hash the password before saving
        string hashedPassword = _passwordHasher.HashPassword(user, user.Password);

        int id = _dal.ExecCmdWithParamsAndResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "RegisterUser",
            new Dictionary<string, object>
            {
                { "Username", user.Username },
                { "Password", hashedPassword },
                { "Nickname", user.Nickname }
            },
            reader => reader.ReadBestResultset(()=> reader.GetInt32("UserId")
            )
        );
        return id;
    }

    /// <summary>
    /// Get a list of all users and their online status
    /// </summary>
    public List<UserChatDto> GetAllUsersChatDTOs()
    {
        // get all online users ids.
        HashSet<int> onlineUsersIds = _connectionManager.GetOnlineUsersIds().ToHashSet();

        List<UserChatDto> result = GetAllUsers()
            .Select(u => new UserChatDto
            {
                UserId = u.UserId,
                Nickname = u.Nickname,
                IsOnline = onlineUsersIds.Contains(u.UserId)
            }).ToList();

        return result;
    }

}
