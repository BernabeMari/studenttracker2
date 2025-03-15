using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTracker.Models
{
    public class StudentTeacherConnection
    {
        [Key]
        public int ConnectionId { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int TeacherId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
        
        public string Status { get; set; } = "Pending";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
        
        [ForeignKey("TeacherId")]
        public virtual Teacher? Teacher { get; set; }
        
        [ForeignKey("SubjectId")]
        public virtual Subject? Subject { get; set; }
    }
    
    public class StudentConnectionRequest
    {
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int SubjectId { get; set; }
    }
    
    public class ConnectionStatusUpdateModel
    {
        [Required]
        public int ConnectionId { get; set; }
        
        [Required]
        public string Status { get; set; } = string.Empty;
    }
} 