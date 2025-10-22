namespace ChatApp.Server.Hub;
using Microsoft.AspNetCore.SignalR;
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using System.Collections.Concurrent;
public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly ConnectionManager _connectionManager;
    private readonly ChatHubService _chatHubService;

    private static readonly ConcurrentDictionary<string, object> _privateChatLocks = new();
    private static readonly ConcurrentDictionary<string, RefCountedLock> _chatLocks = new();

    public ChatHub(ChatService chatService, ConnectionManager connectionManager, ChatHubService chatHubService)
    {
        _chatService = chatService;
        _connectionManager = connectionManager;
        _chatHubService = chatHubService;
    }

    /// <summary>
    /// Hub lifecycle method called whenever a user connects to the app.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        int userId = _chatHubService.AuthenticateAndGetUserId(Context);
        _connectionManager.AddConnection(userId, Context.ConnectionId);

        List<int> chatIds = _chatService.GetUserChatIds(userId);

        await _chatHubService.AddClientToGroups(Context.ConnectionId, chatIds);

        if (_connectionManager.GetConnections(userId).Count == 1) {
            await _chatHubService.NotifyUserOnlineStatusChange(userId, true);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "LoggedUsers");

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Hub lifecycle method called whenever the user disconnect from the app (logout, refresh token)
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        int userId = _chatHubService.AuthenticateAndGetUserId(Context);

        _connectionManager.RemoveConnection(userId, Context.ConnectionId);

        if (_connectionManager.GetConnections(userId).Count == 0)
        {
            // store connection id here because context is not defined inside the task
            string connId = Context.ConnectionId;
            _ = Task.Run(async () =>
            {
                await Task.Delay(2000); // 2 seconds
                if (_connectionManager.GetConnections(userId).Count == 0)
                {
                    await _chatHubService.NotifyUserOnlineStatusChange(userId, false);
                    await Groups.RemoveFromGroupAsync(connId, "LoggedUsers");
                }
            });
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Create new chat method, called whenever a user initiate the creation of a new chat or a new group.
    /// /// Uses Concurrent dictionary to prevent race condition when creating private chats.
    /// </summary>
    /// <param name="request">Chat data: name, participants ids and is Group</param>
    /// <returns>The new created Chat Id</returns>
    public async Task<int> CreateChat(CreateChatRequest request)
    {
        int userId = _chatHubService.AuthenticateAndGetUserId(Context);
        _chatHubService.ValidateModel(request);
        if (request.IsGroup)
        {
            return await CreateGroupChat(request, userId);
        }
        return await CreatePrivateChat(request, userId);
    }

    public async Task<int> CreatePrivateChat(CreateChatRequest request, int userId) 
    {
        // check for existing chat with both participants, prevent race condition
        int user1Id = request.ParticipantsIds[0];
        int user2Id = request.ParticipantsIds[1];
        string key = user1Id < user2Id ? $"{user1Id}-{user2Id}" : $"{user2Id}-{user1Id}";
        object _lock = _privateChatLocks.GetOrAdd(key, _ => new object());

        int chatId;
        lock(_lock) {
            int existingChatId = _chatService.FindPrivateChat(user1Id, user2Id);

            if (existingChatId == 0)
            {
                // create new chat
                chatId = _chatService.CreateChat(request.Name, false, request.UpdatedAt);
                _chatService.InsertParticipantsIntoChat(chatId, request.ParticipantsIds, request.UpdatedAt);
            } else 
            {
                // chat already exists, no need to add the participants
                chatId = existingChatId;
            }
        };
        _privateChatLocks.TryRemove(key, out _);

        // add connections to the chat id group
        await _chatHubService.AddClientsToGroup(chatId, request.ParticipantsIds);

        ChatDto chat = new()
        {
            ChatId = chatId,
            ChatName = request.Name,
            Participants = request.ParticipantsIds,
            IsGroup = request.IsGroup,
            LastUpdated = DateTime.UtcNow
        };

        // notifying the other participants of this new chat
        await Clients.OthersInGroup(chatId.ToString()).SendAsync("NewChatCreated", new { chat, CreatorId = userId });
        
        // returning the id to the creator of the chat
        return chatId;
    }

    public async Task<int> CreateGroupChat(CreateChatRequest request, int userId)
    {
        int chatId = _chatService.CreateChat(request.Name, true, request.UpdatedAt);
        _chatService.InsertParticipantsIntoChat(chatId, request.ParticipantsIds, request.UpdatedAt);

        // add connections to the chat id group
        await _chatHubService.AddClientsToGroup(chatId, request.ParticipantsIds);

        ChatDto chat = new()
        {
            ChatId = chatId,
            ChatName = request.Name,
            Participants = request.ParticipantsIds,
            IsGroup = request.IsGroup,
            LastUpdated = DateTime.UtcNow
        };

        // notifying all participants of this new chat
        await Clients.Group(chatId.ToString()).SendAsync("NewChatCreated", new { chat, CreatorId = userId });

        return chatId;
    }

    /// <summary>
    /// Send a message to everybody in the request chat's group.
    /// </summary>
    /// <param name="request">The chat to send the message to.</param>
    public async Task SendMessage(SendMessageRequest request)
    {
        int userId = _chatHubService.AuthenticateAndGetUserId(Context);
        _chatHubService.ValidateModel(request);

        // lock the -- add-message, update chat's last updated -- 
        RefCountedLock chatLock = _chatLocks.GetOrAdd(request.ChatId.ToString(), _ => new RefCountedLock());
        // increment the reference count
        chatLock.Increment();

        int messageId;
        lock(chatLock) {
            messageId = _chatService.AddMessage(userId, request.ChatId, request.Content, request.SentAt);
            _chatService.UpdateChatLastUpdated(request.ChatId, request.SentAt);
        }

        int refs = chatLock.Decrement();
        if (refs == 0) {
            _chatLocks.TryRemove(request.ChatId.ToString(), out _);
        }
        
        await Clients.Group(request.ChatId.ToString()).SendAsync("ReceiveMessage",
            new Message
            {
                ChatId = request.ChatId,
                Content = request.Content,
                MessageId = messageId,
                SenderId = userId,
                SentAt = request.SentAt
            });
    }

    /// <summary>
    /// Called whenever a user decides to leave a group chat.
    /// /// Deletes the participant from the database.
    /// </summary>
    public async Task LeaveGroup(LeaveGroupRequest request)
    {
        _chatHubService.ValidateModel(request);
        
        int chatId = request.ChatId;
        int userId = _chatHubService.AuthenticateAndGetUserId(Context);

        await _chatHubService.RemoveClientFromGroup(chatId, userId);

        _chatService.DeleteParticipantFromChat(userId, chatId);
    }

}
