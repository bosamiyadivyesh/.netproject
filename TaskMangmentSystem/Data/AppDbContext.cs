using Microsoft.EntityFrameworkCore;
using TaskManagement.Models;

namespace TaskManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // =========================
        // TABLES
        // =========================

        public DbSet<User> Users { get; set; }

        public DbSet<Project> Projects { get; set; }

        public DbSet<TaskItem> Tasks { get; set; }

        public DbSet<TaskAssignment> TaskAssignments { get; set; }

        public DbSet<TaskCompletion> TaskCompletions { get; set; }

        public DbSet<TaskHistory> TaskHistories { get; set; }

        public DbSet<TaskComment> TaskComments { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        [Obsolete]
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =========================
            // USER
            // =========================

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.Role)
                    .HasMaxLength(20)
                    .HasDefaultValue("user");
            });


            // =========================
            // PROJECT
            // =========================

            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(p => p.ProjectId);

                entity.Property(p => p.Name)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.HasOne(p => p.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // =========================
            // TASK
            // =========================

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(t => t.TaskId);

                entity.Property(t => t.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(t => t.Status)
                    .HasMaxLength(20)
                    .HasDefaultValue("pending");

                entity.Property(t => t.Priority)
                    .HasMaxLength(10)
                    .HasDefaultValue("medium");

                // Project -> Tasks
                entity.HasOne(t => t.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(t => t.ProjectId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Created By -> User
                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                // Assigned To -> User
                entity.HasOne(t => t.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedTo)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasCheckConstraint(
      "CK_tasks_status",
      "\"Status\" IN ('pending','in_progress','completed','cancelled')"
  );

                entity.HasCheckConstraint(
                    "CK_tasks_priority",
                    "\"Priority\" IN ('low','medium','high','urgent')"
                );
            });


            // =========================
            // TASK ASSIGNMENT
            // =========================

            modelBuilder.Entity<TaskAssignment>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasIndex(a => new
                {
                    a.TaskId,
                    a.UserId
                }).IsUnique();

                // Task -> Assignments
                entity.HasOne(a => a.Task)
                    .WithMany(t => t.Assignments)
                    .HasForeignKey(a => a.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> Assignments
                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });


            // =========================
            // TASK COMPLETION
            // =========================

            modelBuilder.Entity<TaskCompletion>(entity =>
            {
                entity.HasKey(c => c.CompletionId);

                // Task -> Completions
                entity.HasOne(c => c.Task)
                    .WithMany(t => t.Completions)
                    .HasForeignKey(c => c.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> Completion
                entity.HasOne(c => c.CompletedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CompletedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // =========================
            // TASK HISTORY
            // =========================

            modelBuilder.Entity<TaskHistory>(static entity =>
            {
                entity.HasKey(h => h.HistoryId);

                // Task -> History
                entity.HasOne(h => h.Task)
                    .WithMany(t => t.Histories)
                    .HasForeignKey(h => h.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> History
                entity.HasOne(h => h.ChangedByUser)
                    .WithMany()
                    .HasForeignKey(h => h.ChangedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(h => h.Action)
                    .HasMaxLength(30);
                entity.HasCheckConstraint(
    "CK_task_history_action",
    "\"Action\" IN ('created','updated','deleted','status_changed','completed')"
);
            });


            // =========================
            // TASK COMMENT
            // =========================

            modelBuilder.Entity<TaskComment>(entity =>
            {
                entity.HasKey(c => c.CommentId);

                // Task -> Comments
                entity.HasOne(c => c.Task)
                    .WithMany(t => t.Comments)
                    .HasForeignKey(c => c.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                // User -> Comments
                entity.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });


            // =========================
            // NOTIFICATION
            // =========================

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(n => n.NotificationId);

                // User -> Notifications
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Task -> Notifications
                entity.HasOne(n => n.Task)
                    .WithMany()
                    .HasForeignKey(n => n.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}