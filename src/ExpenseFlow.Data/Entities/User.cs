using Microsoft.AspNetCore.Identity;

namespace ExpenseFlow.Data.Entities;

public enum UserRole
{
    Employee,
    Manager
}

public class User : IdentityUser<Guid>
{
    public required string Name { get; set; }

    public UserRole Role { get; set; }

    public Guid? ManagerId { get; set; }

    public User? Manager { get; set; }

    public ICollection<User> DirectReports { get; set; } = new List<User>();
}
