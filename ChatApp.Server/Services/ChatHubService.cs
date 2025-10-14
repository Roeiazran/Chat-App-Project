namespace ChatApp.Server.Services;
using ChatApp.Server.Hub;
using ChatApp.Server.Models;
using Microsoft.AspNetCore.SignalR;
using System.ComponentModel.DataAnnotations;

public class ChatHubService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ConnectionManager _connectionManager;
    private readonly JwtService _jwtService;
    public ChatHubService(IHubContext<ChatHub> hubContext, ConnectionManager connectionManager, JwtService jwtService)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Adds all client connections to the groups specified by the groups ids
    /// </summary>
    /// <param name="connection">The connection id</param>
    /// <param name="groupsIds">The group id</param>
    public async Task AddClientToGroups(string connection, List<int> groupsIds) 
    {
        foreach (int id in groupsIds) 
        {
            await _hubContext.Groups.AddToGroupAsync(connection, id.ToString());
        }
    }

    /// <summary>
    /// Adds all clients connections to the group specified in groupId
    /// </summary>
    /// <param name="groupId">The Group to add the clients to.</param>
    /// <param name="clientsIds">The clients ids</param>
    public async Task AddClientsToGroup(int groupId, List<int> clientsIds)
    {
        foreach (int id in clientsIds)
        {
            // get client connections
            HashSet<string> connections = _connectionManager.GetConnections(id);

            // add each connection to the group
            foreach (string connection in connections)
            {
                await _hubContext.Groups.AddToGroupAsync(connection, groupId.ToString());
            }
        }
    }
    /// <summary>
    ///  Removes all client connections from the group specified in groupId
    /// </summary>
    public async Task RemoveClientFromGroup(int groupId, int clientId)
    {
        HashSet<string> connections = _connectionManager.GetConnections(clientId);

        foreach (string connection in connections)
        {
            await _hubContext.Groups.RemoveFromGroupAsync(connection, groupId.ToString());
        }
    }

    /// <summary>
    /// Notifies all logged-in users whenever new user registers in the app
    /// </summary>
    /// <param name="userId">The registered user id</param>
    /// <param name="nickname">The registered user nickname</param>
    /// <returns></returns>
    public async Task NotifyNewRegister(int userId, string nickname)
    {
        // prepare the user notification data
        UserChatDto newUser = new()
        {
            UserId = userId,
            Nickname = nickname,
            IsOnline = true
        };

        await _hubContext.Clients.Group("LoggedUsers")
            .SendAsync("UserRegister", newUser);
    }

    /// <summary>
    /// Notifies all logged-in users whenever a user changes their online status
    /// </summary>
    public async Task NotifyUserOnlineStatusChange(int userId, bool status)
    {
        UserChatDto user = new()
        {
            UserId = userId,
            IsOnline = status,
            Nickname = "",
        };

        await _hubContext.Clients.Group("LoggedUsers")
        .SendAsync("UserOnlineStatusChanged", user);
    }

    /// <summary>
    /// Authenticates the user using the token attached to the header.
    /// </summary>
    /// <param name="context">The Http Context of the request</param>
    /// <returns>The in-question user's id</returns>
    /// <exception cref="HubException">Authentication failed.</exception>
    public int AuthenticateAndGetUserId(HubCallerContext context)
    {

        var httpContext = context.GetHttpContext();
        if (httpContext == null) throw new HubException("Invalid connection");

        var token = httpContext.Request.Query["access_token"].ToString();
        UserDto? user = _jwtService.IsAuthorize("Bearer " + token);

        if (user == null) throw new HubException("Unauthorized");

        return user.UserId;
    }
   

    /// <summary>
    /// Validates any model argument in the hub methods
    /// </summary>
    /// <typeparam name="T">The model type.</typeparam>
    /// <param name="model">The model instance to validate</param>
    /// <exception cref="ArgumentNullException">No model giving.</exception>
    /// <exception cref="HubException">Model isn't valid.</exception>
    
    public void ValidateModel<T>(T model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        // create validation context contains the information the validator uses
        var context = new ValidationContext(model, serviceProvider: null, items: null);

        var results = new List<ValidationResult>(); // stores validation errors

        // check all properties on the model, return true if the model is valid
        bool valid = Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        if (!valid)
        {
            string errorMsg = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new HubException(errorMsg);
        }
    }
}