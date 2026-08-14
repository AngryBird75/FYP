namespace AspiraHub.DTOs
{
    // Matches AspiraHub.Controllers.MessagingIndexVM exactly (confirmed
    // against Views/Messaging/Index.cshtml). There's no separate
    // "conversation" entity on the website — conversations are just
    // messages grouped by the other participant ("partner").
    public class ConversationDto
    {
        public int partnerId { get; set; }
        public string partnerName { get; set; } = "";
        public string partnerRole { get; set; } = "";
        public string lastMessage { get; set; } = "";
        public int unreadCount { get; set; }
    }

    public class ChatMessageDto
    {
        public bool isMine { get; set; }
        public string body { get; set; } = "";
        public string sentAt { get; set; } = "";
    }

    // Matches the website's /Messaging/SearchUsers?q= results (used by the
    // "+ New" box).
    public class UserSearchResultDto
    {
        public int userId { get; set; }
        public string name { get; set; } = "";
        public string role { get; set; } = "";
    }

    // Matches the website's POST /Messaging/Send body { receiverId, body }.
    public class SendMessageRequest
    {
        public int receiverId { get; set; }
        public string body { get; set; } = "";
    }
}
