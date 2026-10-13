namespace Notificator.Models;

// Entity для хранения пользователей
public class Subscriber
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }
    public required string Username { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool Deleted { get; set; } // Удален, когад пользователь выселился
    public bool IsActive { get; set; } // не активен когда пользователь временно отсутствует
    public Apartment? Apartment { get; set; }
    public long ApartmentId { get; set; }
    public bool IsCurrent { get; set; }
    public required string Role { get; set; }
}

