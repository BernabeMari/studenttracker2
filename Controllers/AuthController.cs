using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentTracker.Data;
using StudentTracker.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace StudentTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        // Admin credentials - hardcoded for security (not stored in the database)
        private const string AdminUsername = "zyb20";
        private const string AdminPassword = "Bernabe202003!";

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromForm] RegisterModel model, 
            [FromForm(Name = "profilePic")] IFormFile? profilePic = null)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { message = string.Join(", ", errors) });
            }

            try
            {
                // Validate the profile picture if provided (only if one is actually uploaded)
                if (profilePic != null && profilePic.Length > 0 && model.UserType == "Student")
                {
                    // Check file type
                    if (!IsImageFile(profilePic.FileName))
                    {
                        return BadRequest(new { message = "Invalid file type. Only JPG, JPEG, PNG, and GIF files are allowed." });
                    }

                    // Check file size (limit to 5MB)
                    if (profilePic.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { message = "File size exceeds the limit of 5MB." });
                    }
                }

                string? profilePicBase64 = null;
                if (model.UserType == "Student")
                {
                    // Always set a default profile when no image is uploaded
                    profilePicBase64 = "default-profile";
                    
                    // Only process the profile pic if one was uploaded
                    if (profilePic != null && profilePic.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await profilePic.CopyToAsync(memoryStream);
                            var imageBytes = memoryStream.ToArray();
                            profilePicBase64 = Convert.ToBase64String(imageBytes);
                        }
                    }
                }

                if (model.UserType == "Student")
                {
                    if (await _context.Students.AnyAsync(s => s.Username == model.Username))
                        return BadRequest(new { message = "Username already exists" });

                    if (await _context.Students.AnyAsync(s => s.Email == model.Email))
                        return BadRequest(new { message = "Email already exists" });

                    var student = new Student
                    {
                        Username = model.Username,
                        Password = model.Password,
                        Email = model.Email,
                        Fullname = model.Fullname,
                        CreatedAt = DateTime.UtcNow,
                        ProfilePic = profilePicBase64
                    };

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    // Auto login after registration
                    var token = GenerateJwtToken(student.StudentId, student.Username, student.Email, "Student");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = student.StudentId,
                            student.Username,
                            student.Email,
                            student.Fullname,
                            student.ProfilePic,
                            UserType = "Student"
                        },
                        message = "Registration successful"
                    });
                }
                else if (model.UserType == "Parent")
                {
                    if (await _context.Parents.AnyAsync(p => p.Username == model.Username))
                        return BadRequest(new { message = "Username already exists" });

                    if (await _context.Parents.AnyAsync(p => p.Email == model.Email))
                        return BadRequest(new { message = "Email already exists" });

                    var parent = new Parent
                    {
                        Username = model.Username,
                        Password = model.Password,
                        Email = model.Email,
                        Fullname = model.Fullname,
                        CreatedAt = DateTime.UtcNow,
                        ProfilePic = string.Empty
                    };

                    _context.Parents.Add(parent);
                    await _context.SaveChangesAsync();

                    // Auto login after registration
                    var token = GenerateJwtToken(parent.ParentId, parent.Username, parent.Email, "Parent");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = parent.ParentId,
                            parent.Username,
                            parent.Email,
                            parent.Fullname,
                            parent.ProfilePic,
                            UserType = "Parent"
                        },
                        message = "Registration successful"
                    });
                }
                else if (model.UserType == "Teacher")
                {
                    if (await _context.Teachers.AnyAsync(t => t.Username == model.Username))
                        return BadRequest(new { message = "Username already exists" });

                    if (await _context.Teachers.AnyAsync(t => t.Email == model.Email))
                        return BadRequest(new { message = "Email already exists" });

                    var teacher = new Teacher
                    {
                        Username = model.Username,
                        Password = model.Password,
                        Email = model.Email,
                        Fullname = model.Fullname,
                        CreatedAt = DateTime.UtcNow,
                        ProfilePic = string.Empty
                    };

                    _context.Teachers.Add(teacher);
                    await _context.SaveChangesAsync();

                    // Auto login after registration
                    var token = GenerateJwtToken(teacher.TeacherId, teacher.Username, teacher.Email, "Teacher");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = teacher.TeacherId,
                            teacher.Username,
                            teacher.Email,
                            teacher.Fullname,
                            teacher.ProfilePic,
                            UserType = "Teacher"
                        },
                        message = "Registration successful"
                    });
                }

                return BadRequest(new { message = "Invalid user type" });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { message = "Registration failed. Internal server error.", error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            try
            {
                if (model.UserType == "Student")
                {
                    var student = await _context.Students.FirstOrDefaultAsync(s => s.Username == model.Username);

                    if (student == null || student.Password != model.Password)
                        return Unauthorized("Invalid username or password");

                    // Handle profile pic - set default if null or special marker
                    string profilePic = student.ProfilePic ?? "default-profile";
                    
                    var token = GenerateJwtToken(student.StudentId, student.Username, student.Email, "Student");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = student.StudentId,
                            student.Username,
                            student.Email,
                            student.Fullname,
                            ProfilePic = profilePic,
                            UserType = "Student"
                        }
                    });
                }
                else if (model.UserType == "Parent")
                {
                    var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Username == model.Username);

                    if (parent == null || parent.Password != model.Password)
                        return Unauthorized("Invalid username or password");

                    // Handle profile pic - set default if null or special marker
                    string profilePic = parent.ProfilePic ?? "default-profile";
                    
                    var token = GenerateJwtToken(parent.ParentId, parent.Username, parent.Email, "Parent");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = parent.ParentId,
                            parent.Username,
                            parent.Email,
                            parent.Fullname,
                            ProfilePic = profilePic,
                            UserType = "Parent"
                        }
                    });
                }
                else if (model.UserType == "Teacher")
                {
                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Username == model.Username);

                    if (teacher == null || teacher.Password != model.Password)
                        return Unauthorized("Invalid username or password");

                    // Handle profile pic - set default if null or special marker
                    string profilePic = teacher.ProfilePic ?? "default-profile";
                    
                    var token = GenerateJwtToken(teacher.TeacherId, teacher.Username, teacher.Email, "Teacher");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = teacher.TeacherId,
                            teacher.Username,
                            teacher.Email,
                            teacher.Fullname,
                            ProfilePic = profilePic,
                            UserType = "Teacher"
                        }
                    });
                }

                return BadRequest("Invalid user type");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"An error occurred during login: {ex.Message}");
            }
        }

        [HttpPost("adminLogin")]
        public IActionResult AdminLogin([FromBody] LoginModel model)
        {
            try
            {
                // Check hardcoded admin credentials
                if (model.Username == AdminUsername && model.Password == AdminPassword)
                {
                    var token = GenerateJwtToken(0, AdminUsername, "admin@system.com", "Admin");

                    return Ok(new
                    {
                        token,
                        user = new
                        {
                            userId = 0,
                            Username = AdminUsername,
                            Email = "admin@system.com",
                            Fullname = "System Administrator",
                            ProfilePic = "default-profile",
                            UserType = "Admin"
                        }
                    });
                }

                return Unauthorized("Invalid admin credentials");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Admin login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, $"An error occurred during admin login: {ex.Message}");
            }
        }

        private string GenerateJwtToken(int userId, string username, string email, string userType)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, userType)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Helper method to check if a file is an image
        private bool IsImageFile(string fileName)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return allowedExtensions.Contains(extension);
        }
    }
} 