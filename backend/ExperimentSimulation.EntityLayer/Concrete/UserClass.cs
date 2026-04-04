using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class UserClass
    {
        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusRejected = "Rejected";

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int ClassId { get; set; }
        public Class Class { get; set; } = null!;

        public DateTime? JoinedAt { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = StatusApproved;

        public string MemberRole { get; set; } = "Student";
    }
}
