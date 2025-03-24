using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTracker.Models
{
    public class AttendanceSession
    {
        [Key]
        public int SessionId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
        
        [ForeignKey("TeacherId")]
        public virtual Teacher? Teacher { get; set; }
    }
    
    public class AttendanceRecord
    {
        [Key]
        public int AttendanceId { get; set; }
        
        [Required]
        public int SessionId { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("SessionId")]
        public virtual AttendanceSession? Session { get; set; }
        
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
        
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
    }
    
    public class CreateSessionRequest
    {
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
    }
    
    public class RecordAttendanceRequest
    {
        [Required]
        public int SessionId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        // Optional Student ID for legacy QR format support
        // If not provided, will use the authenticated user's ID
        public int? StudentId { get; set; }
    }
} 