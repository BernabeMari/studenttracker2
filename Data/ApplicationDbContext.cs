using Microsoft.EntityFrameworkCore;
using StudentTracker.Models;

namespace StudentTracker.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Student> Students => Set<Student>();
        public DbSet<Parent> Parents => Set<Parent>();
<<<<<<< HEAD
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<StudentParentConnection> StudentParentConnections => Set<StudentParentConnection>();
        public DbSet<LocationTracking> LocationTrackings => Set<LocationTracking>();
        public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<StudentTeacherConnection> StudentTeacherConnections => Set<StudentTeacherConnection>();
=======
        public DbSet<StudentParentConnection> StudentParentConnections => Set<StudentParentConnection>();
        public DbSet<LocationTracking> LocationTrackings => Set<LocationTracking>();
        public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();
>>>>>>> 8b7e4d7a541335e8cb2c0f11f449f889ac63074c

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure unique constraints
            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Username)
                .IsUnique();

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.Email)
                .IsUnique();

            modelBuilder.Entity<Parent>()
                .HasIndex(p => p.Username)
                .IsUnique();

            modelBuilder.Entity<Parent>()
                .HasIndex(p => p.Email)
                .IsUnique();

<<<<<<< HEAD
            modelBuilder.Entity<Teacher>()
                .HasIndex(t => t.Username)
                .IsUnique();

            modelBuilder.Entity<Teacher>()
                .HasIndex(t => t.Email)
                .IsUnique();

            // Configure Subject constraints
            modelBuilder.Entity<Subject>()
                .HasIndex(s => new { s.Name, s.TeacherId })
                .IsUnique();

            // Configure StudentTeacherConnection constraints
            modelBuilder.Entity<StudentTeacherConnection>()
                .HasIndex(stc => new { stc.StudentId, stc.TeacherId, stc.SubjectId })
                .IsUnique();

=======
>>>>>>> 8b7e4d7a541335e8cb2c0f11f449f889ac63074c
            // Configure relationships
            modelBuilder.Entity<StudentParentConnection>()
                .HasOne(sp => sp.Student)
                .WithMany()
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentParentConnection>()
                .HasOne(sp => sp.Parent)
                .WithMany()
                .HasForeignKey(sp => sp.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

<<<<<<< HEAD
            // Configure Subject relationships
            modelBuilder.Entity<Subject>()
                .HasOne(s => s.Teacher)
                .WithMany()
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure StudentTeacherConnection relationships
            modelBuilder.Entity<StudentTeacherConnection>()
                .HasOne(stc => stc.Student)
                .WithMany()
                .HasForeignKey(stc => stc.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentTeacherConnection>()
                .HasOne(stc => stc.Teacher)
                .WithMany()
                .HasForeignKey(stc => stc.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentTeacherConnection>()
                .HasOne(stc => stc.Subject)
                .WithMany()
                .HasForeignKey(stc => stc.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

=======
>>>>>>> 8b7e4d7a541335e8cb2c0f11f449f889ac63074c
            // Configure decimal precision
            modelBuilder.Entity<LocationTracking>()
                .HasKey(lt => lt.LocationId);

            modelBuilder.Entity<LocationTracking>()
                .Property(lt => lt.Latitude)
                .HasPrecision(10, 8);

            modelBuilder.Entity<LocationTracking>()
                .Property(lt => lt.Longitude)
                .HasPrecision(11, 8);
        }
    }
} 