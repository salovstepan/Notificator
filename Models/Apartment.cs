namespace Notificator.Models;

// Entity для хранения квартир
public class Apartment
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool Deleted { get; set; }
    public ICollection<Subscriber> Subscribers { get; set; } = []; // инициализируем для удобвства
    public ICollection<Schedule> Schedules { get; set; } = [];
}

