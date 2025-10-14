namespace ChatApp.Server.Controllers;
using Microsoft.AspNetCore.Mvc;
using ChatApp.Server.Models;
using ChatApp.Server.Services;
using ChatApp.Server.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[ServiceFilter(typeof(JwtAuthorizationFilter))]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly UserService _userService;

    public ChatController(ChatService chatService, UserService userService)
    {
        _chatService = chatService;
        _userService = userService;
    }

    /// <summary>
    ///  Called to get the initial state of the the app. 
    /// </summary>
    /// <returns>
    /// Two lists of chats, off and ongoing chats, and a list of users.
    /// </returns>
    [HttpGet]
    public IActionResult Index()
    {
        int userId = HttpContext.GetUserId();

        List<ChatDto> onGoingChats = _chatService.GetUserOnGoingChats(userId);
        List<UserChatDto> availableUsers = _userService.GetAllUsersChatDTOs()
        .Where(u => u.UserId != userId).ToList();

        // list of all user ids for users who participate in a private chat with the current user
        var privateChatUserIds = onGoingChats
        .Where(c => !c.IsGroup)             // filter out the group chats
        .SelectMany(c => c.Participants)    // flatten the participant ids array
        .Where(p => p != userId)            // exclude current user
        .ToHashSet();                       // fast lookup

        // list of all private chats the current user does not participate in
        List<ChatDto> offGoingChats = availableUsers
        .Where(u => !privateChatUserIds.Contains(u.UserId)) // exclude users appearing in privateChat list
        .Select(u => new ChatDto
        {
            ChatName = "",
            IsGroup = false,
            Participants = new List<int> { u.UserId, userId },
            UnreadCount = 0,
            LastUpdated = null
        }).ToList();

        return Ok(new { 
            onGoingChats, 
            offGoingChats, 
            Users = availableUsers 
        });
    }

    /// <summary>
    /// Get all messages for the chat and update the chat's last visited time.
    /// </summary>
    /// <returns>Messages list</returns>
    [HttpGet("{chatId}/messages")]
    public IActionResult GetMessages(int chatId)
    {
        List<Message> messages = _chatService.GetMessagesForChat(chatId);
        return Ok(new { messages });
    }

    /// <summary>
    /// Update the chat's last visited time.
    /// </summary>
    [HttpPost("{chatId}/updateLastVisited")]
    public IActionResult UpdateChatLastVisited(int chatId, [FromQuery] DateTime date)
    {
        int userId = HttpContext.GetUserId();
        _chatService.UpdateChatLastVisited(userId, chatId, date);

        return Ok();
    }

    /// <summary>
    /// Called to get message's hourly reports.
    /// </summary>
    /// <param name="request">Date object of the day in question</param>
    /// <returns>List of hourly reports</returns>
    [HttpGet("report")]
    public IActionResult GetMessageReport([FromQuery] ReportRequest request)
    {
        // Computing the UTC offset in hours from local time
        TimeZoneInfo israelTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Israel Standard Time");
        TimeSpan offset = israelTimeZone.GetUtcOffset(DateTime.UtcNow);
        int hoursOffset = offset.Hours;

        // Get reports for today and yesterday to account for time zone shifts
        DateTime date = request.Date;
        List<HourReport> utcYesterdayReports = _chatService.GetHourReports(date.AddDays(-1));
        List<HourReport> utcTodayReports = _chatService.GetHourReports(date);

        // Extract the relevant reports from yesterday's data
        IEnumerable<HourReport> yesterdayPart = utcYesterdayReports
            .Where(r => (r.Hour + hoursOffset) >= 24)
            .Select(r =>
            {
                r.Hour = (r.Hour + hoursOffset) % 24;
                return r;
            });

        // Extract the relevant reports from today's data
        IEnumerable<HourReport> todayPart = utcTodayReports
            .Where(r => (r.Hour + hoursOffset) < 24)
            .Select(r =>
            {
                r.Hour = (r.Hour + hoursOffset) % 24;
                return r;
            });

        // Concatenate yesterday's and today's relevant reports and sort them by hour
        List<HourReport> reports = yesterdayPart.Concat(todayPart).OrderBy(r => r.Hour).ToList();
        return Ok(new { reports });
    }

    
}
