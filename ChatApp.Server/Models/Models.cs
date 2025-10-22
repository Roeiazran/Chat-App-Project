using System;
using System.ComponentModel.DataAnnotations;
namespace ChatApp.Server.Models
{
    public class RefCountedLock
    {
        public object LockObj { get; set; }
        private int RefCount;

        public int Decrement() => Interlocked.Decrement(ref RefCount);
        public int Increment() => Interlocked.Increment(ref RefCount);
    }
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Nickname { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginUser
    {
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(100, ErrorMessage = "Username is too long")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Password is required")]
        [MaxLength(256, ErrorMessage = "Password is too long")]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "Nickname is required")]
        [MaxLength(100, ErrorMessage = "Nickname is too long")]
        public string Nickname { get; set; } = "";
    }
  
    public class Chat
    {
        public int ChatId { get; set; }
        public string ChatName { get; set; } = "";

        public bool IsGroup { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ChatParticipant {
        public int UserId { get; set; }
        public int ChatId { get; set; }
        public DateTime LastVisited { get; set; }
    }
    
    public class UserChatDto {
        public int UserId { get; set; }
        public string Nickname { get; set; } = "";
        public bool IsOnline { get; set; }
    }
    
    public class UserDto {
        public int UserId { get; set; }
        public string Nickname { get; set; } = "";
        public string Username { get; set; } = "";
    }

    public class ChatDto
    {
        public int? ChatId { get; set; }
        public string ChatName { get; set; } = "";
        public DateTime? LastUpdated { get; set; }
        public List<int> Participants { get; set; } = [];
        public bool IsGroup { get; set; }
        public int UnreadCount { get; set; }
    }
    public class Message
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public int SenderId { get; set; }
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; }
    }

    public class SendMessageRequest
    {
        [Required(ErrorMessage = "Chat id is required")]
        public int ChatId { get; set; }
        [Required(ErrorMessage = "Message content is required")]
        public string Content { get; set; } = "";

        [Required(ErrorMessage="Sent date is required")]
        public DateTime SentAt { get; set; }
    }

    public class LeaveGroupRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ChatId must be greater than 0.")]
        public int ChatId { get; set; }
    }

    public class CreateChatRequest : IValidatableObject
    {
        [MaxLength(100, ErrorMessage = "Chat name has at most 100 characters")]
        public string Name { get; set; } = "";

        [Required]
        [MinLength(1, ErrorMessage = "You must select at least one participant.")]
        public List<int> ParticipantsIds { get; set; } = [];

        [Required(ErrorMessage = "Group type is required")]
        public bool IsGroup { get; set; }

        [Required(ErrorMessage="Update date is required")]
        public DateTime UpdatedAt { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (IsGroup && string.IsNullOrWhiteSpace(Name))
            {
                yield return new ValidationResult(
                    "Group chat must have a name.",
                    new[] { nameof(Name) }
                );
            }
        }
    }

    public class HourReport
    {
        public int Hour { get; set; }
        public double AvgMessageLength { get; set; }
    }

    public class ReportRequest
    {
        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date, ErrorMessage = "Invalid date format.")]
        public DateTime Date { get; set; }
    }

    public class RefreshToken 
    {
        public int UserId { get; set; }
        public string Token { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

}
