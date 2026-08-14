using AspiraHub.Data;
using AspiraHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AspiraHub.Controllers
{
    // Person-to-person messaging for Students, Companies and Admin.
    //  - Student  -> can message Admin or any Company
    //  - Company  -> can message Admin or any Student, and replies to Students in-thread
    //  - Admin    -> can message anyone, and replies to Students/Companies in-thread
    public class MessagingController : Controller
    {
        private readonly AppDbContext _db;

        public MessagingController(AppDbContext db)
        {
            _db = db;
        }

        // Inbox + (optionally) an open thread with ?with={userId}
        public async Task<IActionResult> Index(int? with)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Auth");

            int myId = GetUserId();

            // One row per conversation partner, with the latest message + unread count.
            var partnerIds = await _db.DirectMessages
                .Where(m => m.sender_id == myId || m.receiver_id == myId)
                .Select(m => m.sender_id == myId ? m.receiver_id : m.sender_id)
                .Distinct()
                .ToListAsync();

            var conversations = new List<ConversationVM>();
            foreach (var pid in partnerIds)
            {
                var last = await _db.DirectMessages
                    .Where(m => (m.sender_id == myId && m.receiver_id == pid) || (m.sender_id == pid && m.receiver_id == myId))
                    .OrderByDescending(m => m.sent_at)
                    .FirstOrDefaultAsync();
                if (last == null) continue;

                var partner = await _db.Users.FindAsync(pid);
                if (partner == null) continue;

                int unread = await _db.DirectMessages.CountAsync(m => m.sender_id == pid && m.receiver_id == myId && !m.is_read);

                conversations.Add(new ConversationVM
                {
                    PartnerId = pid,
                    PartnerName = partner.name,
                    PartnerRole = partner.role,
                    LastMessage = last.body,
                    LastMessageAt = last.sent_at,
                    UnreadCount = unread
                });
            }
            conversations = conversations.OrderByDescending(c => c.LastMessageAt).ToList();

            List<ThreadMessageVM> thread = new();
            UserLiteVM? openWith = null;

            if (with.HasValue && with.Value > 0)
            {
                var partnerUser = await _db.Users.FindAsync(with.Value);
                if (partnerUser != null && CanMessage(GetRole(), partnerUser.role))
                {
                    openWith = new UserLiteVM { UserId = partnerUser.user_id, Name = partnerUser.name, Role = partnerUser.role };

                    thread = await _db.DirectMessages
                        .Where(m => (m.sender_id == myId && m.receiver_id == with.Value) || (m.sender_id == with.Value && m.receiver_id == myId))
                        .OrderBy(m => m.sent_at)
                        .Select(m => new ThreadMessageVM
                        {
                            MessageId = m.message_id,
                            IsMine = m.sender_id == myId,
                            Body = m.body,
                            SentAt = m.sent_at
                        })
                        .ToListAsync();

                    // mark incoming messages from this partner as read
                    var unreadMsgs = await _db.DirectMessages
                        .Where(m => m.sender_id == with.Value && m.receiver_id == myId && !m.is_read)
                        .ToListAsync();
                    if (unreadMsgs.Count > 0)
                    {
                        foreach (var m in unreadMsgs) m.is_read = true;
                        await _db.SaveChangesAsync();
                    }
                }
            }

            var vm = new MessagingIndexVM
            {
                Role = GetRole() ?? "",
                Conversations = conversations,
                OpenWith = openWith,
                Thread = thread
            };

            return View(vm);
        }

        public class SendMessageRequest { public int ReceiverId { get; set; } public string Body { get; set; } = ""; }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest req)
        {
            if (!IsLoggedIn()) return Unauthorized();
            if (req == null || req.ReceiverId <= 0 || string.IsNullOrWhiteSpace(req.Body))
                return Json(new { success = false, message = "Message can't be empty." });

            var receiver = await _db.Users.FindAsync(req.ReceiverId);
            if (receiver == null) return Json(new { success = false, message = "Recipient not found." });

            var myRole = GetRole();
            if (!CanMessage(myRole, receiver.role))
                return Json(new { success = false, message = "You can't message this user." });

            int myId = GetUserId();
            if (receiver.user_id == myId)
                return Json(new { success = false, message = "You can't message yourself." });

            var msg = new DirectMessage
            {
                sender_id = myId,
                receiver_id = receiver.user_id,
                body = req.Body.Trim(),
                sent_at = DateTime.Now,
                is_read = false
            };
            _db.DirectMessages.Add(msg);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                messageId = msg.message_id,
                sentAt = msg.sent_at.ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }

        // Search for people the current user is allowed to start a new conversation with.
        public async Task<IActionResult> SearchUsers(string? q)
        {
            if (!IsLoggedIn()) return Unauthorized();

            var myRole = GetRole();
            int myId = GetUserId();

            var allowedRoles = myRole switch
            {
                "Student" => new[] { "Admin", "Company" },
                "Company" => new[] { "Admin", "Student" },
                "Admin" => new[] { "Admin", "Student", "Company" },
                _ => Array.Empty<string>()
            };

            var query = _db.Users.Where(u => allowedRoles.Contains(u.role) && u.user_id != myId && u.is_active);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var s = q.Trim().ToLower();
                query = query.Where(u => u.name.ToLower().Contains(s) || u.email.ToLower().Contains(s));
            }

            var results = await query
                .OrderBy(u => u.role).ThenBy(u => u.name)
                .Take(25)
                .Select(u => new { userId = u.user_id, name = u.name, role = u.role })
                .ToListAsync();

            return Json(results);
        }

        public async Task<IActionResult> UnreadCount()
        {
            if (!IsLoggedIn()) return Json(new { count = 0 });
            int myId = GetUserId();
            int count = await _db.DirectMessages.CountAsync(m => m.receiver_id == myId && !m.is_read);
            return Json(new { count });
        }

        // Who is allowed to message whom.
        private static bool CanMessage(string? myRole, string otherRole)
        {
            if (myRole == "Admin") return true; // admin can message/reply to everyone
            if (myRole == "Student") return otherRole is "Admin" or "Company";
            if (myRole == "Company") return otherRole is "Admin" or "Student";
            return false;
        }

        private bool IsLoggedIn() => GetRole() is "Student" or "Company" or "Admin";

        private string? GetRole() => HttpContext.Session.GetString("Role");

        private int GetUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    public class ConversationVM
    {
        public int PartnerId { get; set; }
        public string PartnerName { get; set; } = "";
        public string PartnerRole { get; set; } = "";
        public string LastMessage { get; set; } = "";
        public DateTime LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
    }

    public class ThreadMessageVM
    {
        public int MessageId { get; set; }
        public bool IsMine { get; set; }
        public string Body { get; set; } = "";
        public DateTime SentAt { get; set; }
    }

    public class UserLiteVM
    {
        public int UserId { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
    }

    public class MessagingIndexVM
    {
        public string Role { get; set; } = "";
        public List<ConversationVM> Conversations { get; set; } = new();
        public UserLiteVM? OpenWith { get; set; }
        public List<ThreadMessageVM> Thread { get; set; } = new();
    }
}
