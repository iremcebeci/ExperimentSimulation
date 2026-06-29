using System;

namespace ExperimentSimulation.EntityLayer.Concrete
{
    public class TeacherRoleRequest
    {
        public const string StatusPending = "Pending";
        public const string StatusApproved = "Approved";
        public const string StatusRejected = "Rejected";

        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public string Status { get; set; } = StatusPending;

        public string? Note { get; set; }
        public string? DecisionNote { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAtUtc { get; set; }

        public int? ReviewedByUserId { get; set; }
    }
}
