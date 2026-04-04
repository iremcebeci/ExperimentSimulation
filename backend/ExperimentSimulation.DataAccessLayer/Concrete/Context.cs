using ExperimentSimulation.EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSimulation.DataAccessLayer.Concrete
{
    public class Context:DbContext
    {
        public Context(DbContextOptions<Context> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Eğer options DI'dan gelmemişse, default connection string'i kullan
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Server=localhost;Database=ExperimentSimulation;Uid=root;Pwd=0000;";
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<Class> Classes { get; set; }
        public DbSet<UserClass> UserClasses { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<ContentTask> ContentTasks { get; set; }
        public DbSet<ContentTaskComment> ContentTaskComments { get; set; }
        public DbSet<ContentTaskRevisionRequest> ContentTaskRevisionRequests { get; set; }
        public DbSet<TodoItem> TodoItems { get; set; }
        public DbSet<TodoSubtask> TodoSubtasks { get; set; }

        public DbSet<Experiment> Experiments { get; set; }
        public DbSet<UserSessionActivity> UserSessionActivities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<Class>().ToTable("class");
            modelBuilder.Entity<UserClass>().ToTable("userclass");
            modelBuilder.Entity<Assignment>().ToTable("assignments");
            modelBuilder.Entity<ContentTask>().ToTable("content_tasks");
            modelBuilder.Entity<ContentTaskComment>().ToTable("content_task_comments");
            modelBuilder.Entity<ContentTaskRevisionRequest>().ToTable("content_task_revision_requests");
            modelBuilder.Entity<TodoItem>().ToTable("todo_items");
            modelBuilder.Entity<TodoSubtask>().ToTable("todo_subtasks");
            modelBuilder.Entity<UserSessionActivity>().ToTable("user_session_activities");

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserSessionActivity>()
                .HasOne(a => a.User)
                .WithMany(u => u.SessionActivities)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserSessionActivity>()
                .HasIndex(a => new { a.UserId, a.LoginAt });

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Class)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContentTask>()
                .Property(t => t.Title)
                .HasMaxLength(200)
                .IsRequired();

            modelBuilder.Entity<ContentTask>()
                .Property(t => t.TaskType)
                .HasMaxLength(120);

            modelBuilder.Entity<ContentTask>()
                .Property(t => t.ExperimentName)
                .HasMaxLength(160);

            modelBuilder.Entity<ContentTask>()
                .Property(t => t.EstimatedDuration)
                .HasMaxLength(80);

            modelBuilder.Entity<ContentTask>()
                .Property(t => t.Priority)
                .HasMaxLength(40)
                .HasDefaultValue("Orta");

            modelBuilder.Entity<ContentTask>()
                .Property(t => t.Status)
                .HasMaxLength(40)
                .HasDefaultValue("Atandı");

            modelBuilder.Entity<ContentTask>()
                .HasOne(t => t.AssigneeUser)
                .WithMany(u => u.AssignedContentTasks)
                .HasForeignKey(t => t.AssigneeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContentTask>()
                .HasOne(t => t.CreatedByUser)
                .WithMany(u => u.CreatedContentTasks)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContentTask>()
                .HasIndex(t => t.AssigneeUserId);

            modelBuilder.Entity<ContentTask>()
                .HasIndex(t => t.Deadline);

            modelBuilder.Entity<ContentTask>()
                .HasIndex(t => t.Status);

            modelBuilder.Entity<ContentTaskComment>()
                .Property(c => c.Text)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<ContentTaskComment>()
                .HasOne(c => c.ContentTask)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.ContentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContentTaskComment>()
                .HasOne(c => c.User)
                .WithMany(u => u.ContentTaskComments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContentTaskComment>()
                .HasIndex(c => new { c.ContentTaskId, c.CreatedAtUtc });

            modelBuilder.Entity<ContentTaskRevisionRequest>()
                .Property(r => r.RevisionType)
                .HasMaxLength(80)
                .IsRequired();

            modelBuilder.Entity<ContentTaskRevisionRequest>()
                .Property(r => r.Priority)
                .HasMaxLength(40)
                .HasDefaultValue("Orta");

            modelBuilder.Entity<ContentTaskRevisionRequest>()
                .Property(r => r.Note)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<ContentTaskRevisionRequest>()
                .HasOne(r => r.ContentTask)
                .WithMany(t => t.RevisionRequests)
                .HasForeignKey(r => r.ContentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ContentTaskRevisionRequest>()
                .HasOne(r => r.RequestedByUser)
                .WithMany(u => u.ContentTaskRevisionRequests)
                .HasForeignKey(r => r.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ContentTaskRevisionRequest>()
                .HasIndex(r => new { r.ContentTaskId, r.CreatedAtUtc });

            modelBuilder.Entity<TodoItem>()
                .Property(t => t.Title)
                .HasMaxLength(240)
                .IsRequired();

            modelBuilder.Entity<TodoItem>()
                .Property(t => t.Priority)
                .HasMaxLength(40)
                .HasDefaultValue("Orta");

            modelBuilder.Entity<TodoItem>()
                .Property(t => t.Description)
                .HasMaxLength(4000);

            modelBuilder.Entity<TodoItem>()
                .Property(t => t.Notes)
                .HasMaxLength(4000);

            modelBuilder.Entity<TodoItem>()
                .HasOne(t => t.User)
                .WithMany(u => u.TodoItems)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => new { t.UserId, t.DueDate });

            modelBuilder.Entity<TodoItem>()
                .HasIndex(t => new { t.UserId, t.IsCompleted });

            modelBuilder.Entity<TodoSubtask>()
                .Property(s => s.Title)
                .HasMaxLength(240)
                .IsRequired();

            modelBuilder.Entity<TodoSubtask>()
                .HasOne(s => s.TodoItem)
                .WithMany(t => t.Subtasks)
                .HasForeignKey(s => s.TodoItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TodoSubtask>()
                .HasIndex(s => new { s.TodoItemId, s.CreatedAtUtc });

            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Student", Description = "Student user" },
                new Role { Id = 2, Name = "Teacher", Description = "Teacher user" },
                new Role { Id = 3, Name = "Independent", Description = "Independent user" },
                new Role { Id = 4, Name = "ContentCreator", Description = "Creates content" },
                new Role { Id = 5, Name = "Admin", Description = "System admin" }
            );

            modelBuilder.Entity<UserClass>()
                .HasKey(uc => new { uc.UserId, uc.ClassId });

            modelBuilder.Entity<UserClass>()
                .HasOne(uc => uc.User)
                .WithMany(u => u.UserClasses)
                .HasForeignKey(uc => uc.UserId);

            modelBuilder.Entity<UserClass>()
                .HasOne(uc => uc.Class)
                .WithMany(c => c.UserClasses)
                .HasForeignKey(uc => uc.ClassId);

            modelBuilder.Entity<UserClass>()
                .Property(uc => uc.Status)
                .HasMaxLength(20)
                .HasDefaultValue(UserClass.StatusApproved);

            modelBuilder.Entity<UserClass>()
                .Property(uc => uc.MemberRole)
                .HasMaxLength(20);

            modelBuilder.Entity<UserClass>()
                .Property(uc => uc.RequestedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP()");

            modelBuilder.Entity<Class>()
                .HasIndex(c => c.Code)
                .IsUnique();
        }
    }
}
