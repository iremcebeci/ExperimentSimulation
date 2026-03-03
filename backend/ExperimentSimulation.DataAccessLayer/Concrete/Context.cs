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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

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

            modelBuilder.Entity<Class>()
                .HasIndex(c => c.Code)
                .IsUnique();
        }
    }
}
