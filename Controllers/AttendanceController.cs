using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Hubs;
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
        private readonly IHubContext<AttendanceHub> _attendanceHubContext;

        public AttendanceController(ApplicationDbContext context, IHubContext<AttendanceHub> attendanceHubContext)
        {
            _context = context;
            _attendanceHubContext = attendanceHubContext;
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

                // Check if there's an existing active session for this subject and date
                var existingSession = await _context.AttendanceSessions
                    .Where(s => s.SubjectId == request.SubjectId && 
                           s.TeacherId == teacherId && 
                           s.Date.Date == request.Date.Date)
                    .OrderByDescending(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                AttendanceSession session;
                
                if (existingSession != null)
                {
                    // Reuse the existing session for this specific date
                    session = existingSession;
                    Console.WriteLine($"Reusing existing session ID {session.SessionId} for date {request.Date.Date}");
                }
                else
                {
                    // Create a new attendance session for this date
                    session = new AttendanceSession
                    {
                        SubjectId = request.SubjectId,
                        TeacherId = teacherId,
                        Date = request.Date,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.AttendanceSessions.Add(session);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Created new session ID {session.SessionId} for date {request.Date.Date}");
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
                    // If StudentId is provided in the request
                    studentId = request.StudentId.Value;
                    Console.WriteLine($"Using provided StudentId: {studentId}");
                    
                    // Security check - only teachers can record attendance for other students
                    if (userTypeClaim.Value != "Teacher")
                    {
                        // If not a teacher, verify that the provided StudentId matches the authenticated user
                        int authenticatedStudentId = int.Parse(userIdClaim.Value);
                        if (studentId != authenticatedStudentId)
                        {
                            Console.WriteLine($"Security check failed: Student {authenticatedStudentId} tried to record for {studentId}");
                            return Unauthorized(new { message = "Students can only record their own attendance" });
                        }
                    }
                }
                else
                {
                    // Use the authenticated user's ID
                    studentId = int.Parse(userIdClaim.Value);
                    Console.WriteLine($"Using authenticated StudentId: {studentId}");
                    
                    // Only students can record attendance this way
                    if (userTypeClaim.Value != "Student")
                    {
                        Console.WriteLine($"User type check failed: {userTypeClaim.Value} tried to record as student");
                        return BadRequest(new { message = "Only students can record attendance" });
                    }
                }

                // Validate student exists in database
                var studentExists = await _context.Students.AnyAsync(s => s.StudentId == studentId);
                if (!studentExists)
                {
                    Console.WriteLine($"Student with ID {studentId} does not exist in database");
                    return BadRequest(new { message = "Student not found in system. Please contact administrator." });
                }

                // Verify the session exists
                var session = await _context.AttendanceSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.SubjectId == request.SubjectId);
                
                if (session == null)
                {
                    Console.WriteLine($"Session not found: SessionId={request.SessionId}, SubjectId={request.SubjectId}");
                    return NotFound(new { message = "Attendance session not found" });
                }
                
                Console.WriteLine($"Found session: ID={session.SessionId}, Subject={session.SubjectId}, Date={session.Date}");

                // Verify the student is enrolled in the subject
                bool isEnrolled = false;
                
                try {
                    // Check if student is formally enrolled in the subject
                    isEnrolled = await _context.StudentTeacherConnections
                        .AnyAsync(stc => stc.StudentId == studentId && 
                                     stc.SubjectId == request.SubjectId && 
                                     stc.Status == "Approved");
                    
                    Console.WriteLine($"Enrollment check for student {studentId} in subject {request.SubjectId}: {isEnrolled}");
                }
                catch (Exception ex) {
                    // Log error but continue
                    Console.WriteLine($"Error checking enrollment: {ex.Message}");
                    return StatusCode(500, new { message = "Error verifying student enrollment: " + ex.Message });
                }
                
                // Don't bypass the enrollment check - only enrolled students can record attendance
                if (!isEnrolled) {
                    Console.WriteLine($"Rejecting attendance for student {studentId} - not enrolled in subject {request.SubjectId}");
                    return BadRequest(new { message = "Student is not enrolled in this subject. Please request enrollment from your teacher." });
                }
                
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

                Console.WriteLine($"Adding attendance record: SessionId={record.SessionId}, StudentId={record.StudentId}, SubjectId={record.SubjectId}, Time={record.Timestamp}");
                
                try
                {
                    _context.AttendanceRecords.Add(record);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Successfully recorded attendance with ID: {record.AttendanceId}");
                    
                    // Notify parents about the attendance
                    try 
                    {
                        // Get student information
                        var student = await _context.Students.FindAsync(studentId);
                        if (student != null)
                        {
                            // Get subject information
                            var subject = await _context.Subjects.FindAsync(request.SubjectId);
                            if (subject != null)
                            {
                                // Get connected parents
                                var connectedParents = await _context.StudentParentConnections
                                    .Where(c => c.StudentId == studentId && c.Status == "Approved")
                                    .Select(c => c.ParentId.ToString())
                                    .ToListAsync();

                                // Send attendance notification to all connected parents
                                foreach (var parentId in connectedParents)
                                {
                                    await _attendanceHubContext.Clients.Group(parentId).SendAsync(
                                        "ReceiveAttendanceNotification",
                                        studentId,
                                        student.Fullname,
                                        subject.Name,
                                        record.Timestamp,
                                        student.ProfilePic);
                                    
                                    Console.WriteLine($"Sent attendance notification to parent {parentId} for student {studentId}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending attendance notification: {ex.Message}");
                        // Don't throw, just log the error
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database error recording attendance: {ex.Message}");
                    throw; // Rethrow to be caught by outer exception handler
                }

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
        public async Task<IActionResult> GetSubjectAttendance(int subjectId, [FromQuery] string date = null)
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

                // Parse date filter if provided
                DateTime? filterDate = null;
                if (!string.IsNullOrEmpty(date))
                {
                    if (DateTime.TryParse(date, out DateTime parsedDate))
                    {
                        filterDate = parsedDate.Date;
                        Console.WriteLine($"Filtering attendance for date: {filterDate.Value.ToString("yyyy-MM-dd")}");
                    }
                }

                // Find sessions for this subject and filter by date if provided
                var sessionsQuery = _context.AttendanceSessions
                    .Where(s => s.SubjectId == subjectId);
                    
                // Apply date filter on the session date, not on attendance record timestamp
                if (filterDate.HasValue)
                {
                    sessionsQuery = sessionsQuery.Where(s => s.Date.Date == filterDate.Value.Date);
                    Console.WriteLine($"Filtering sessions to date {filterDate.Value.Date}");
                }
                
                var sessionIds = await sessionsQuery
                    .Select(s => s.SessionId)
                    .ToListAsync();

                if (sessionIds.Count == 0)
                    return Ok(new
                    {
                        subjectId = subjectId,
                        subjectName = subject.Name,
                        message = "No attendance session exists for this subject for the selected date",
                        attendanceCount = 0,
                        records = new object[] { }
                    });

                Console.WriteLine($"Found {sessionIds.Count} sessions for subject {subjectId} on date {filterDate?.ToString("yyyy-MM-dd") ?? "all dates"}");

                // Get attendance records for the filtered sessions
                var attendanceRecords = await _context.AttendanceRecords
                    .Where(a => a.SubjectId == subjectId && sessionIds.Contains(a.SessionId))
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => new
                    {
                        attendanceId = a.AttendanceId,
                        studentId = a.StudentId,
                        sessionId = a.SessionId,
                        timestamp = a.Timestamp,
                        date = a.Timestamp.ToString("yyyy-MM-dd"),
                        time = a.Timestamp.ToString("HH:mm:ss"),
                        status = "Present", // Hardcoded status since it doesn't exist in the model
                        studentName = _context.Students
                            .Where(s => s.StudentId == a.StudentId)
                            .Select(s => s.Fullname ?? s.Username)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                Console.WriteLine($"Found {attendanceRecords.Count} attendance records for date {filterDate?.ToString("yyyy-MM-dd") ?? "all dates"}");

                return Ok(new
                {
                    subjectId,
                    subjectName = subject.Name,
                    attendanceCount = attendanceRecords.Count,
                    date = filterDate?.ToString("yyyy-MM-dd"),
                    records = attendanceRecords
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetStudentAttendance(int studentId, [FromQuery] string fromDate = null, [FromQuery] string toDate = null)
        {
            try
            {
                // Log all request parameters for debugging
                Console.WriteLine($"GetStudentAttendance called with: studentId={studentId}, fromDate={fromDate}, toDate={toDate}");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                
                // Only parents and teachers can view student attendance
                if (userTypeClaim.Value != "Parent" && userTypeClaim.Value != "Teacher")
                    return BadRequest(new { message = "Only parents and teachers can view student attendance" });

                // If a parent is making the request, verify that the student is connected to them
                if (userTypeClaim.Value == "Parent")
                {
                    bool isConnected = await _context.StudentParentConnections
                        .AnyAsync(c => c.StudentId == studentId && 
                                  c.ParentId == userId && 
                                  c.Status == "Approved");
                    
                    if (!isConnected)
                        return Forbid();
                }
                // If a teacher is making the request, verify they teach this student
                else if (userTypeClaim.Value == "Teacher")
                {
                    bool teachesStudent = await _context.StudentTeacherConnections
                        .AnyAsync(c => c.StudentId == studentId &&
                                 c.Subject.TeacherId == userId &&
                                 c.Status == "Approved");
                    
                    if (!teachesStudent)
                        return Forbid();
                }

                // Parse from date if provided
                DateTime? filterFromDate = null;
                if (!string.IsNullOrEmpty(fromDate))
                {
                    try
                    {
                        // Try standard parsing first
                        if (DateTime.TryParse(fromDate, out DateTime parsedDate))
                        {
                            filterFromDate = parsedDate;
                            Console.WriteLine($"Successfully parsed fromDate: {filterFromDate.Value.ToString("yyyy-MM-dd")}");
                        }
                        // Try parsing as simple yyyy-MM-dd format if standard parsing fails
                        else if (DateTime.TryParseExact(fromDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, 
                                System.Globalization.DateTimeStyles.None, out parsedDate))
                        {
                            filterFromDate = parsedDate;
                            Console.WriteLine($"Successfully parsed fromDate using exact format: {filterFromDate.Value.ToString("yyyy-MM-dd")}");
                        }
                        else
                        {
                            Console.WriteLine($"Could not parse fromDate: {fromDate}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing fromDate ({fromDate}): {ex.Message}");
                        // Continue without the filter rather than failing the request
                    }
                }
                
                // Parse to date if provided
                DateTime? filterToDate = null;
                if (!string.IsNullOrEmpty(toDate))
                {
                    try
                    {
                        // Try standard parsing first
                        if (DateTime.TryParse(toDate, out DateTime parsedDate))
                        {
                            filterToDate = parsedDate;
                            Console.WriteLine($"Successfully parsed toDate: {filterToDate.Value.ToString("yyyy-MM-dd")}");
                        }
                        // Try parsing as simple yyyy-MM-dd format if standard parsing fails
                        else if (DateTime.TryParseExact(toDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, 
                                System.Globalization.DateTimeStyles.None, out parsedDate))
                        {
                            filterToDate = parsedDate;
                            Console.WriteLine($"Successfully parsed toDate using exact format: {filterToDate.Value.ToString("yyyy-MM-dd")}");
                        }
                        else
                        {
                            Console.WriteLine($"Could not parse toDate: {toDate}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing toDate ({toDate}): {ex.Message}");
                        // Continue without the filter rather than failing the request
                    }
                }

                // Build query for attendance records
                var query = _context.AttendanceRecords
                    .Where(a => a.StudentId == studentId);

                // Apply date filters if provided
                if (filterFromDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= filterFromDate.Value);
                }
                
                if (filterToDate.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= filterToDate.Value);
                }
                
                // If no date filter was provided, limit to last 7 days by default
                if (!filterFromDate.HasValue && !filterToDate.HasValue)
                {
                    var defaultFromDate = DateTime.Now.AddDays(-7);
                    query = query.Where(a => a.Timestamp >= defaultFromDate);
                    Console.WriteLine($"No date filters provided, limiting to last 7 days (from {defaultFromDate.ToString("yyyy-MM-dd")})");
                }

                // Get attendance records with subject information
                var attendanceRecords = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Select(a => new
                    {
                        a.AttendanceId,
                        a.SessionId,
                        a.StudentId,
                        a.SubjectId,
                        a.Timestamp,
                        subjectName = a.Subject.Name
                    })
                    .ToListAsync();

                // Get student info
                var student = await _context.Students
                    .Where(s => s.StudentId == studentId)
                    .Select(s => new
                    {
                        s.StudentId,
                        s.Username,
                        s.Fullname
                    })
                    .FirstOrDefaultAsync();

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                return Ok(new
                {
                    studentId,
                    studentName = student.Fullname ?? student.Username,
                    fromDate = filterFromDate?.ToString("yyyy-MM-dd"),
                    toDate = filterToDate?.ToString("yyyy-MM-dd"),
                    recordCount = attendanceRecords.Count,
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