using System.Linq;
using System.Security.Claims;
using AspiraHub.DTOs;
using AspiraHub.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class ApiNotificationController : ControllerBase
    {
        private readonly IDashboardRepository _dashboard;
        public ApiNotificationController(IDashboardRepository dashboard) => _dashboard = dashboard;

        private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int take = 20)
        {
            var list = await _dashboard.GetNotificationsAsync(UserId, take);

            // Same projection as ApiStudentController.GetDashboard() — the raw
            // EF model's field names (notif_id, body, is_read, created_at)
            // don't line up with the Android app's NotificationDto (id,
            // message, isRead, createdAt). Without this, every notification
            // silently gets id=0 (Gson leaves missing fields at their Kotlin
            // default) and the NotificationsScreen LazyColumn crashes with
            // "Key '0' was already used".
            var payload = list.Select(n => new
            {
                id = n.notif_id,
                message = n.body ?? n.title,
                isRead = n.is_read,
                createdAt = n.created_at
            });

            return Ok(ApiResponse<object>.Ok(payload));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var count = await _dashboard.GetUnreadCountAsync(UserId);
            return Ok(ApiResponse<int>.Ok(count));
        }

        [HttpPost("{notifId:int}/read")]
        public async Task<IActionResult> MarkRead(int notifId)
        {
            await _dashboard.MarkNotificationReadAsync(notifId, UserId);
            return Ok(ApiResponse<string>.Ok("", "Marked read"));
        }

        // Called once after login / on app start so the backend can push
        // via Firebase Cloud Messaging to this device later. Store the
        // token against the user (add a DeviceTokens table — see README).
        [HttpPost("register-device")]
        public async Task<IActionResult> RegisterDevice(RegisterDeviceRequest req)
        {
            // TODO: persist (UserId, req.fcmToken) in a DeviceTokens table
            // via a small repository method, so a background job / other
            // controllers can call the Firebase Admin SDK to send pushes
            // (e.g. "new message", "application status changed").
            return Ok(ApiResponse<string>.Ok("", "Device registered"));
        }
    }
}