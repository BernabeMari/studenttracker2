using System;
using System.ComponentModel.DataAnnotations;

namespace StudentTracker.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;
        
        public string ProfilePic { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }

    public class Parent
    {
        public int ParentId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }

    public class Teacher
    {
        public int TeacherId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;
        
        public string ProfilePic { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? UpdatedAt { get; set; }
    }

    public class LoginModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string UserType { get; set; } = string.Empty; // "Student", "Parent", or "Teacher"
    }

    public class RegisterModel : LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; } = string.Empty;
    }
} 