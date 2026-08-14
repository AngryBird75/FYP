using AspiraHub.Data;
using AspiraHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers
{
    // Handles the public "Send us a message" form on the landing page.
    // No login required — a visitor doesn't need an account to reach out.
    public class ContactController : Controller
    {
        private readonly AppDbContext _db;

        public ContactController(AppDbContext db)
        {
            _db = db;
        }

        public class SendContactRequest
        {
            public string FullName { get; set; } = "";
            public string Email { get; set; } = "";
            public string? InquiryType { get; set; }
            public string Message { get; set; } = "";
        }

        [HttpPost]
        public async Task<IActionResult> Send([FromBody] SendContactRequest req)
        {
            if (req == null)
                return Json(new { success = false, message = "Invalid request." });

            var name = (req.FullName ?? "").Trim();
            var email = (req.Email ?? "").Trim();
            var message = (req.Message ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "Please enter your name." });

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return Json(new { success = false, message = "Please enter a valid email address." });

            if (string.IsNullOrWhiteSpace(message))
                return Json(new { success = false, message = "Please enter a message." });

            if (name.Length > 150) name = name[..150];
            if (email.Length > 200) email = email[..200];

            _db.ContactMessages.Add(new ContactMessage
            {
                full_name = name,
                email = email,
                inquiry_type = string.IsNullOrWhiteSpace(req.InquiryType) ? "Other" : req.InquiryType!.Trim(),
                message = message,
                submitted_at = DateTime.Now,
                is_read = false
            });

            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
