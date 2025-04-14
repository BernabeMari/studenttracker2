using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StudentTracker.Data;
using StudentTracker.Models;
using System.Security.Claims;

namespace StudentTracker.Hubs
{
    [Authorize]
    public class TrackingHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public TrackingHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, userIdClaim.Value);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userIdClaim.Value);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task UpdateLocation(double latitude, double longitude)
        {
            try
            {
                // Get current user ID from claims
                var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
                var userTypeClaim = Context.User?.FindFirst(ClaimTypes.Role);
                
                if (userIdClaim == null || userTypeClaim == null || userTypeClaim.Value != "Student")
                    return;

                int studentId = int.Parse(userIdClaim.Value);
                
                // Get student information
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return;

                // Get connected parents
                var connectedParents = await _context.StudentParentConnections
                    .Where(c => c.StudentId == studentId && c.Status == "Approved")
                    .Select(c => c.ParentId.ToString())
                    .ToListAsync();

                // Send location to all connected parents
                foreach (var parentId in connectedParents)
                {
                    await Clients.Group(parentId).SendAsync("ReceiveLocation", 
                        studentId, 
                        student.Username, 
                        latitude, 
                        longitude,
                        student.Fullname,
                        student.ProfilePic);
                }
                
                // Create or update tracking session
                var activeSession = await _context.TrackingSessions
                    .FirstOrDefaultAsync(s => s.StudentId == studentId && s.EndTime == null);
                
                if (activeSession == null)
                {
                    // Create new session if none exists
                    activeSession = new TrackingSession
                    {
                        StudentId = studentId,
                        StartTime = DateTime.UtcNow,
                        IsActive = true
                    };
                    _context.TrackingSessions.Add(activeSession);
                    await _context.SaveChangesAsync();
                }
                
                // Add location point
                var location = new LocationTracking
                {
                    SessionId = activeSession.SessionId,
                    Latitude = Convert.ToDecimal(latitude),
                    Longitude = Convert.ToDecimal(longitude),
                    Timestamp = DateTime.UtcNow
                };
                
                _context.LocationTrackings.Add(location);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw to client
                Console.WriteLine($"Error in UpdateLocation: {ex.Message}");
            }
        }

        public async Task StopTracking()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
                return;

            int studentId = int.Parse(userIdClaim.Value);

            // Find active session
            var activeSession = await _context.TrackingSessions
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.EndTime == null);
            
            if (activeSession != null)
            {
                activeSession.EndTime = DateTime.UtcNow;
                activeSession.IsActive = false;
                await _context.SaveChangesAsync();
            }

            // Notify parents that tracking has stopped
            var connectedParents = await _context.StudentParentConnections
                .Where(c => c.StudentId == studentId && c.Status == "Approved")
                .Select(c => c.ParentId.ToString())
                .ToListAsync();

            foreach (var parentId in connectedParents)
            {
                await Clients.Group(parentId).SendAsync("StudentStoppedTracking", studentId);
            }
        }
    }
} 