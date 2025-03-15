using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;
<<<<<<< HEAD
=======
using System.Collections.Generic;
>>>>>>> d4a1f77 (90% done)

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

<<<<<<< HEAD
            // Check for existing connection
            bool connectionExists = await _context.StudentTeacherConnections.AnyAsync(stc => 
                stc.StudentId == request.StudentId && 
                stc.TeacherId == teacherId && 
                stc.SubjectId == request.SubjectId);

            if (connectionExists)
                return BadRequest(new { message = "Connection request already exists" });
=======
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
>>>>>>> d4a1f77 (90% done)

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
<<<<<<< HEAD
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
=======
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
>>>>>>> d4a1f77 (90% done)
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingConnections()
        {
<<<<<<< HEAD
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
=======
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
>>>>>>> d4a1f77 (90% done)
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
<<<<<<< HEAD
=======
                            stc.Teacher.Username,
>>>>>>> d4a1f77 (90% done)
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
<<<<<<< HEAD
=======
                                stc.Student.Username,
>>>>>>> d4a1f77 (90% done)
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
<<<<<<< HEAD
=======
        
        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedConnections()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;

            if (userType == "Student")
            {
                var rejectedConnections = await _context.StudentTeacherConnections
                    .Where(stc => stc.StudentId == userId && stc.Status == "Rejected")
                    .Include(stc => stc.Teacher)
                    .Include(stc => stc.Subject)
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

                return Ok(rejectedConnections);
            }
            else if (userType == "Teacher")
            {
                var rejectedRequests = await _context.StudentTeacherConnections
                    .Where(stc => stc.TeacherId == userId && stc.Status == "Rejected")
                    .Include(stc => stc.Student)
                    .Include(stc => stc.Subject)
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
                        Student = new
                        {
                            stc.Student.StudentId,
                            stc.Student.Username,
                            stc.Student.Fullname,
                            stc.Student.Email
                        },
                        RejectionCount = _context.StudentTeacherConnections
                            .Count(c => c.StudentId == stc.StudentId && 
                                       c.TeacherId == stc.TeacherId && 
                                       c.SubjectId == stc.SubjectId && 
                                       c.Status == "Rejected")
                    })
                    .ToListAsync();

                return Ok(rejectedRequests);
            }

            return BadRequest(new { message = "Invalid user type" });
        }
>>>>>>> d4a1f77 (90% done)

        [HttpDelete("{connectionId}")]
        public async Task<IActionResult> RemoveConnection(int connectionId)
        {
<<<<<<< HEAD
=======
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var userId = int.Parse(userIdClaim.Value);
            var userType = userTypeClaim.Value;
            
            var connection = await _context.StudentTeacherConnections.FindAsync(connectionId);
            if (connection == null)
                return NotFound(new { message = "Connection not found" });
                
            if (userType == "Teacher" && connection.TeacherId != userId)
                return Forbid();
                
            if (userType == "Student" && connection.StudentId != userId)
                return Forbid();
                
            _context.StudentTeacherConnections.Remove(connection);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Connection removed successfully" });
        }

        [HttpGet("test")]
        public IActionResult TestEndpoint()
        {
            return Ok(new { message = "TeacherConnection controller is working" });
        }

        [HttpGet("debug")]
        public async Task<IActionResult> DebugConnections()
        {
>>>>>>> d4a1f77 (90% done)
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null)
                    return Unauthorized(new { message = "User not authenticated" });

