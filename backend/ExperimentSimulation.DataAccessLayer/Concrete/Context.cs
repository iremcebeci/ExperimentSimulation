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
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var str = $"Server=localhost;" +
             $"Database=ExperimentSimulation;" +
             $"Uid=root;" +
             $"Pwd=0000";
            optionsBuilder.UseMySql(str
                , ServerVersion.AutoDetect(str)
            );
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
        }
    }
}
