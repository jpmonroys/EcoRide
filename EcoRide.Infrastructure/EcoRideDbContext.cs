using EcoRide.Domain;
using Microsoft.EntityFrameworkCore;

namespace EcoRide.Infrastructure;

public sealed class EcoRideDbContext(DbContextOptions<EcoRideDbContext> options) : DbContext(options)
{
    public DbSet<Trip> Trips => Set<Trip>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var trip = modelBuilder.Entity<Trip>();
        trip.ToTable("Trips");
        trip.HasKey(x => x.Id);
        trip.Property(x => x.VehicleId).IsRequired().HasMaxLength(128);
        trip.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);

        trip.OwnsOne(x => x.Origin, origin =>
        {
            origin.Property(x => x.Latitude).HasColumnName("OriginLatitude");
            origin.Property(x => x.Longitude).HasColumnName("OriginLongitude");
        });

        trip.OwnsOne(x => x.Destination, destination =>
        {
            destination.Property(x => x.Latitude).HasColumnName("DestinationLatitude");
            destination.Property(x => x.Longitude).HasColumnName("DestinationLongitude");
        });

        base.OnModelCreating(modelBuilder);
    }
}
