namespace Notificator.Models;

// Entity для хранения расписания
public class Schedule
{
    public int Id { get; set; }
    public required string Description { get; set; } // тут будет хранится описания того, что нужно делать постояльцу
    public DayOfWeek Day { get; set; }
    public DateTime LastNotification { get; set; }
    public Apartment? Apartment { get; set; }
    public long ApartmentId { get; set; }
}

