using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mirrors Views/Messaging/Index.cshtml exactly: conversations are
    // grouped by "partner" (the other user), not a conversation id. Matches
    // the routes the Android app calls in ApiService.kt: messaging/conversations,
    // messaging/thread?with={userId}, messaging/search-users?q=, messaging/send.
    [ApiController]
    [Route("api/messaging")]
    [Authorize(Roles = "Student")]
    public class ApiMessagingController : ControllerBase
    {
        private readonly IMessagingRepository _messaging;

        public ApiMessagingController(IMessagingRepository messaging)
        {
            _messaging = messaging;
        }

        private int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet("conversations")]
        public async Task<IActionResult> Conversations()
        {
            var conversations = await _messaging.GetConversationsAsync(CurrentUserId);
            return Ok(ApiResponse<List<ConversationDto>>.Ok(conversations));
        }

        [HttpGet("thread")]
        public async Task<IActionResult> Thread([FromQuery] int with)
        {
            var messages = await _messaging.GetThreadAsync(CurrentUserId, with);
            return Ok(ApiResponse<List<ChatMessageDto>>.Ok(messages));
        }

        [HttpGet("search-users")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? q)
        {
            var results = await _messaging.SearchUsersAsync(CurrentUserId, q ?? "");
            return Ok(ApiResponse<List<UserSearchResultDto>>.Ok(results));
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] SendMessageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.body))
                return BadRequest(ApiResponse<object>.Fail("Message text is required."));
            if (req.receiverId <= 0)
                return BadRequest(ApiResponse<object>.Fail("A recipient is required."));

            var sent = await _messaging.SendMessageAsync(CurrentUserId, req.receiverId, req.body);
            return Ok(ApiResponse<ChatMessageDto>.Ok(sent, "Sent"));
        }
    }
}