<<<<<<< HEAD
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
=======
                var userId = int.Parse(userIdClaim.Value);
                var userType = userTypeClaim.Value;

                // Count total connections in database
                var totalConnections = await _context.StudentTeacherConnections.CountAsync();
                
                // Count connections for this user
                var userConnections = 0;
                
                if (userType == "Student")
                {
                    userConnections = await _context.StudentTeacherConnections
                        .CountAsync(stc => stc.StudentId == userId);
                }
                else if (userType == "Teacher")
                {
                    userConnections = await _context.StudentTeacherConnections
                        .CountAsync(stc => stc.TeacherId == userId);
                }

                return Ok(new 
                { 
                    userId = userId,
                    userType = userType,
                    totalConnectionsInDb = totalConnections,
                    userConnectionsCount = userConnections,
                    message = "Debug connection information"
>>>>>>> d4a1f77 (90% done)
                });
            }
            catch (Exception ex)
            {
<<<<<<< HEAD
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
=======
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

                var userId = int.Parse(userIdClaim.Value);
                var userType = userTypeClaim.Value;
                
                if (userType != "Student")
                    return BadRequest(new { message = "Only students can use this debug endpoint" });
                
                // Find a teacher
                var teacher = await _context.Teachers.FirstOrDefaultAsync();
                if (teacher == null)
                    return NotFound(new { message = "No teachers found in the system" });
                
                // Find a subject
                var subject = await _context.Subjects.FirstOrDefaultAsync(s => s.TeacherId == teacher.TeacherId);
                if (subject == null)
                    return NotFound(new { message = "No subjects found for the teacher" });
                
                // Create a test connection if one doesn't exist
                var existingConnection = await _context.StudentTeacherConnections
                    .FirstOrDefaultAsync(stc => 
                        stc.StudentId == userId && 
                        stc.TeacherId == teacher.TeacherId && 
                        stc.SubjectId == subject.SubjectId);
                
                if (existingConnection != null)
                {
                    // If connection exists but is not pending, reset it to pending for testing
                    if (existingConnection.Status != "Pending")
                    {
                        existingConnection.Status = "Pending";
                        existingConnection.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        return Ok(new 
                        { 
                            message = "Existing connection reset to Pending status",
                            connectionId = existingConnection.ConnectionId,
                            teacherName = teacher.Fullname,
                            subjectName = subject.Name
                        });
                    }
                    
                    return Ok(new 
                    { 
                        message = "Test connection already exists",
                        connectionId = existingConnection.ConnectionId,
                        teacherName = teacher.Fullname,
                        subjectName = subject.Name
                    });
                }
                
                // Create a test connection
                var connection = new StudentTeacherConnection
                {
                    StudentId = userId,
                    TeacherId = teacher.TeacherId,
                    SubjectId = subject.SubjectId,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.StudentTeacherConnections.Add(connection);
                await _context.SaveChangesAsync();
                
                return Ok(new 
                { 
                    message = "Test connection created successfully",
                    connectionId = connection.ConnectionId,
                    teacherName = teacher.Fullname,
                    subjectName = subject.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating test connection: {ex.Message}" });
            }
        }

        [HttpGet("directdebug")]
        public async Task<IActionResult> DirectDebug()
        {
            try
            {
                // Include the table structure and counts of each user type
                var studentCount = await _context.Students.CountAsync();
                var teacherCount = await _context.Teachers.CountAsync();
                var subjectCount = await _context.Subjects.CountAsync();
                var connectionCount = await _context.StudentTeacherConnections.CountAsync();
                
                // Get the first few entries in the StudentTeacherConnections table
                var recentConnections = await _context.StudentTeacherConnections
                    .Include(stc => stc.Student)
                    .Include(stc => stc.Teacher)
                    .Include(stc => stc.Subject)
                    .OrderByDescending(stc => stc.CreatedAt)
                    .Take(5)
                    .Select(stc => new
                    {
                        stc.ConnectionId,
                        stc.StudentId,
                        StudentName = stc.Student != null ? stc.Student.Fullname : "Unknown",
                        stc.TeacherId,
                        TeacherName = stc.Teacher != null ? stc.Teacher.Fullname : "Unknown",
                        stc.SubjectId,
                        SubjectName = stc.Subject != null ? stc.Subject.Name : "Unknown",
                        stc.Status,
                        stc.CreatedAt,
                        stc.UpdatedAt
                    })
                    .ToListAsync();
                
                // Debug information for database schema
                var tableData = new Dictionary<string, object>
                {
                    { "StudentsCount", studentCount },
                    { "TeachersCount", teacherCount },
                    { "SubjectsCount", subjectCount },
                    { "StudentTeacherConnectionsCount", connectionCount },
                    { "RecentConnections", recentConnections }
                };
                
                // Add user context
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = User.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim != null && userTypeClaim != null)
                {
                    var userId = int.Parse(userIdClaim.Value);
                    var userType = userTypeClaim.Value;
                    
                    tableData.Add("CurrentUserId", userId);
                    tableData.Add("CurrentUserType", userType);
                    
                    // Add user-specific connection information
                    if (userType == "Student")
                    {
                        var studentConnections = await _context.StudentTeacherConnections
                            .Where(stc => stc.StudentId == userId)
                            .OrderByDescending(stc => stc.CreatedAt)
                            .Select(stc => new
                            {
                                stc.ConnectionId,
                                stc.TeacherId,
                                stc.SubjectId,
                                stc.Status,
                                stc.CreatedAt,
                                stc.UpdatedAt
                            })
                            .ToListAsync();
                        
                        tableData.Add("UserConnections", studentConnections);
                    }
                    else if (userType == "Teacher")
                    {
                        var teacherConnections = await _context.StudentTeacherConnections
                            .Where(stc => stc.TeacherId == userId)
                            .OrderByDescending(stc => stc.CreatedAt)
                            .Select(stc => new
                            {
                                stc.ConnectionId,
                                stc.StudentId,
                                stc.SubjectId,
                                stc.Status,
                                stc.CreatedAt,
                                stc.UpdatedAt
                            })
                            .ToListAsync();
                        
                        tableData.Add("UserConnections", teacherConnections);
                    }
                }
                
                return Ok(tableData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Direct debug error: {ex.Message}" });
>>>>>>> d4a1f77 (90% done)
            }
        }
    }
} 