using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Cors;

namespace StudentTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public StudentController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("profilepic")]
        [EnableCors("AllowAll")]
        public async Task<IActionResult> UploadProfilePic(IFormFile profilePic)
        {
            try
            {
                Console.WriteLine("\n===== PROFILE PIC UPLOAD REQUEST =====");
                Console.WriteLine($"Time: {DateTime.Now}");
                Console.WriteLine($"Headers:");
                foreach (var header in Request.Headers)
                {
                    Console.WriteLine($"  {header.Key}: {header.Value}");
                }
                
                Console.WriteLine($"Request.Form.Count: {Request.Form.Count}");
                foreach (var key in Request.Form.Keys)
                {
                    Console.WriteLine($"Form key: {key}");
                }
                
                Console.WriteLine($"Request.Form.Files.Count: {Request.Form.Files.Count}");
                foreach (var file in Request.Form.Files)
                {
                    Console.WriteLine($"File: {file.Name}, {file.FileName}, {file.ContentType}, {file.Length} bytes");
                }
                
                // Check for token in form if no authorization header is present
                string jwtToken = null;
                if (!Request.Headers.ContainsKey("Authorization"))
                {
                    Console.WriteLine("No Authorization header found, checking form for token");
                    if (Request.Form.ContainsKey("token"))
                    {
                        jwtToken = Request.Form["token"];
                        Console.WriteLine($"Found token in form: {jwtToken}");
                    }
                    else
                    {
                        Console.WriteLine("No token found in form");
                    }
                }
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                Console.WriteLine($"User ID: {userIdClaim.Value}, Type: {userTypeClaim.Value}");
                var currentUserId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Student")
                {
                    Console.WriteLine("Not a student user");
                    return BadRequest(new { message = "Only students can update their profile picture" });
                }

                var student = await _context.Students.FindAsync(currentUserId);
                if (student == null)
                {
                    Console.WriteLine("Student not found");
                    return NotFound(new { message = "Student not found" });
                }

                Console.WriteLine($"Found student: {student.Username}");

                if (profilePic == null)
                {
                    Console.WriteLine("Profile pic parameter is null, checking Request.Form.Files");
                    
                    if (Request.Form.Files.Count > 0)
                    {
                        profilePic = Request.Form.Files[0];
                        Console.WriteLine($"Found file from form: {profilePic.FileName}, {profilePic.ContentType}, {profilePic.Length} bytes");
                    }
                    else
                    {
                        Console.WriteLine("No files found in request");
                        return BadRequest(new { message = "No file was uploaded" });
                    }
                }

                if (profilePic.Length == 0)
                {
                    Console.WriteLine("Profile pic is empty (zero bytes)");
                    return BadRequest(new { message = "Empty file uploaded" });
                }

                Console.WriteLine($"Processing file: {profilePic.FileName}, {profilePic.ContentType}, {profilePic.Length} bytes");

                // Check if file is an image
                if (!IsImageFile(profilePic.FileName))
                {
                    Console.WriteLine($"Invalid file type: {Path.GetExtension(profilePic.FileName)}");
                    return BadRequest(new { message = "Only image files are allowed" });
                }

                // Check file size - limit to 1MB
                if (profilePic.Length > 1024 * 1024)
                {
                    Console.WriteLine($"File too large: {profilePic.Length} bytes");
                    return BadRequest(new { message = "File size should be less than 1MB" });
                }

                // Convert image to base64 string
                string base64String;
                using (var memoryStream = new MemoryStream())
                {
                    await profilePic.CopyToAsync(memoryStream);
                    byte[] imageBytes = memoryStream.ToArray();
                    Console.WriteLine($"Read {imageBytes.Length} bytes");
                    
                    string fileExtension = Path.GetExtension(profilePic.FileName).Replace(".", "").ToLower();
                    if (string.IsNullOrEmpty(fileExtension))
                    {
                        // Default to jpg if no extension
                        fileExtension = "jpeg";
                    }
                    
                    // Make sure it's a valid MIME type
                    string mimeType;
                    switch (fileExtension)
                    {
                        case "jpg":
                        case "jpeg":
                            mimeType = "image/jpeg";
                            break;
                        case "png":
                            mimeType = "image/png";
                            break;
                        case "gif":
                            mimeType = "image/gif";
                            break;
                        default:
                            mimeType = $"image/{fileExtension}";
                            break;
                    }
                    
                    base64String = $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
                    Console.WriteLine($"Created base64 string with length: {base64String.Length}");
                }

                // Update student record
                student.ProfilePic = base64String;
                await _context.SaveChangesAsync();
                Console.WriteLine("Profile picture updated in database successfully");
                Console.WriteLine("===== END PROFILE PIC UPLOAD REQUEST =====\n");

                // Return success response with profilePicUrl
                return Ok(new { 
                    message = "Profile picture updated successfully", 
                    profilePicUrl = base64String 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION IN UPLOADPROFILEPIC: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpGet("profilepic/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProfilePic(int id)
        {
            try
            {
                Console.WriteLine($"GetProfilePic called for student ID: {id}");
                
                var student = await _context.Students.FindAsync(id);
                if (student == null)
                {
                    Console.WriteLine("Student not found");
                    return NotFound(new { message = "Student not found" });
                }
                
                if (string.IsNullOrEmpty(student.ProfilePic))
                {
                    Console.WriteLine("Student has no profile picture");
                    return NotFound(new { message = "Student has no profile picture" });
                }
                
                Console.WriteLine("Returning profile picture");
                // If it doesn't already have a data URL prefix, add one for base64 images
                string profilePicUrl = student.ProfilePic;
                if (!profilePicUrl.StartsWith("data:"))
                {
                    // Assuming JPEG format - adjust if needed based on your application
                    profilePicUrl = $"data:image/jpeg;base64,{profilePicUrl}";
                }
                
                return Ok(new { profilePicUrl = profilePicUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetProfilePic: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        
        [HttpGet("profilepic")]
        public async Task<IActionResult> GetCurrentUserProfilePic()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }
                
                var currentUserId = int.Parse(userIdClaim.Value);
                return await GetProfilePic(currentUserId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetCurrentUserProfilePic: {ex.Message}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPut("update-profile")]
        [EnableCors("AllowAll")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileModel model)
        {
            try
            {
                Console.WriteLine("\n===== PROFILE UPDATE REQUEST =====");
                Console.WriteLine($"Time: {DateTime.Now}");
                Console.WriteLine($"Headers:");
                foreach (var header in Request.Headers)
                {
                    Console.WriteLine($"  {header.Key}: {header.Value}");
                }
                
                Console.WriteLine($"Request body: {System.Text.Json.JsonSerializer.Serialize(model)}");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                Console.WriteLine($"User ID: {userIdClaim.Value}, Type: {userTypeClaim.Value}");
                var currentUserId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Student")
                {
                    Console.WriteLine("Not a student user");
                    return BadRequest(new { message = "Only students can update their profile" });
                }

                var student = await _context.Students.FindAsync(currentUserId);
                if (student == null)
                {
                    Console.WriteLine("Student not found");
                    return NotFound(new { message = "Student not found" });
                }

                Console.WriteLine($"Found student: {student.Username}");
                
                // Validate username is not already taken (if changed)
                if (model.Username != student.Username)
                {
                    var existingUser = await _context.Students
                        .FirstOrDefaultAsync(s => s.Username == model.Username);
                    
                    if (existingUser != null)
                    {
                        Console.WriteLine($"Username {model.Username} is already taken");
                        return BadRequest(new { message = "Username is already taken" });
                    }
                }
                
                // Update student properties
                student.Username = model.Username ?? student.Username;
                student.Fullname = model.Fullname ?? student.Fullname;
                
                // Save changes to database
                await _context.SaveChangesAsync();
                Console.WriteLine("Profile updated successfully");
                Console.WriteLine("===== END PROFILE UPDATE REQUEST =====\n");

                return Ok(new { 
                    message = "Profile updated successfully",
                    user = new {
                        userId = student.StudentId,
                        student.Username,
                        student.Email,
                        student.Fullname,
                        student.ProfilePic,
                        UserType = "Student"
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION IN UPDATEPROFILE: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        
        [HttpPost("update-profile")]
        [EnableCors("AllowAll")]
        public async Task<IActionResult> UpdateProfilePost([FromBody] UpdateProfileModel model)
        {
            // For clients that only support POST, redirect to the PUT endpoint
            return await UpdateProfile(model);
        }
        
        [HttpPut("change-password")]
        [EnableCors("AllowAll")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                Console.WriteLine("\n===== PASSWORD CHANGE REQUEST =====");
                Console.WriteLine($"Time: {DateTime.Now}");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                {
                    Console.WriteLine("User not authenticated");
                    return Unauthorized(new { message = "User not authenticated" });
                }

                Console.WriteLine($"User ID: {userIdClaim.Value}, Type: {userTypeClaim.Value}");
                var currentUserId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Student")
                {
                    Console.WriteLine("Not a student user");
                    return BadRequest(new { message = "Only students can change their password" });
                }

                var student = await _context.Students.FindAsync(currentUserId);
                if (student == null)
                {
                    Console.WriteLine("Student not found");
                    return NotFound(new { message = "Student not found" });
                }

                Console.WriteLine($"Found student: {student.Username}");
                
                // Verify current password
                if (student.Password != model.CurrentPassword)
                {
                    Console.WriteLine("Current password is incorrect");
                    return BadRequest(new { message = "Current password is incorrect" });
                }
                
                // Validate new password
                if (string.IsNullOrEmpty(model.NewPassword) || model.NewPassword.Length < 6)
                {
                    Console.WriteLine("New password is too short");
                    return BadRequest(new { message = "New password must be at least 6 characters long" });
                }
                
                if (model.NewPassword != model.ConfirmPassword)
                {
                    Console.WriteLine("New password and confirmation do not match");
                    return BadRequest(new { message = "New password and confirmation do not match" });
                }
                
                // Update password
                student.Password = model.NewPassword;
                
                // Save changes to database
                await _context.SaveChangesAsync();
                Console.WriteLine("Password changed successfully");
                Console.WriteLine("===== END PASSWORD CHANGE REQUEST =====\n");

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION IN CHANGEPASSWORD: {ex.Message}");
                Console.WriteLine($"STACK TRACE: {ex.StackTrace}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }
        
        [HttpPost("change-password")]
        [EnableCors("AllowAll")]
        public async Task<IActionResult> ChangePasswordPost([FromBody] ChangePasswordModel model)
        {
            // For clients that only support POST, redirect to the PUT endpoint
            return await ChangePassword(model);
        }

        private bool IsImageFile(string fileName)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
    
    // Model classes for requests
    public class UpdateProfileModel
    {
        public string? Username { get; set; }
        public string? Fullname { get; set; }
    }
    
    public class ChangePasswordModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
} 