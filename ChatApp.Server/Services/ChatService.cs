namespace ChatApp.Server.Services;

using System;
using System.Collections.Generic;
using ChatApp.Server.DAL;
using ChatApp.Server.Models;

public class ChatService
{
    private readonly SqlServerDAL _dal;

    public ChatService(SqlServerDAL dal)
    {
        _dal = dal;
    }

    /// <summary>
    /// Get all chats in the data base.
    /// </summary>
    public List<Chat> GetAllChats()
    {
        return _dal.ExecCmdWithResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "GetAllChats",
            reader => reader.ReadResultset(() => new Chat
            {
                ChatId = reader.GetInt32("ChatId"),
                ChatName = reader.GetString("ChatName"),
                IsGroup = reader.GetBoolean("IsGroup"),
                LastUpdated = reader.GetDateTime("LastUpdated")
            }).ToList()
        );
    }

    /// <summary>
    /// Get all chat participants in the data base.
    /// </summary>
    public List<ChatParticipant> GetAllChatParticipants()
    {
        return _dal.ExecCmdWithResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "GetAllChatParticipants",
            reader => reader.ReadResultset(() => new ChatParticipant
            {
                UserId = reader.GetInt32("UserId"),
                ChatId = reader.GetInt32("ChatId"),
                LastVisited = reader.GetDateTime("LastVisited")
            }).ToList()
        );
    }

    /// <summary>
    /// Get all the messages in the data base.
    /// </summary>
    public List<Message> GetAllMessages()
    {
        return _dal.ExecCmdWithResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "GetAllMessages",
            reader => reader.ReadResultset(() => new Message
            {
                MessageId = reader.GetInt32("MessageId"),
                ChatId = reader.GetInt32("ChatId"),
                SenderId = reader.GetInt32("SenderId"),
                Content = reader.GetString("Content"),
                SentAt = DateTime.SpecifyKind(reader.GetDateTime("SentAt"), DateTimeKind.Utc),
            }).ToList()
        );
    }

    /// <summary>
    /// Get all chat ids the user participates in
    /// </summary>
    public List<int> GetUserChatIds(int userId)
    {
        return GetAllChatParticipants()
        .Where(cp => cp.UserId == userId)
        .Select(cp => cp.ChatId)
        .ToList();
    }


    /// <summary>
    /// Get a list of chat DTOs the user participates in
    /// </summary>
    public List<ChatDto> GetUserOnGoingChats(int userId)
    {
        // pre-group participants and messages by ChatId
        ILookup<int, ChatParticipant>  participantsByChat = GetAllChatParticipants().ToLookup(cp => cp.ChatId);
        ILookup<int, Message> messagesByChat = GetAllMessages().ToLookup(m => m.ChatId);

        return GetAllChats()
            // only chats where the user participates
            .Where(c => participantsByChat[c.ChatId].Any(cp => cp.UserId == userId))
            .Select(c => new ChatDto
            {
                ChatId = c.ChatId,
                ChatName = c.ChatName,
                IsGroup = c.IsGroup,
                LastUpdated =  DateTime.SpecifyKind(c.LastUpdated, DateTimeKind.Utc),

                Participants = participantsByChat[c.ChatId]
                                .Select(cp => cp.UserId)
                                .ToList(),

                // count unread messages from other participants
                UnreadCount = messagesByChat[c.ChatId]                      // filter for all the messages in the chat. 
                                .Where(m => m.SenderId != userId            // sended by the other participants
                                            && participantsByChat[c.ChatId]
                                            // user's last visited time is < message sent at.
                                                .Any(cp => cp.UserId == userId 
                                                        && cp.LastVisited <= m.SentAt))
                                .Count()
            }).OrderByDescending(c => c.LastUpdated)
            .ToList();
    }

    /// <summary>
    /// Get all chat messages, ordered by SentAt ascending
    /// </summary>
    public List<Message> GetMessagesForChat(int chatId)
    {
        return GetAllMessages()
        .Where(m => m.ChatId == chatId)
        .OrderBy(m => m.SentAt)
        .ToList();
    }

    /// <summary>
    /// Add message to Messages table, and update the chat's last update time
    /// </summary>
    /// <param name="userId">The sender id</param>
    /// <param name="chatId">The chat id</param>
    /// <param name="content">The message content</param>
    /// <returns>The id of the new message</returns>
    public int AddMessage(int userId, int chatId, string content, DateTime date)
    {
        return _dal.ExecCmdWithParamsAndResult(
        SqlServerDAL.CommandType.StoredProcedure,
        "AddMessage",
        new Dictionary<string, object> {
            { "ChatId", chatId },
            { "Content", content },
            { "SenderId", userId },
            { "Date", date }
        },
        reader => reader.ReadBestResultset(() => reader.GetInt32("MessageId"))
        );
    }

    /// <summary>
    /// // Check if a both users share a private chat
    /// <returns>Chat id if found, else 0</returns>
    public int FindPrivateChat(int user1Id, int user2Id)
    {
        List<Chat> chats = GetAllChats();
        ILookup<int,ChatParticipant> chatParticipants = GetAllChatParticipants()
            .ToLookup(cp => cp.UserId);

        HashSet<int> user1Chats = chatParticipants[user1Id]
            .Select(cp => cp.ChatId).ToHashSet();
        HashSet<int> user2Chats = chatParticipants[user2Id]
            .Select(cp => cp.ChatId).ToHashSet();

        return chats
            .Where(c => !c.IsGroup && user1Chats.Contains(c.ChatId) && user2Chats.Contains(c.ChatId))
            .Select(c => c.ChatId)
            .FirstOrDefault();
    }

    /// <summary>
    /// Create new chat in the database.
    /// </summary>
    /// <returns>The id of the new chat</returns>
    public int CreateChat(string name, bool isGroup, DateTime date)
    {
        return _dal.ExecCmdWithParamsAndResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "CreateChat",
            new Dictionary<string, object>
            {
                { "Name", name },
                { "IsGroup", isGroup },
                { "Date", date }
            },
            reader => reader.ReadBestResultset(()=> reader.GetInt32("ChatId"))
        );
    }

    /// <summary>
    /// Insert many participants into the ChatParticipants table.
    /// </summary>
    /// <param name="participantsIds">Array of ids</param>
    public void InsertParticipantsIntoChat(int chatId, List<int> participantsIds, DateTime date)
    {
        foreach (int id in participantsIds)
        {
            _dal.ExecCmdWithParams(
                SqlServerDAL.CommandType.StoredProcedure,
                "InsertParticipantIntoChat",
                new Dictionary<string, object>
                {
                    { "UserId", id },
                    { "ChatId", chatId },
                    { "Date", date }
                }
            );
        }
    }

    /// <summary>
    ///  Remove a record from the ChatParticipants table.
    ///  Deletes from Chats and Messages if no participants left.
    /// </summary>
    /// <returns>The number of participants left in the chat.</returns>
    public int DeleteParticipantFromChat(int userId, int chatId)
    {
        // remove the record from chatParticipants table
        _dal.ExecCmdWithParams(
            SqlServerDAL.CommandType.StoredProcedure,
            "DeleteParticipantFromChat",
            new Dictionary<string, object>
            {
                { "UserId", userId },
                { "ChatId", chatId }
            }
        );
        
        // find how many participants are left in the chat
        int participantsCount = GetAllChatParticipants()
        .Where(cp => cp.ChatId == chatId).Count();

        if (participantsCount == 0)
        {
            _dal.ExecCmdWithParams(
                SqlServerDAL.CommandType.StoredProcedure,
                "DeleteChat",
                new Dictionary<string, object>
                {
                    { "ChatId", chatId }
                }
            );
        }
        return participantsCount;
    }

    /// <summary>
    /// Update the chat's last visit time in the ChatParticipants table
    /// </summary>
    public void UpdateChatLastVisited(int userId, int chatId, DateTime date)
    {
        _dal.ExecCmdWithParams(
            SqlServerDAL.CommandType.StoredProcedure,
            "UpdateChatLastVisited",
            new Dictionary<string, object> {
                { "UserId", userId },
                { "ChatId", chatId },
                { "Date", date }
            }
        );
    }

    public void UpdateChatLastUpdated(int chatId, DateTime date)
    {
        _dal.ExecCmdWithParams(
            SqlServerDAL.CommandType.StoredProcedure,
            "UpdateChatLastUpdated",
            new Dictionary<string, object>
            {
                { "ChatId", chatId },
                { "Date", date }
            }
        );
    }

    /// <summary>
    /// Get a list of hourly reports for each hour of the specified date
    /// </summary>
    public List<HourReport> GetHourReports(DateTime date) {

        List<HourReport> reports = _dal.ExecCmdWithParamsAndResult(
            SqlServerDAL.CommandType.StoredProcedure,
            "GetAvgMessageLengthPerHour",
            new Dictionary<string, object> {
                { "TargetDate" , date },
            },
            reader => reader.ReadResultset(() => new HourReport
            {
                Hour = reader.GetInt32("Hour"),
                AvgMessageLength = reader.GetDouble("AvgMessageLength")
            })
        ).ToList();

        return reports;
    }


}
