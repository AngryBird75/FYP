using AspiraHub.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers
{
    // Powers the shared notification bell/panel in _DashboardLayout.cshtml
    // for every role (Student, Company, Admin) — the panel previously just
    // said "Loading..." forever because nothing ever called an endpoint to
    // fill it in.
    public class NotificationsController : Controller
    {
        private readonly IDashboardRepository _repo;

        public NotificationsController(IDashboardRepository repo)
        {
            _repo = repo;
        }

        [HttpGet]
        public async Task<IActionResult> Recent()
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var notifs = await _repo.GetNotificationsAsync(userId, 8);
            var unreadCount = await _repo.GetUnreadCountAsync(userId);

            return Json(new
            {
                unreadCount,
                items = notifs.Select(n => new
                {
                    id = n.notif_id,
                    title = n.title,
                    body = n.body,
                    isRead = n.is_read,
                    createdAt = n.created_at.ToString("MMM dd, hh:mm tt")
                })
            });
        }

        public class IdRequest { public int Id { get; set; } }

        [HttpPost]
        public async Task<IActionResult> MarkRead([FromBody] IdRequest req)
        {
            int userId = GetUserId();
            if (userId == 0) return Unauthorized();
            if (req == null) return Json(new { success = false });

            await _repo.MarkNotificationReadAsync(req.Id, userId);
            return Json(new { success = true });
        }

        private int GetUserId() => HttpContext.Session.GetInt32("UserId") ?? 0;
    }
}
