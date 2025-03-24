using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTracker.Models
{
    public class StudentParentConnection
    {
        [Key]
        public int ConnectionId { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        [Required]
        public int ParentId { get; set; }
        
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("ParentId")]
        public virtual Parent? Parent { get; set; }
    }

    public class ConnectionRequest
    {
        [Required]
        public string StudentUsername { get; set; } = string.Empty;
    }
} 