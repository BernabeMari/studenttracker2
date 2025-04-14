using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;
using System.Collections.Generic;

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

            // Check for existing connection with any status
            var existingConnection = await _context.StudentTeacherConnections
                .FirstOrDefaultAsync(stc => 
                    stc.StudentId == request.StudentId && 
                    stc.TeacherId == teacherId && 
                    stc.SubjectId == request.SubjectId &&
                    (stc.Status == "Pending" || stc.Status == "Approved"));

            if (existingConnection != null)
            {
                if (existingConnection.Status == "Pending")
                    return BadRequest(new { message = "A pending invitation already exists for this student" });
                else if (existingConnection.Status == "Approved")
                    return BadRequest(new { message = "This student is already enrolled in this subject" });
            }

            // Count previous rejections
            var rejectionCount = await _context.StudentTeacherConnections
                .CountAsync(stc => 
                    stc.StudentId == request.StudentId && 
                    stc.TeacherId == teacherId && 
                    stc.SubjectId == request.SubjectId && 
                    stc.Status == "Rejected");

            // If already rejected 3 times, don't allow more requests
            if (rejectionCount >= 3)
                return BadRequest(new { message = "This student has rejected your invitation 3 times. You cannot send another invitation." });

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
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                
                Console.WriteLine($"Responding to connection request: userId={userId}, role={userTypeClaim.Value}, connectionId={model.ConnectionId}, status={model.Status}");
                
                if (userTypeClaim.Value != "Student")
                    return BadRequest(new { message = "Only students can respond to connection requests" });

                var connection = await _context.StudentTeacherConnections
                    .Include(stc => stc.Subject)
                    .FirstOrDefaultAsync(stc => stc.ConnectionId == model.ConnectionId);

                if (connection == null)
                {
                    Console.WriteLine($"Connection not found: {model.ConnectionId}");
                    return NotFound(new { message = "Connection request not found" });
                }

                if (connection.StudentId != userId)
                {
                    Console.WriteLine($"Forbidden: Connection student {connection.StudentId} doesn't match user {userId}");
                    return Forbid();
                }

                if (connection.Status != "Pending")
                {
                    Console.WriteLine($"Bad request: Connection already processed, status is {connection.Status}");
                    return BadRequest(new { message = "This request has already been processed" });
                }

                if (model.Status != "Approved" && model.Status != "Rejected")
                {
                    Console.WriteLine($"Bad request: Invalid status {model.Status}");
                    return BadRequest(new { message = "Invalid status. Status must be 'Approved' or 'Rejected'" });
                }

                connection.Status = model.Status;
                connection.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                Console.WriteLine($"Connection {model.ConnectionId} updated to {model.Status}");

                // For rejection, check if we've reached the limit
                if (model.Status == "Rejected")
                {
                    // Count rejections after this one
                    var rejectionCount = await _context.StudentTeacherConnections
                        .CountAsync(c => c.StudentId == connection.StudentId && 
                                        c.TeacherId == connection.TeacherId && 
                                        c.SubjectId == connection.SubjectId && 
                                        c.Status == "Rejected");

                    // Include information about the rejection limit in the response
                    string additionalMessage = "";
                    if (rejectionCount >= 3) {
                        additionalMessage = " Maximum of 3 rejections reached. The teacher cannot send more invitations.";
                    }

                    return Ok(new 
                    { 
                        message = $"Connection request {model.Status.ToLower()} successfully" + additionalMessage,
                        subjectName = connection.Subject?.Name,
                        rejectionCount = rejectionCount,
                        maxRejectionsReached = rejectionCount >= 3
                    });
                }

                return Ok(new 
                { 
                    message = $"Connection request {model.Status.ToLower()} successfully",
                    subjectName = connection.Subject?.Name
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TeacherConnectionController.RespondToRequest: {ex.Message}");
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingConnections()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                var userType = userTypeClaim.Value;

                Console.WriteLine($"Getting pending connections for user {userId} with role {userType}");

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
                                stc.Teacher.Username,
                                stc.Teacher.Fullname,
                                stc.Teacher.Email
                            }
                        })
                        .ToListAsync();

                    Console.WriteLine($"Found {connections.Count} pending connections for student {userId}");
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
                                stc.Student.Username,
                                stc.Student.Fullname,
                                stc.Student.Email
                            }
                        })
                        .ToListAsync();

                    Console.WriteLine($"Found {connections.Count} pending connections for teacher {userId}");
                    return Ok(connections);
                }

                return BadRequest(new { message = "Invalid user type" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TeacherConnectionController.GetPendingConnections: {ex.Message}");
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("connected")]
        public async Task<IActionResult> GetConnectedUsers()
        {
            try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;

                Console.WriteLine($"Getting connected users for user {userId} with role {userType}");

            if (userType == "Student")
            {
                var connections = await _context.StudentTeacherConnections
                    .Where(stc => stc.StudentId == userId && stc.Status == "Approved")
                    .Include(stc => stc.Teacher)
                    .Include(stc => stc.Subject)
                        .OrderBy(stc => stc.Subject.Name)
                    .Select(stc => new
                    {
                        stc.ConnectionId,
                        stc.Status,
                        stc.CreatedAt,
                        stc.UpdatedAt,
                        Subject = new
                        {
                            stc.Subject.SubjectId,
                            stc.Subject.Name,
                            stc.Subject.Description
                        },
                        Teacher = new
                        {
                            stc.Teacher.TeacherId,
                            stc.Teacher.Username,
                            stc.Teacher.Fullname,
                            stc.Teacher.Email
                        }
                    })
                    .ToListAsync();

                    Console.WriteLine($"Found {connections.Count} approved connections for student {userId}");
                    return Ok(connections);
                }
                else if (userType == "Teacher")
                {
                    var connections = await _context.StudentTeacherConnections
                        .Where(stc => stc.TeacherId == userId && stc.Status == "Approved")
                        .Include(stc => stc.Student)
                        .Include(stc => stc.Subject)
                        .OrderBy(stc => stc.Subject.Name)
                        .ThenBy(stc => stc.Student.Fullname)
                        .Select(stc => new
                        {
                            stc.ConnectionId,
                            stc.Status,
                            stc.CreatedAt,
                            stc.UpdatedAt,
                            Subject = new
                            {
                                stc.Subject.SubjectId,
                                stc.Subject.Name
                            },
                            Student = new
                            {
                                stc.Student.StudentId,
                                stc.Student.Username,
                                stc.Student.Fullname,
                                stc.Student.Email
                            }
                        })
                        .ToListAsync();

                    Console.WriteLine($"Found {connections.Count} approved connections for teacher {userId}");
                    return Ok(connections);
                }

                return BadRequest(new { message = "Invalid user type" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TeacherConnectionController.GetConnectedUsers: {ex.Message}");
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
        
        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedConnections()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                var userType = userTypeClaim.Value;

                Console.WriteLine($"Getting rejected connections for user {userId} with role {userType}");

                if (userType == "Teacher")
                {
                    var connections = await _context.StudentTeacherConnections
                    .Where(stc => stc.TeacherId == userId && stc.Status == "Rejected")
                    .Include(stc => stc.Student)
                    .Include(stc => stc.Subject)
                        .OrderByDescending(stc => stc.UpdatedAt)
                    .Select(stc => new
                    {
                        stc.ConnectionId,
                        stc.Status,
                        stc.CreatedAt,
                        stc.UpdatedAt,
                        Subject = new
                        {
                            stc.Subject.SubjectId,
                                stc.Subject.Name
                        },
                        Student = new
                        {
                            stc.Student.StudentId,
                            stc.Student.Username,
                            stc.Student.Fullname,
                            stc.Student.Email
                        },
                            // Count of rejections for this student-subject pair
                        RejectionCount = _context.StudentTeacherConnections
                            .Count(c => c.StudentId == stc.StudentId && 
                                       c.TeacherId == stc.TeacherId && 
                                       c.SubjectId == stc.SubjectId && 
                                       c.Status == "Rejected")
                    })
                    .ToListAsync();

                    Console.WriteLine($"Found {connections.Count} rejected connections for teacher {userId}");
                    return Ok(connections);
            }

            return BadRequest(new { message = "Invalid user type" });
        }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TeacherConnectionController.GetRejectedConnections: {ex.Message}");
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpDelete("{connectionId}")]
        public async Task<IActionResult> RemoveConnection(int connectionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;
            
            try
            {
                // Find the connection
                var connection = await _context.StudentTeacherConnections
                    .Include(stc => stc.Subject)
                    .Include(stc => stc.Student)
                    .FirstOrDefaultAsync(stc => stc.ConnectionId == connectionId);
                
                if (connection == null)
                    return NotFound(new { message = "Connection not found" });
                
                // Check authorization
                bool isAuthorized = false;
                string responseMessage = "";
                
                if (userType == "Teacher" && connection.TeacherId == userId)
                {
                    // Teachers can remove any of their connections
                    isAuthorized = true;
                    responseMessage = $"Student {connection.Student.Fullname} removed from subject {connection.Subject.Name} successfully";
                }
                else if (userType == "Student" && connection.StudentId == userId && connection.Status == "Approved")
                {
                    // Students can only remove themselves from approved connections (unenroll)
                    isAuthorized = true;
                    responseMessage = $"Successfully unenrolled from {connection.Subject.Name}";
                }
                
                if (!isAuthorized)
                    return Forbid();
                
                // Remove the connection
                _context.StudentTeacherConnections.Remove(connection);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = responseMessage,
                    studentId = connection.StudentId,
                    subjectId = connection.SubjectId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("test")]
        public IActionResult TestEndpoint()
        {
            return Ok(new { message = "Connection API is working" });
        }

        [HttpGet("debug")]
        public async Task<IActionResult> DebugConnections()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                var userId = int.Parse(userIdClaim.Value);
                var userType = userTypeClaim.Value;
                
                var allConnections = await _context.StudentTeacherConnections.ToListAsync();
                var userConnections = await _context.StudentTeacherConnections
                    .Where(c => 
                        (userType == "Student" && c.StudentId == userId) || 
                        (userType == "Teacher" && c.TeacherId == userId))
                    .ToListAsync();
                
                return Ok(new {
                    userId,
                    userType,
                    totalConnectionsCount = allConnections.Count,
                    userConnectionsCount = userConnections.Count,
                    userConnections = userConnections,
                    message = "Debug connection information"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Debug error: {ex.Message}" });
            }
        }

        [HttpGet("createtest")]
        public async Task<IActionResult> CreateTestConnection()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

                int userId = int.Parse(userIdClaim.Value);
                string userType = userTypeClaim.Value;
                
                // Only for development environment
                if (!Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? true)
                {
                    return BadRequest(new { message = "This endpoint is only available in the development environment" });
                }
                
                if (userType != "Teacher")
                {
                    return BadRequest(new { message = "You must be a teacher to create test connections" });
                }
                
                // Get first student in the system
                var student = await _context.Students.FirstOrDefaultAsync();
                if (student == null)
                {
                    return NotFound(new { message = "No students found in the system" });
                }
                
                // Get first subject owned by this teacher
                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.TeacherId == userId);
                if (subject == null)
                {
                    return NotFound(new { message = "No subjects found for this teacher" });
                }
                
                // Check for existing connection
                var existingConnection = await _context.StudentTeacherConnections
                    .FirstOrDefaultAsync(stc => 
                        stc.StudentId == student.StudentId && 
                        stc.TeacherId == userId && 
                        stc.SubjectId == subject.SubjectId);
                
                if (existingConnection != null)
                {
                    return Ok(new { 
                        message = $"Connection already exists with status: {existingConnection.Status}",
                        existingConnection
                    });
                }
                
                // Create new connection
                var connection = new StudentTeacherConnection
                {
                    StudentId = student.StudentId,
                    TeacherId = userId,
                    SubjectId = subject.SubjectId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.StudentTeacherConnections.Add(connection);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    message = "Test connection created successfully",
                    studentId = student.StudentId,
                    studentName = student.Fullname,
                    subjectId = subject.SubjectId,
                    subjectName = subject.Name,
                    connectionId = connection.ConnectionId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }

        [HttpGet("directdebug")]
        public async Task<IActionResult> DirectDebug()
        {
            try
            {
                // Direct debug - bypasses authentication
                
                // Get counts
                int studentCount = await _context.Students.CountAsync();
                int teacherCount = await _context.Teachers.CountAsync();
                int subjectCount = await _context.Subjects.CountAsync();
                int connectionCount = await _context.StudentTeacherConnections.CountAsync();
                
                // Get connection status counts
                int pendingCount = await _context.StudentTeacherConnections.CountAsync(c => c.Status == "Pending");
                int approvedCount = await _context.StudentTeacherConnections.CountAsync(c => c.Status == "Approved");
                int rejectedCount = await _context.StudentTeacherConnections.CountAsync(c => c.Status == "Rejected");
                
                // Sample connections
                var sampleConnections = await _context.StudentTeacherConnections
                    .Include(c => c.Student)
                    .Include(c => c.Teacher)
                    .Include(c => c.Subject)
                    .Take(5)
                    .Select(c => new {
                        c.ConnectionId,
                        c.Status,
                        StudentName = c.Student.Fullname,
                        TeacherName = c.Teacher.Fullname,
                        SubjectName = c.Subject.Name,
                        c.CreatedAt,
                        c.UpdatedAt
                    })
                    .ToListAsync();
                
                return Ok(new {
                    studentCount,
                    teacherCount, 
                    subjectCount,
                    connectionCount,
                    statusCounts = new {
                        pending = pendingCount,
                        approved = approvedCount,
                        rejected = rejectedCount
                    },
                    sampleConnections,
                    message = "Direct debug information"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Direct debug error: {ex.Message}" });
            }
        }
    }
} 