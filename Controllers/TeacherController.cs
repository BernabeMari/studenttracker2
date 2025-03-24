using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StudentTracker.Models;
using StudentTracker.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace StudentTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeacherController> _logger;

        public TeacherController(ApplicationDbContext context, ILogger<TeacherController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("profilepic")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> UploadProfilePic(IFormFile profilePic)
        {
            try
            {
                if (profilePic == null || profilePic.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                // Check file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(profilePic.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest("Invalid file type. Only JPG, JPEG, PNG, and GIF files are allowed.");
                }

                // Check file size (limit to 5MB)
                if (profilePic.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("File size exceeds the limit of 5MB.");
                }

                // Get user ID from token
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                // Find teacher in database
                var teacher = await _context.Teachers.FindAsync(int.Parse(userId));
                if (teacher == null)
                {
                    return NotFound($"Teacher with ID {userId} not found.");
                }

                // Process the image file
                using (var memoryStream = new MemoryStream())
                {
                    await profilePic.CopyToAsync(memoryStream);
                    var imageBytes = memoryStream.ToArray();
                    var base64String = Convert.ToBase64String(imageBytes);

                    // Save the profile picture to the database
                    teacher.ProfilePic = base64String;
                    await _context.SaveChangesAsync();
                }

                return Ok(new { message = "Profile picture uploaded successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading teacher profile picture");
                return StatusCode(500, "An error occurred while uploading the profile picture.");
            }
        }
    }
} 