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
        public DbSet<StudentParentConnection> StudentParentConnections => Set<StudentParentConnection>();
        public DbSet<LocationTracking> LocationTrackings => Set<LocationTracking>();
        public DbSet<TrackingSession> TrackingSessions => Set<TrackingSession>();

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