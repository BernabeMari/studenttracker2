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
    public class TrackingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TrackingHub> _hubContext;

        public TrackingController(ApplicationDbContext context, IHubContext<TrackingHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetTrackingStatus()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized("User not authenticated");

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest("Only students can check tracking status");

            var activeSession = await _context.TrackingSessions
                .FirstOrDefaultAsync(s => s.StudentId == currentUserId && s.EndTime == null);

            return Ok(new { 
                isActive = activeSession != null,
                sessionId = activeSession?.SessionId,
                startTime = activeSession?.StartTime
            });
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartTracking()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized("User not authenticated");

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest("Only students can start tracking");

            var student = await _context.Students.FindAsync(currentUserId);
            if (student == null)
                return NotFound("Student not found");

            var activeSession = await _context.TrackingSessions
                .FirstOrDefaultAsync(s => s.StudentId == currentUserId && s.EndTime == null);

            if (activeSession != null)
            {
                // Return the existing session
                return Ok(new 
                { 
                    sessionId = activeSession.SessionId,
                    message = "Tracking session already active"
                });
            }

            var session = new TrackingSession
            {
                StudentId = currentUserId,
                StartTime = DateTime.UtcNow,
                IsActive = true
            };

            _context.TrackingSessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(new { sessionId = session.SessionId });
        }

        [HttpPost("stop")]
        public async Task<IActionResult> StopTracking()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized("User not authenticated");

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest("Only students can stop tracking");

            var activeSession = await _context.TrackingSessions
                .FirstOrDefaultAsync(s => s.StudentId == currentUserId && s.EndTime == null);

            if (activeSession == null)
                return BadRequest("No active tracking session found");

            activeSession.EndTime = DateTime.UtcNow;
            activeSession.IsActive = false;
            await _context.SaveChangesAsync();

            // Notify connected parents
            var connectedParents = await _context.StudentParentConnections
                .Where(c => c.StudentId == currentUserId && c.Status == "Approved")
                .Select(c => c.ParentId.ToString())
                .ToListAsync();

            // Notification is now handled in the TrackingHub.StopTracking method

            return Ok(new { message = "Tracking stopped successfully" });
        }

        [HttpPost("location")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdate update)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized("User not authenticated");

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest("Only students can update location");

            var student = await _context.Students.FindAsync(currentUserId);
            if (student == null)
                return NotFound("Student not found");

            var activeSession = await _context.TrackingSessions
                .FirstOrDefaultAsync(s => s.StudentId == currentUserId && s.EndTime == null);

            if (activeSession == null)
            {
                // Auto-create session if none exists
                activeSession = new TrackingSession
                {
                    StudentId = currentUserId,
                    StartTime = DateTime.UtcNow,
                    IsActive = true
                };
                _context.TrackingSessions.Add(activeSession);
                await _context.SaveChangesAsync();
            }

            var location = new LocationTracking
            {
                SessionId = activeSession.SessionId,
                Latitude = Convert.ToDecimal(update.Latitude),
                Longitude = Convert.ToDecimal(update.Longitude),
                Timestamp = DateTime.UtcNow
            };

            _context.LocationTrackings.Add(location);
            await _context.SaveChangesAsync();

            // Notify connected parents
            var connectedParents = await _context.StudentParentConnections
                .Where(c => c.StudentId == currentUserId && c.Status == "Approved")
                .Select(c => c.ParentId.ToString())
                .ToListAsync();

            foreach (var parentId in connectedParents)
            {
                await _hubContext.Clients.User(parentId).SendAsync("ReceiveLocation", 
                    currentUserId, 
                    student.Username, 
                    update.Latitude, 
                    update.Longitude);
            }

            return Ok();
        }

        [HttpGet("history/{studentId}")]
        public async Task<IActionResult> GetLocationHistory(int studentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized("User not authenticated");

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Parent")
                return BadRequest("Only parents can view location history");

            var connection = await _context.StudentParentConnections
                .FirstOrDefaultAsync(c => c.StudentId == studentId && 
                                        c.ParentId == currentUserId && 
                                        c.Status == "Approved");

            if (connection == null)
                return BadRequest("No approved connection with this student");

            var history = await _context.TrackingSessions
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.StartTime)
                .Take(10)
                .Select(s => new
                {
                    s.SessionId,
                    s.StartTime,
                    s.EndTime,
                    s.IsActive,
                    Locations = _context.LocationTrackings
                        .Where(l => l.SessionId == s.SessionId)
                        .OrderBy(l => l.Timestamp)
                        .Select(l => new
                        {
                            l.Latitude,
                            l.Longitude,
                            l.Timestamp
                        })
                })
                .ToListAsync();

            return Ok(history);
        }
    }

    public class LocationUpdate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
} 