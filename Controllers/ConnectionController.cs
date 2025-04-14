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
    public class ConnectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConnectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("request")]
        public async Task<IActionResult> RequestConnection(ConnectionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Parent")
                return BadRequest(new { message = "Only parents can send connection requests" });

            var parent = await _context.Parents.FindAsync(currentUserId);
            if (parent == null)
                return NotFound(new { message = "Parent not found" });

            var student = await _context.Students.FirstOrDefaultAsync(s => s.Username == request.StudentUsername);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            // Check for existing pending or approved connection
            var existingConnection = await _context.StudentParentConnections
                .FirstOrDefaultAsync(c => c.StudentId == student.StudentId && 
                                       c.ParentId == currentUserId && 
                                       (c.Status == "Pending" || c.Status == "Approved"));

            if (existingConnection != null)
                return BadRequest(new { message = "Connection request already exists" });

            // Count previous rejections
            var rejectionCount = await _context.StudentParentConnections
                .CountAsync(c => c.StudentId == student.StudentId && 
                                c.ParentId == currentUserId && 
                                c.Status == "Rejected");

            // If already rejected 3 times, don't allow more requests
            if (rejectionCount >= 3)
                return BadRequest(new { message = "Ang request mo ay na-reject na ng 3 beses. Hindi na maaaring mag-request muli sa estudyante na ito." });

            var connection = new StudentParentConnection
            {
                StudentId = student.StudentId,
                ParentId = currentUserId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.StudentParentConnections.Add(connection);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Connection request sent successfully" });
        }

        [HttpPost("{connectionId}/accept")]
        public async Task<IActionResult> AcceptConnection(int connectionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest(new { message = "Only students can accept connection requests" });

            var connection = await _context.StudentParentConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && c.StudentId == currentUserId);

            if (connection == null)
                return NotFound(new { message = "Connection request not found" });

            if (connection.Status != "Pending")
                return BadRequest(new { message = "Connection request already processed" });

            connection.Status = "Approved";
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Connection request approved" });
        }

        [HttpPost("{connectionId}/reject")]
        public async Task<IActionResult> RejectConnection(int connectionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var currentUserId = int.Parse(userIdClaim.Value);
            
            if (userTypeClaim.Value != "Student")
                return BadRequest(new { message = "Only students can reject connection requests" });

            var connection = await _context.StudentParentConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connectionId && c.StudentId == currentUserId);

            if (connection == null)
                return NotFound(new { message = "Connection request not found" });

            if (connection.Status != "Pending")
                return BadRequest(new { message = "Connection request already processed" });

            connection.Status = "Rejected";
            connection.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Count rejections after this one
            var rejectionCount = await _context.StudentParentConnections
                .CountAsync(c => c.StudentId == connection.StudentId && 
                                c.ParentId == connection.ParentId && 
                                c.Status == "Rejected");

            // Include information about the rejection limit in the response
            string additionalMessage = "";
            if (rejectionCount >= 3) {
                additionalMessage = " Naabot na ang maximum na 3 rejections. Hindi na maaaring magsend ng request ang magulang.";
            }

            return Ok(new { 
                message = "Connection request rejected" + additionalMessage,
                rejectionCount = rejectionCount,
                maxRejectionsReached = rejectionCount >= 3
            });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingConnections()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var currentUserId = int.Parse(userIdClaim.Value);

            if (userTypeClaim.Value == "Student")
            {
                var studentConnections = await _context.StudentParentConnections
                    .Include(c => c.Parent)
                    .Where(c => c.StudentId == currentUserId && c.Status == "Pending")
                    .Select(c => new
                    {
                        c.ConnectionId,
                        c.Status,
                        c.CreatedAt,
                        Parent = c.Parent == null ? null : new
                        {
                            c.Parent.ParentId,
                            c.Parent.Username,
                            c.Parent.Email,
                            c.Parent.Fullname
                        }
                    })
                    .ToListAsync();

                return Ok(studentConnections);
            }
            else if (userTypeClaim.Value == "Parent")
            {
                var parentConnections = await _context.StudentParentConnections
                    .Include(c => c.Student)
                    .Where(c => c.ParentId == currentUserId && c.Status == "Pending")
                    .Select(c => new
                    {
                        c.ConnectionId,
                        c.Status,
                        c.CreatedAt,
                        Student = c.Student == null ? null : new
                        {
                            c.Student.StudentId,
                            c.Student.Username,
                            c.Student.Email,
                            c.Student.Fullname
                        }
                    })
                    .ToListAsync();

                return Ok(parentConnections);
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

            var currentUserId = int.Parse(userIdClaim.Value);

            if (userTypeClaim.Value == "Student")
            {
                var connectedParents = await _context.StudentParentConnections
                    .Include(c => c.Parent)
                    .Where(c => c.StudentId == currentUserId && c.Status == "Approved")
                    .Select(c => new
                    {
                        c.Parent.ParentId,
                        c.Parent.Username,
                        c.Parent.Email,
                        c.Parent.Fullname
                    })
                    .ToListAsync();

                return Ok(connectedParents);
            }
            else if (userTypeClaim.Value == "Parent")
            {
                var connectedStudents = await _context.StudentParentConnections
                    .Include(c => c.Student)
                    .Where(c => c.ParentId == currentUserId && c.Status == "Approved")
                    .Select(c => new
                    {
                        c.Student.StudentId,
                        c.Student.Username,
                        c.Student.Email,
                        c.Student.Fullname
                    })
                    .ToListAsync();

                return Ok(connectedStudents);
            }

            return BadRequest(new { message = "Invalid user type" });
        }
        
        [HttpGet("rejected")]
        public async Task<IActionResult> GetRejectedConnections()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var userTypeClaim = User.FindFirst(ClaimTypes.Role);
            
            if (userIdClaim == null || userTypeClaim == null)
                return Unauthorized(new { message = "User not authenticated" });

            var currentUserId = int.Parse(userIdClaim.Value);

            if (userTypeClaim.Value == "Student")
            {
                var rejectedConnections = await _context.StudentParentConnections
                    .Include(c => c.Parent)
                    .Where(c => c.StudentId == currentUserId && c.Status == "Rejected")
                    .Select(c => new
                    {
                        c.ConnectionId,
                        c.Status,
                        c.CreatedAt,
                        c.UpdatedAt,
                        Parent = c.Parent == null ? null : new
                        {
                            c.Parent.ParentId,
                            c.Parent.Username,
                            c.Parent.Email,
                            c.Parent.Fullname
                        }
                    })
                    .ToListAsync();

                return Ok(rejectedConnections);
            }
            else if (userTypeClaim.Value == "Parent")
            {
                var rejectedRequests = await _context.StudentParentConnections
                    .Include(c => c.Student)
                    .Where(c => c.ParentId == currentUserId && c.Status == "Rejected")
                    .Select(c => new
                    {
                        c.ConnectionId,
                        c.Status,
                        c.CreatedAt,
                        c.UpdatedAt,
                        Student = c.Student == null ? null : new
                        {
                            c.Student.StudentId,
                            c.Student.Username,
                            c.Student.Email,
                            c.Student.Fullname
                        }
                    })
                    .ToListAsync();

                return Ok(rejectedRequests);
            }

            return BadRequest(new { message = "Invalid user type" });
        }
    }
} 