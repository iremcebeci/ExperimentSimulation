using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public string PasswordSalt { get; set; } = null!;

        public int RoleId { get; set; }
        public Role? Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLogin { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ProfilePictureUrl { get; set; }

        public string? Phone { get; set; }
        public DateTime? BirthDate { get; set; }

        public ICollection<UserClass> UserClasses { get; set; } = new List<UserClass>();
        public ICollection<UserSessionActivity> SessionActivities { get; set; } = new List<UserSessionActivity>();
        public ICollection<ContentTask> AssignedContentTasks { get; set; } = new List<ContentTask>();
        public ICollection<ContentTask> CreatedContentTasks { get; set; } = new List<ContentTask>();
        public ICollection<ContentTaskComment> ContentTaskComments { get; set; } = new List<ContentTaskComment>();
        public ICollection<ContentTaskRevisionRequest> ContentTaskRevisionRequests { get; set; } = new List<ContentTaskRevisionRequest>();
        public ICollection<TodoItem> TodoItems { get; set; } = new List<TodoItem>();
    }
}
