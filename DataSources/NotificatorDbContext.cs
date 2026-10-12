using Microsoft.EntityFrameworkCore;

namespace Notificator.DataSources;

internal class NotificatorDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Subscriber> Subscribers { get; set; }
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<Schedule> Schedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var subscriber = modelBuilder.Entity<Subscriber>();
        subscriber.ToTable("subscribers");
        subscriber.HasKey(u => u.Id);
        subscriber.Property(u => u.Id).HasColumnName("id").UseAutoincrement();
        subscriber.Property(u => u.Username).HasColumnName("username");
        subscriber.Property(u => u.CreatedAt).HasColumnName("created_at");
        subscriber.Property(u => u.Deleted).HasColumnName("deleted").HasDefaultValue(false);
        subscriber.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(false);
        subscriber.Property(u => u.ApartmentId).HasColumnName("apartment_id");
        subscriber.HasOne(u => u.Apartment).WithMany(u => u.Subscribers).HasForeignKey(u => u.ApartmentId);

        var apartment = modelBuilder.Entity<Apartment>();
        apartment.HasKey(u => u.Id);
        apartment.Property(u => u.Id).HasColumnName("id").UseAutoincrement();
        apartment.Property(u => u.Name).HasColumnName("name").HasMaxLength(400); // 400 символов для адреса должно хватить
        apartment.HasMany(u => u.Subscribers).WithOne(u => u.Apartment);

        var schedule = modelBuilder.Entity<Schedule>();
        schedule.HasKey(u => u.Id);
        schedule.Property(u => u.Id).HasColumnName("id").UseAutoincrement();
        schedule.Property(u => u.Description).HasColumnName("description").HasMaxLength(400);

        base.OnModelCreating(modelBuilder);
    }
}
