using AspiraHub.Data;
using AspiraHub.DTOs;
using AspiraHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace AspiraHub.Controllers.Api
{
    // Mobile-facing mirror of ContactController's public "Send us a
    // message" form. No login required — same as the website version.
    [ApiController]
    [Route("api/contact")]
    public class ApiContactController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ApiContactController(AppDbContext db)
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

        [HttpPost("send")]
        public async Task<IActionResult> Send(SendContactRequest req)
        {
            if (req == null)
                return BadRequest(ApiResponse<string>.Fail("Invalid request."));

            var name = (req.FullName ?? "").Trim();
            var email = (req.Email ?? "").Trim();
            var message = (req.Message ?? "").Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(ApiResponse<string>.Fail("Please enter your name."));

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return BadRequest(ApiResponse<string>.Fail("Please enter a valid email address."));

            if (string.IsNullOrWhiteSpace(message))
                return BadRequest(ApiResponse<string>.Fail("Please enter a message."));

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

            return Ok(ApiResponse<string>.Ok("", "Message sent"));
        }
    }
}
