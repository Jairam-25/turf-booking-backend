using Domain.Common;

namespace Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public int FailedLoginAttempts { get; set; }
    public bool IsLocked { get; set; }
    // User mobile number
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? LockoutEnd { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? PasswordResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }
    
    // Firebase Cloud Messaging (FCM) Token for Push Notifications
    public string? FcmToken { get; set; }
    
    public string? ProfilePictureUrl { get; set; }

    // Additional User Details
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? MaritalStatus { get; set; } // "Married", "Unmarried", etc.
    public string? PlayerType { get; set; } // e.g., "Football", "Cricket", "Tennis"
    public string? PlayingLevel { get; set; } // e.g., "State Level", "District Level", "National Level", "Amateur"
    
    public DateTime? LastActive { get; set; }
    public string Status { get; set; } = "Active"; // Active, Inactive, Blocked
    
    public ICollection<Booking> Bookings { get; set; }
    = new List<Booking>();
}