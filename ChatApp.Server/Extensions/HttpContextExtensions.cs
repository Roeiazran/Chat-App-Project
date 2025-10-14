namespace ChatApp.Server.Extensions;

/// <summary>
/// Extensions of Http Context
/// </summary>
public static class HttpContextExtensions
{
    public static int GetUserId(this HttpContext context)
    {
        // try to get the userId from the request pipeline and cast it to int
        if (context.Items.TryGetValue("UserId", out var id) && id is int userId)
        {
            return userId;
        }

        // throw an exception if userId is missing
        throw new UnauthorizedAccessException("User ID not found in context.");
    }

    public static string GetUserNickname(this HttpContext context)
    {
        if (context.Items.TryGetValue("Nickname", out var nickname) && nickname is string nickName)
        {
            return nickName;
        }

        throw new UnauthorizedAccessException("Nickname not found in context.");
    }
}
