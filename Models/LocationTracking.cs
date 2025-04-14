using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StudentTracker.Models
{
    public class LocationTracking
    {
        public int LocationId { get; set; }
        public int SessionId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual TrackingSession? Session { get; set; }
    }

    public class TrackingSession
    {
        [Key]
        public int SessionId { get; set; }
        
        [Required]
        public int StudentId { get; set; }
        
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool IsActive { get; set; }

        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }
    }

    public class LocationUpdate
    {
        [Required]
        public decimal Latitude { get; set; }
        
        [Required]
        public decimal Longitude { get; set; }
    }
} 