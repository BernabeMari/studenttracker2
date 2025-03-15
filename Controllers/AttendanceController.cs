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
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AttendanceController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var teacherId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Teacher")
                    return BadRequest(new { message = "Only teachers can create attendance sessions" });

                // Verify the subject belongs to the teacher
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectId == request.SubjectId && s.TeacherId == teacherId);
                
                if (subject == null)
                    return NotFound(new { message = "Subject not found or you don't have access to it" });

                // Check if there's an existing active session for this subject
                var existingSession = await _context.AttendanceSessions
                    .Where(s => s.SubjectId == request.SubjectId && s.TeacherId == teacherId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                AttendanceSession session;
                
                if (existingSession != null)
                {
                    // Reuse the existing session
                    session = existingSession;
                    // Update the date if needed
                    if (request.Date.Date != session.Date.Date)
                    {
                        session.Date = request.Date;
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Create a new attendance session if none exists
                    session = new AttendanceSession
                    {
                        SubjectId = request.SubjectId,
                        TeacherId = teacherId,
                        Date = request.Date,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AttendanceSessions.Add(session);
                    await _context.SaveChangesAsync();
                }

                return Ok(new
                {
                    sessionId = session.SessionId,
                    subjectId = session.SubjectId,
                    subjectName = subject.Name,
                    date = session.Date,
                    message = "Attendance session retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpPost("record")]
        public async Task<IActionResult> RecordAttendance([FromBody] RecordAttendanceRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                // Determine which student ID to use
                int studentId;
                
                if (request.StudentId.HasValue)
                {
                    // If StudentId is provided in the request (legacy format support)
                    studentId = request.StudentId.Value;
                    
                    // Security check - only teachers can record attendance for other students
                    if (userTypeClaim.Value != "Teacher")
                    {
                        // If not a teacher, verify that the provided StudentId matches the authenticated user
                        int authenticatedStudentId = int.Parse(userIdClaim.Value);
                        if (studentId != authenticatedStudentId)
                        {
                            return Unauthorized(new { message = "Students can only record their own attendance" });
                        }
                    }
                }
                else
                {
                    // Use the authenticated user's ID (regular flow)
                    studentId = int.Parse(userIdClaim.Value);
                    
                    // Only students can record attendance this way
                    if (userTypeClaim.Value != "Student")
                        return BadRequest(new { message = "Only students can record attendance" });
                }

                // Verify the session exists
                var session = await _context.AttendanceSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.SubjectId == request.SubjectId);
                
                if (session == null)
                    return NotFound(new { message = "Attendance session not found" });

                // Verify the student is enrolled in the subject
                var isEnrolled = await _context.StudentTeacherConnections
                    .AnyAsync(stc => stc.StudentId == studentId && 
                                     stc.SubjectId == request.SubjectId && 
                                     stc.Status == "Approved");
                
                if (!isEnrolled)
                    return BadRequest(new { message = "Student is not enrolled in this subject" });

                // Check if attendance record already exists
                var existingRecord = await _context.AttendanceRecords
                    .FirstOrDefaultAsync(ar => ar.SessionId == request.SessionId && 
                                             ar.StudentId == studentId && 
                                             ar.SubjectId == request.SubjectId);
                
                if (existingRecord != null)
                    return Ok(new { message = "Attendance has already been recorded for this session" });

                // Create a new attendance record
                var record = new AttendanceRecord
                {
                    SessionId = request.SessionId,
                    StudentId = studentId,
                    SubjectId = request.SubjectId,
                    Timestamp = request.Timestamp
                };

                _context.AttendanceRecords.Add(record);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    attendanceId = record.AttendanceId,
                    sessionId = record.SessionId,
                    subjectId = record.SubjectId,
                    timestamp = record.Timestamp,
                    message = "Attendance recorded successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error: " + ex.Message });
            }
        }

        [HttpGet("session/{sessionId}")]
        public async Task<IActionResult> GetSessionAttendance(int sessionId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Teacher")
                    return BadRequest(new { message = "Only teachers can view session attendance" });

                var session = await _context.AttendanceSessions
                    .Include(s => s.Subject)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId);
                
                if (session == null)
                    return NotFound(new { message = "Attendance session not found" });

                // Verify the teacher owns the session
                if (session.TeacherId != userId)
                    return Forbid();

                var attendanceRecords = await _context.AttendanceRecords
                    .Where(ar => ar.SessionId == sessionId)
                    .Include(ar => ar.Student)
                    .Select(ar => new
                    {
                        attendanceId = ar.AttendanceId,
                        studentId = ar.StudentId,
                        studentName = ar.Student != null ? ar.Student.Fullname : "Unknown",
                        timestamp = ar.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    sessionId = session.SessionId,
                    subjectId = session.SubjectId,
                    subjectName = session.Subject?.Name,
                    date = session.Date,
                    attendanceCount = attendanceRecords.Count,
                    records = attendanceRecords
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("subject/{subjectId}")]
        public async Task<IActionResult> GetSubjectAttendance(int subjectId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                
                if (userTypeClaim.Value != "Teacher")
                    return BadRequest(new { message = "Only teachers can view subject attendance" });

                // Verify the subject belongs to the teacher
                var subject = await _context.Subjects
                    .FirstOrDefaultAsync(s => s.SubjectId == subjectId && s.TeacherId == userId);
                
                if (subject == null)
                    return NotFound(new { message = "Subject not found or you don't have access to it" });

                // Get the subject's session
                var session = await _context.AttendanceSessions
                    .Where(s => s.SubjectId == subjectId)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                if (session == null)
                    return Ok(new
                    {
                        subjectId = subjectId,
                        subjectName = subject.Name,
                        message = "No attendance session exists for this subject yet",
                        attendanceCount = 0,
                        records = new object[] { }
                    });

                // Get all attendance records for this session
                var attendanceRecords = await _context.AttendanceRecords
                    .Where(ar => ar.SessionId == session.SessionId)
                    .Include(ar => ar.Student)
                    .Select(ar => new
                    {
                        attendanceId = ar.AttendanceId,
                        studentId = ar.StudentId,
                        studentName = ar.Student != null ? ar.Student.Fullname : "Unknown",
                        timestamp = ar.Timestamp,
                        date = ar.Timestamp.ToString("yyyy-MM-dd"),
                        time = ar.Timestamp.ToString("HH:mm:ss")
                    })
                    .OrderByDescending(ar => ar.timestamp)
                    .ToListAsync();

                return Ok(new
                {
                    subjectId = subjectId,
                    subjectName = subject.Name,
                    sessionId = session.SessionId,
                    sessionDate = session.Date,
                    attendanceCount = attendanceRecords.Count,
                    records = attendanceRecords
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
} 