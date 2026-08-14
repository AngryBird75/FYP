using AspiraHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatBotService _chat;

        public ChatController(IChatBotService chat)
        {
            _chat = chat;
        }

        // Renders the chat widget markup for whichever role is logged in.
        public async Task<IActionResult> Widget()
        {
            var role = GetRole();
            if (role == null) return Content("");

            ViewBag.Role = role;
            var history = await _chat.GetHistoryAsync(GetUserId());
            return PartialView("_ChatWidget", history);
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendChatRequest req)
        {
            var role = GetRole();
            if (role == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(req?.Message))
                return BadRequest(new { error = "Message is required." });

            var reply = await _chat.SendMessageAsync(GetUserId(), role, req.Message.Trim());
            return Json(new { reply = reply.message, sentAt = reply.sent_at });
        }

        private string? GetRole()
        {
            var role = HttpContext.Session.GetString("Role");
            return role is "Student" or "Company" or "Admin" ? role : null;
        }

        private int GetUserId()
            => HttpContext.Session.GetInt32("UserId") ?? 0;
    }

    public class SendChatRequest
    {
        public string Message { get; set; } = "";
    }
}
