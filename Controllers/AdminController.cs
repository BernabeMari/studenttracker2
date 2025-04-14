using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace StudentTracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                // Only allow Admin role
                if (!IsAdminUser())
                {
                    return Unauthorized("Admin access required");
                }

                // Count of entities
                var studentsCount = await _context.Students.CountAsync();
                var parentsCount = await _context.Parents.CountAsync();
                var teachersCount = await _context.Teachers.CountAsync();
                var subjectsCount = await _context.Subjects.CountAsync();
                var activeSessionsCount = await _context.TrackingSessions.Where(t => t.EndTime == null).CountAsync();
                var totalSessionsCount = await _context.TrackingSessions.CountAsync();
                var locationPointsCount = await _context.LocationTrackings.CountAsync();
                var studentParentConnectionsCount = await _context.StudentParentConnections.CountAsync();
                var studentTeacherConnectionsCount = await _context.StudentTeacherConnections.CountAsync();
                
                // Database size estimation (simplified - just count total records)
                var totalRecords = studentsCount + parentsCount + teachersCount + 
                                  subjectsCount + totalSessionsCount + locationPointsCount +
                                  studentParentConnectionsCount + studentTeacherConnectionsCount;

                // Calculate tracking storage
                var trackingStoragePercentage = totalRecords > 0 
                    ? (double)(totalSessionsCount + locationPointsCount) / totalRecords * 100 
                    : 0;

                return Ok(new
                {
                    counts = new
                    {
                        students = studentsCount,
                        parents = parentsCount,
                        teachers = teachersCount,
                        subjects = subjectsCount,
                        activeSessions = activeSessionsCount,
                        totalSessions = totalSessionsCount,
                        locationPoints = locationPointsCount,
                        studentParentConnections = studentParentConnectionsCount,
                        studentTeacherConnections = studentTeacherConnectionsCount
                    },
                    trackingStorage = new
                    {
                        percentage = Math.Round(trackingStoragePercentage, 2),
                        totalRecords = totalRecords,
                        trackingRecords = totalSessionsCount + locationPointsCount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching dashboard data: {ex.Message}");
            }
        }

        [HttpGet("students")]
        public async Task<IActionResult> GetStudents(int page = 1, int pageSize = 10)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                var totalStudents = await _context.Students.CountAsync();
                var students = await _context.Students
                    .OrderBy(s => s.StudentId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.StudentId,
                        s.Username,
                        s.Email,
                        s.Fullname,
                        s.CreatedAt,
                        HasProfilePic = !string.IsNullOrEmpty(s.ProfilePic) && s.ProfilePic != "default-profile"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total = totalStudents,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalStudents / pageSize),
                    data = students
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching students: {ex.Message}");
            }
        }

        [HttpGet("parents")]
        public async Task<IActionResult> GetParents(int page = 1, int pageSize = 10)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                var totalParents = await _context.Parents.CountAsync();
                var parents = await _context.Parents
                    .OrderBy(p => p.ParentId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.ParentId,
                        p.Username,
                        p.Email,
                        p.Fullname,
                        p.CreatedAt,
                        HasProfilePic = !string.IsNullOrEmpty(p.ProfilePic) && p.ProfilePic != "default-profile"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total = totalParents,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalParents / pageSize),
                    data = parents
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching parents: {ex.Message}");
            }
        }

        [HttpGet("teachers")]
        public async Task<IActionResult> GetTeachers(int page = 1, int pageSize = 10)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                var totalTeachers = await _context.Teachers.CountAsync();
                var teachers = await _context.Teachers
                    .OrderBy(t => t.TeacherId)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new
                    {
                        t.TeacherId,
                        t.Username,
                        t.Email,
                        t.Fullname,
                        t.CreatedAt,
                        HasProfilePic = !string.IsNullOrEmpty(t.ProfilePic) && t.ProfilePic != "default-profile"
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total = totalTeachers,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalTeachers / pageSize),
                    data = teachers
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching teachers: {ex.Message}");
            }
        }

        [HttpGet("tracking/sessions")]
        public async Task<IActionResult> GetTrackingSessions(int page = 1, int pageSize = 10)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                var totalSessions = await _context.TrackingSessions.CountAsync();
                var sessions = await _context.TrackingSessions
                    .Include(s => s.Student)
                    .OrderByDescending(s => s.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.SessionId,
                        s.StudentId,
                        StudentName = s.Student != null ? s.Student.Fullname : "Unknown",
                        s.StartTime,
                        s.EndTime,
                        s.IsActive,
                        Duration = s.EndTime.HasValue 
                            ? (s.EndTime.Value - s.StartTime).ToString(@"hh\:mm\:ss") 
                            : "Active",
                        LocationCount = _context.LocationTrackings.Count(l => l.SessionId == s.SessionId)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total = totalSessions,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalSessions / pageSize),
                    data = sessions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching tracking sessions: {ex.Message}");
            }
        }

        [HttpGet("subjects")]
        public async Task<IActionResult> GetSubjects(int page = 1, int pageSize = 10)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                var totalSubjects = await _context.Subjects.CountAsync();
                var subjects = await _context.Subjects
                    .Include(s => s.Teacher)
                    .OrderBy(s => s.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        s.SubjectId,
                        s.Name,
                        s.Description,
                        s.TeacherId,
                        TeacherName = s.Teacher != null ? s.Teacher.Fullname : "Unknown",
                        s.CreatedAt,
                        StudentsCount = _context.StudentTeacherConnections
                            .Count(stc => stc.SubjectId == s.SubjectId && stc.Status == "Approved")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    total = totalSubjects,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalSubjects / pageSize),
                    data = subjects
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching subjects: {ex.Message}");
            }
        }

        [HttpDelete("tracking/purge")]
        public async Task<IActionResult> PurgeTrackingData([FromQuery] DateTime? olderThan = null, [FromQuery] bool completeWipe = false)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                int locationPointsDeleted = 0;
                int sessionsDeleted = 0;

                if (completeWipe)
                {
                    // Delete all location tracking data
                    locationPointsDeleted = await _context.LocationTrackings.CountAsync();
                    _context.LocationTrackings.RemoveRange(_context.LocationTrackings);
                    
                    // Delete all tracking sessions
                    sessionsDeleted = await _context.TrackingSessions.CountAsync();
                    _context.TrackingSessions.RemoveRange(_context.TrackingSessions);
                }
                else if (olderThan.HasValue)
                {
                    // Find old sessions
                    var oldSessions = await _context.TrackingSessions
                        .Where(s => s.StartTime < olderThan.Value && s.EndTime != null)
                        .ToListAsync();
                    
                    if (oldSessions.Any())
                    {
                        var sessionIds = oldSessions.Select(s => s.SessionId).ToList();
                        
                        // Delete location points for these sessions
                        var locationPoints = await _context.LocationTrackings
                            .Where(l => sessionIds.Contains(l.SessionId))
                            .ToListAsync();
                        
                        locationPointsDeleted = locationPoints.Count;
                        _context.LocationTrackings.RemoveRange(locationPoints);
                        
                        // Delete the sessions
                        sessionsDeleted = oldSessions.Count;
                        _context.TrackingSessions.RemoveRange(oldSessions);
                    }
                }
                else
                {
                    // Delete only completed tracking sessions (with EndTime set)
                    var completedSessions = await _context.TrackingSessions
                        .Where(s => s.EndTime != null)
                        .ToListAsync();
                    
                    if (completedSessions.Any())
                    {
                        var sessionIds = completedSessions.Select(s => s.SessionId).ToList();
                        
                        // Delete location points for these sessions
                        var locationPoints = await _context.LocationTrackings
                            .Where(l => sessionIds.Contains(l.SessionId))
                            .ToListAsync();
                        
                        locationPointsDeleted = locationPoints.Count;
                        _context.LocationTrackings.RemoveRange(locationPoints);
                        
                        // Delete the sessions
                        sessionsDeleted = completedSessions.Count;
                        _context.TrackingSessions.RemoveRange(completedSessions);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Successfully purged tracking data. {sessionsDeleted} sessions and {locationPointsDeleted} location points were deleted.",
                    deletedSessions = sessionsDeleted,
                    deletedLocationPoints = locationPointsDeleted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error purging tracking data: {ex.Message}");
            }
        }

        [HttpDelete("users/{type}/{id}")]
        public async Task<IActionResult> DeleteUser(string type, int id)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                switch (type.ToLower())
                {
                    case "student":
                        var student = await _context.Students.FindAsync(id);
                        if (student != null)
                        {
                            // Delete related data first
                            var studentSessions = await _context.TrackingSessions
                                .Where(s => s.StudentId == id)
                                .ToListAsync();
                                
                            foreach (var session in studentSessions)
                            {
                                var locationPoints = await _context.LocationTrackings
                                    .Where(l => l.SessionId == session.SessionId)
                                    .ToListAsync();
                                    
                                _context.LocationTrackings.RemoveRange(locationPoints);
                            }
                            
                            _context.TrackingSessions.RemoveRange(studentSessions);
                            
                            // Delete parent connections
                            var parentConnections = await _context.StudentParentConnections
                                .Where(c => c.StudentId == id)
                                .ToListAsync();
                                
                            _context.StudentParentConnections.RemoveRange(parentConnections);
                            
                            // Delete teacher connections
                            var teacherConnections = await _context.StudentTeacherConnections
                                .Where(c => c.StudentId == id)
                                .ToListAsync();
                                
                            _context.StudentTeacherConnections.RemoveRange(teacherConnections);
                            
                            // Delete student
                            _context.Students.Remove(student);
                            await _context.SaveChangesAsync();
                            
                            return Ok(new { success = true, message = $"Student with ID {id} was deleted successfully." });
                        }
                        return NotFound($"Student with ID {id} not found.");
                        
                    case "parent":
                        var parent = await _context.Parents.FindAsync(id);
                        if (parent != null)
                        {
                            // Delete parent connections
                            var connections = await _context.StudentParentConnections
                                .Where(c => c.ParentId == id)
                                .ToListAsync();
                                
                            _context.StudentParentConnections.RemoveRange(connections);
                            
                            // Delete parent
                            _context.Parents.Remove(parent);
                            await _context.SaveChangesAsync();
                            
                            return Ok(new { success = true, message = $"Parent with ID {id} was deleted successfully." });
                        }
                        return NotFound($"Parent with ID {id} not found.");
                        
                    case "teacher":
                        var teacher = await _context.Teachers.FindAsync(id);
                        if (teacher != null)
                        {
                            // Delete subjects
                            var subjects = await _context.Subjects
                                .Where(s => s.TeacherId == id)
                                .ToListAsync();
                                
                            foreach (var subject in subjects)
                            {
                                // Delete student-teacher connections for this subject
                                var connections = await _context.StudentTeacherConnections
                                    .Where(c => c.SubjectId == subject.SubjectId)
                                    .ToListAsync();
                                    
                                _context.StudentTeacherConnections.RemoveRange(connections);
                            }
                            
                            _context.Subjects.RemoveRange(subjects);
                            
                            // Delete teacher connections
                            var teacherConnections = await _context.StudentTeacherConnections
                                .Where(c => c.TeacherId == id)
                                .ToListAsync();
                                
                            _context.StudentTeacherConnections.RemoveRange(teacherConnections);
                            
                            // Delete teacher
                            _context.Teachers.Remove(teacher);
                            await _context.SaveChangesAsync();
                            
                            return Ok(new { success = true, message = $"Teacher with ID {id} was deleted successfully." });
                        }
                        return NotFound($"Teacher with ID {id} not found.");
                        
                    default:
                        return BadRequest("Invalid user type. Valid types are 'student', 'parent', and 'teacher'.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting user: {ex.Message}");
            }
        }

        [HttpDelete("subjects/{id}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            if (!IsAdminUser())
            {
                return Unauthorized("Admin access required");
            }

            try
            {
                var subject = await _context.Subjects.FindAsync(id);
                if (subject != null)
                {
                    // Delete student-teacher connections for this subject
                    var connections = await _context.StudentTeacherConnections
                        .Where(c => c.SubjectId == id)
                        .ToListAsync();
                        
                    _context.StudentTeacherConnections.RemoveRange(connections);
                    
                    // Delete subject
                    _context.Subjects.Remove(subject);
                    await _context.SaveChangesAsync();
                    
                    return Ok(new { success = true, message = $"Subject with ID {id} was deleted successfully." });
                }
                return NotFound($"Subject with ID {id} not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting subject: {ex.Message}");
            }
        }

        private bool IsAdminUser()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            return userRole == "Admin";
        }
    }
} 