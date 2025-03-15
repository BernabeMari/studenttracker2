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

<<<<<<< HEAD
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

=======
>>>>>>> 8b7e4d7a541335e8cb2c0f11f449f889ac63074c
    public class LoginModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
<<<<<<< HEAD
        public string UserType { get; set; } = string.Empty; // "Student", "Parent", or "Teacher"
=======
        public string UserType { get; set; } = string.Empty; // "Student" or "Parent"
>>>>>>> 8b7e4d7a541335e8cb2c0f11f449f889ac63074c
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