using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentTracker.Data;
using StudentTracker.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace StudentTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (model.UserType == "Student")
            {
                if (await _context.Students.AnyAsync(s => s.Username == model.Username))
                    return BadRequest("Username already exists");

                if (await _context.Students.AnyAsync(s => s.Email == model.Email))
                    return BadRequest("Email already exists");

                var student = new Student
                {
                    Username = model.Username,
                    Password = model.Password,
                    Email = model.Email,
                    Fullname = model.Fullname,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Registration successful" });
            }
            else if (model.UserType == "Parent")
            {
                if (await _context.Parents.AnyAsync(p => p.Username == model.Username))
                    return BadRequest("Username already exists");

                if (await _context.Parents.AnyAsync(p => p.Email == model.Email))
                    return BadRequest("Email already exists");

                var parent = new Parent
                {
                    Username = model.Username,
                    Password = model.Password,
                    Email = model.Email,
                    Fullname = model.Fullname,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Parents.Add(parent);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Registration successful" });
            }
            else if (model.UserType == "Teacher")
            {
                if (await _context.Teachers.AnyAsync(t => t.Username == model.Username))
                    return BadRequest("Username already exists");

                if (await _context.Teachers.AnyAsync(t => t.Email == model.Email))
                    return BadRequest("Email already exists");

                var teacher = new Teacher
                {
                    Username = model.Username,
                    Password = model.Password,
                    Email = model.Email,
                    Fullname = model.Fullname,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Registration successful" });
            }

            return BadRequest("Invalid user type");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (model.UserType == "Student")
            {
                var student = await _context.Students.FirstOrDefaultAsync(s => s.Username == model.Username);

                if (student == null || student.Password != model.Password)
                    return Unauthorized("Invalid username or password");

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
                    }
                });
            }
            else if (model.UserType == "Parent")
            {
                var parent = await _context.Parents.FirstOrDefaultAsync(p => p.Username == model.Username);

                if (parent == null || parent.Password != model.Password)
                    return Unauthorized("Invalid username or password");

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
                        UserType = "Parent"
                    }
                });
            }
            else if (model.UserType == "Teacher")
            {
                var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Username == model.Username);

                if (teacher == null || teacher.Password != model.Password)
                    return Unauthorized("Invalid username or password");

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
                    }
                });
            }

            return BadRequest("Invalid user type");
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
    }
} 