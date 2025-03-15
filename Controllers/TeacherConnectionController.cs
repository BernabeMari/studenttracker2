using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;

namespace StudentTracker.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TeacherConnectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TeacherConnectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestConnection([FromBody] StudentConnectionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var teacherId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Teacher")
                return BadRequest(new { message = "Only teachers can send connection requests" });

            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            var student = await _context.Students.FindAsync(request.StudentId);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var subject = await _context.Subjects.FirstOrDefaultAsync(s => 
                s.SubjectId == request.SubjectId && s.TeacherId == teacherId);
            
            if (subject == null)
                return NotFound(new { message = "Subject not found or you don't have access to it" });

            // Check for existing connection
            bool connectionExists = await _context.StudentTeacherConnections.AnyAsync(stc => 
                stc.StudentId == request.StudentId && 
                stc.TeacherId == teacherId && 
                stc.SubjectId == request.SubjectId);

            if (connectionExists)
                return BadRequest(new { message = "Connection request already exists" });

            var connection = new StudentTeacherConnection
            {
                StudentId = request.StudentId,
                TeacherId = teacherId,
                SubjectId = request.SubjectId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentTeacherConnections.Add(connection);
            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                connectionId = connection.ConnectionId,
                message = "Connection request sent successfully"
            });
        }

        [HttpPost("respond")]
        public async Task<IActionResult> RespondToRequest([FromBody] ConnectionStatusUpdateModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest(new { message = "Only students can respond to connection requests" });

            var connection = await _context.StudentTeacherConnections
                .Include(stc => stc.Subject)
                .FirstOrDefaultAsync(stc => stc.ConnectionId == model.ConnectionId);

            if (connection == null)
                return NotFound(new { message = "Connection request not found" });

            if (connection.StudentId != userId)
                return Forbid();

            if (connection.Status != "Pending")
                return BadRequest(new { message = "This request has already been processed" });

            if (model.Status != "Approved" && model.Status != "Rejected")
                return BadRequest(new { message = "Invalid status. Status must be 'Approved' or 'Rejected'" });

            connection.Status = model.Status;
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Connection request {model.Status.ToLower()} successfully",
                subjectName = connection.Subject?.Name
            });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingConnections()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;

            if (userType == "Student")
            {
                var connections = await _context.StudentTeacherConnections
                    .Where(stc => stc.StudentId == userId && stc.Status == "Pending")
                    .Include(stc => stc.Teacher)
                    .Include(stc => stc.Subject)
                    .Select(stc => new
                    {
                        stc.ConnectionId,
                        stc.Status,
                        stc.CreatedAt,
                        Subject = new
                        {
                            stc.Subject.SubjectId,
                            stc.Subject.Name,
                            stc.Subject.Description
                        },
                        Teacher = new
                        {
                            stc.Teacher.TeacherId,
                            stc.Teacher.Fullname,
                            stc.Teacher.Email
                        }
                    })
                    .ToListAsync();

                return Ok(connections);
            }
            else if (userType == "Teacher")
            {
                var connections = await _context.StudentTeacherConnections
                    .Where(stc => stc.TeacherId == userId && stc.Status == "Pending")
                    .Include(stc => stc.Student)
                    .Include(stc => stc.Subject)
                    .Select(stc => new
                    {
                        stc.ConnectionId,
                        stc.Status,
                        stc.CreatedAt,
                        Subject = new
                        {
                            stc.Subject.SubjectId,
                            stc.Subject.Name
                        },
                        Student = new
                        {
                            stc.Student.StudentId,
                            stc.Student.Fullname,
                            stc.Student.Email
                        }
                    })
                    .ToListAsync();

                return Ok(connections);
            }

            return BadRequest(new { message = "Invalid user type" });
        }

        [HttpGet("connected")]
        public async Task<IActionResult> GetConnectedUsers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;

            if (userType == "Student")
            {
                var connections = await _context.StudentTeacherConnections
                    .Where(stc => stc.StudentId == userId && stc.Status == "Approved")
                    .Include(stc => stc.Teacher)
                    .Include(stc => stc.Subject)
                    .Select(stc => new
                    {
                        stc.ConnectionId,
                        stc.CreatedAt,
                        Subject = new
                        {
                            stc.Subject.SubjectId,
                            stc.Subject.Name,
                            stc.Subject.Description
                        },
                        Teacher = new
                        {
                            stc.Teacher.TeacherId,
                            stc.Teacher.Fullname,
                            stc.Teacher.Email
                        }
                    })
                    .ToListAsync();

                return Ok(connections);
            }
            else if (userType == "Teacher")
            {
                // Group students by subject
                var subjects = await _context.Subjects
                    .Where(s => s.TeacherId == userId)
                    .Select(s => new
                    {
                        s.SubjectId,
                        s.Name,
                        s.Description,
                        Students = _context.StudentTeacherConnections
                            .Where(stc => stc.SubjectId == s.SubjectId && stc.Status == "Approved")
                            .Include(stc => stc.Student)
                            .Select(stc => new
                            {
                                stc.Student.StudentId,
                                stc.Student.Fullname,
                                stc.Student.Email
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(subjects);
            }

            return BadRequest(new { message = "Invalid user type" });
        }

        [HttpDelete("{connectionId}")]
        public async Task<IActionResult> RemoveConnection(int connectionId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var teacherId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Teacher")
                    return BadRequest(new { message = "Only teachers can remove students from subjects" });

                // Find the connection
                var connection = await _context.StudentTeacherConnections
                    .Include(stc => stc.Subject)
                    .Include(stc => stc.Student)
                    .FirstOrDefaultAsync(stc => stc.ConnectionId == connectionId);
                
                if (connection == null)
                    return NotFound(new { message = "Connection not found" });
                
                // Verify that the subject belongs to the teacher
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectId == connection.SubjectId && s.TeacherId == teacherId);
                
                if (subject == null)
                    return Forbid();
                
                // Remove the connection
                _context.StudentTeacherConnections.Remove(connection);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = $"Student {connection.Student.Fullname} removed from subject {connection.Subject.Name} successfully",
                    studentId = connection.StudentId,
                    subjectId = connection.SubjectId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
} 