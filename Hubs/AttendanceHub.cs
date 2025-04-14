using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;

namespace StudentTracker.Hubs
{
    [Authorize]
    public class AttendanceHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceHub> _logger;

        public AttendanceHub(ApplicationDbContext context, ILogger<AttendanceHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    _logger.LogInformation($"User {userIdClaim.Value} connected to AttendanceHub");
                    await Groups.AddToGroupAsync(Context.ConnectionId, userIdClaim.Value);
                }
                else
                {
                    _logger.LogWarning("User connected to AttendanceHub without a valid user ID claim");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AttendanceHub.OnConnectedAsync");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null)
                {
                    _logger.LogInformation($"User {userIdClaim.Value} disconnected from AttendanceHub");
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, userIdClaim.Value);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AttendanceHub.OnDisconnectedAsync");
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        // Method to notify parents about student attendance
        public async Task NotifyParentsAboutAttendance(int studentId, int subjectId, DateTime timestamp)
        {
            try
            {
                // Get student information
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return;

                // Get subject information
                var subject = await _context.Subjects.FindAsync(subjectId);
                if (subject == null)
                    return;

                // Get connected parents
                var connectedParents = await _context.StudentParentConnections
                    .Where(c => c.StudentId == studentId && c.Status == "Approved")
                    .Select(c => c.ParentId.ToString())
                    .ToListAsync();

                // Send attendance notification to all connected parents
                foreach (var parentId in connectedParents)
                {
                    await Clients.Group(parentId).SendAsync("ReceiveAttendanceNotification", 
                        studentId, 
                        student.Fullname,
                        subject.Name,
                        timestamp,
                        student.ProfilePic);
                        
                    _logger.LogInformation($"Attendance notification sent to parent {parentId} for student {studentId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error notifying parents about attendance for student {studentId}");
            }
        }
    }
} 