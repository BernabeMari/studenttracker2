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
    public class SubjectController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubjectController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateSubject([FromBody] SubjectCreateModel model)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var teacherId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Teacher")
                return BadRequest(new { message = "Only teachers can create subjects" });

            var teacher = await _context.Teachers.FindAsync(teacherId);
            if (teacher == null)
                return NotFound(new { message = "Teacher not found" });

            // Check if subject with same name already exists for this teacher
            bool subjectExists = await _context.Subjects
                .AnyAsync(s => s.TeacherId == teacherId && s.Name.ToLower() == model.Name.ToLower());
                
            if (subjectExists)
                return BadRequest(new { message = "A subject with this name already exists" });

            var subject = new Subject
            {
                Name = model.Name,
                Description = model.Description,
                TeacherId = teacherId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                subjectId = subject.SubjectId,
                subject.Name,
                subject.Description,
                subject.CreatedAt,
                message = "Subject created successfully"
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetSubjects()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;

            if (userType == "Teacher")
            {
                var subjects = await _context.Subjects
                    .Where(s => s.TeacherId == userId)
                    .Select(s => new
                    {
                        s.SubjectId,
                        s.Name,
                        s.Description,
                        s.CreatedAt,
                        s.UpdatedAt,
                        StudentCount = _context.StudentTeacherConnections
                            .Count(stc => stc.SubjectId == s.SubjectId && stc.Status == "Approved")
                    })
                    .ToListAsync();

                return Ok(subjects);
            }
            else if (userType == "Student")
            {
                var subjects = await _context.StudentTeacherConnections
                    .Where(stc => stc.StudentId == userId && stc.Status == "Approved")
                    .Include(stc => stc.Subject)
                    .Include(stc => stc.Teacher)
                    .Select(stc => new
                    {
                        stc.Subject.SubjectId,
                        stc.Subject.Name,
                        stc.Subject.Description,
                        Teacher = new
                        {
                            stc.Teacher.TeacherId,
                            stc.Teacher.Fullname,
                            stc.Teacher.Email
                        }
                    })
                    .ToListAsync();

                return Ok(subjects);
            }

            return BadRequest(new { message = "Invalid user type" });
        }

        [HttpGet("{subjectId}")]
        public async Task<IActionResult> GetSubjectDetails(int subjectId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;

            var subject = await _context.Subjects
                .FirstOrDefaultAsync(s => s.SubjectId == subjectId);

            if (subject == null)
                return NotFound(new { message = "Subject not found" });

            if (userType == "Teacher" && subject.TeacherId != userId)
                return Forbid();

            if (userType == "Student")
            {
                bool isEnrolled = await _context.StudentTeacherConnections
                    .AnyAsync(stc => stc.SubjectId == subjectId && stc.StudentId == userId && stc.Status == "Approved");

                if (!isEnrolled)
                    return Forbid();
            }

            var students = await _context.StudentTeacherConnections
                .Where(stc => stc.SubjectId == subjectId)
                .Include(stc => stc.Student)
                .Select(stc => new
                {
                    stc.Student.StudentId,
                    stc.Student.Username,
                    stc.Student.Fullname,
                    stc.Student.Email,
                    stc.Status,
                    stc.ConnectionId,
                    stc.CreatedAt,
                    stc.UpdatedAt
                })
                .ToListAsync();

            var teacher = await _context.Teachers
                .Where(t => t.TeacherId == subject.TeacherId)
                .Select(t => new
                {
                    t.TeacherId,
                    t.Fullname,
                    t.Email
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                subject.SubjectId,
                subject.Name,
                subject.Description,
                subject.CreatedAt,
                subject.UpdatedAt,
                Teacher = teacher,
                Students = students
            });
        }

        [HttpDelete("{subjectId}")]
        public async Task<IActionResult> DeleteSubject(int subjectId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var teacherId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Teacher")
                    return BadRequest(new { message = "Only teachers can delete subjects" });

                // Find the subject
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectId == subjectId);
                
                if (subject == null)
                    return NotFound(new { message = "Subject not found" });
                
                // Verify ownership
                if (subject.TeacherId != teacherId)
                    return Forbid();

                // Use the execution strategy pattern compatible with SQL Server retrying
                var executionStrategy = _context.Database.CreateExecutionStrategy();
                
                await executionStrategy.ExecuteAsync(async () =>
                {
                    using (var transaction = await _context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Delete attendance records related to this subject
                            var attendanceRecords = await _context.AttendanceRecords
                                .Where(ar => ar.SubjectId == subjectId)
                                .ToListAsync();
                            
                            if (attendanceRecords.Any())
                                _context.AttendanceRecords.RemoveRange(attendanceRecords);
                            
                            // 2. Delete attendance sessions related to this subject
                            var attendanceSessions = await _context.AttendanceSessions
                                .Where(s => s.SubjectId == subjectId)
                                .ToListAsync();
                            
                            if (attendanceSessions.Any())
                                _context.AttendanceSessions.RemoveRange(attendanceSessions);
                            
                            // 3. Delete student-teacher connections related to this subject
                            var connections = await _context.StudentTeacherConnections
                                .Where(stc => stc.SubjectId == subjectId)
                                .ToListAsync();
                            
                            if (connections.Any())
                                _context.StudentTeacherConnections.RemoveRange(connections);
                            
                            // 4. Finally delete the subject
                            _context.Subjects.Remove(subject);
                            
                            // Save all changes
                            await _context.SaveChangesAsync();
                            
                            // Commit transaction
                            await transaction.CommitAsync();
                        }
                        catch
                        {
                            // Transaction will automatically rollback when disposed
                            // if it hasn't been committed
                            throw;
                        }
                    }
                });
                
                return Ok(new { message = $"Subject '{subject.Name}' and all related data deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
} 