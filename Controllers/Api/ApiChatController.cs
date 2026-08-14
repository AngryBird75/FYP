using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mobile-facing mirror of ChatController. The website renders a
    // widget partial view with the history baked in; the app instead
    // fetches history once, then posts new messages one at a time.
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ApiChatController : ControllerBase
    {
        private readonly IChatBotService _chat;

        public ApiChatController(IChatBotService chat)
        {
            _chat = chat;
        }

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        private string Role => User.FindFirst(ClaimTypes.Role)?.Value ?? "";

        [HttpGet("history")]
        public async Task<IActionResult> History()
        {
            var history = await _chat.GetHistoryAsync(UserId);
            return Ok(ApiResponse<object>.Ok(history));
        }

        public class SendChatRequest { public string Message { get; set; } = ""; }

        [HttpPost("send")]
        public async Task<IActionResult> Send(SendChatRequest req)
        {
            if (string.IsNullOrWhiteSpace(req?.Message))
                return BadRequest(ApiResponse<object>.Fail("Message is required."));

            var reply = await _chat.SendMessageAsync(UserId, Role, req.Message.Trim());
            return Ok(ApiResponse<object>.Ok(new { reply = reply.message, sentAt = reply.sent_at }));
        }
    }
}
