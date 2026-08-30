using Microsoft.EntityFrameworkCore;
using TrainBook.API.Models;

namespace TrainBook.API.Data
{
    public class TrainBookContext : DbContext
    {
        public TrainBookContext(DbContextOptions<TrainBookContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Station> Stations => Set<Station>();
        public DbSet<Train> Trains => Set<Train>();
        public DbSet<TicketClass> TicketClasses => Set<TicketClass>();
        public DbSet<Schedule> Schedules => Set<Schedule>();
        public DbSet<Price> Prices => Set<Price>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Passenger> Passengers => Set<Passenger>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ClassId ไม่ตรงกับชื่อ entity (TicketClass) จึงต้องระบุ primary key เอง
            modelBuilder.Entity<TicketClass>().HasKey(t => t.ClassId);

            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
            modelBuilder.Entity<Booking>().HasIndex(b => b.BookingReference).IsUnique();

            // Schedule มี FK ไปที่ Station 2 ตัว (ต้นทาง/ปลายทาง) — ปิด cascade กันเส้นทางลบซ้อน
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.OriginStation)
                .WithMany()
                .HasForeignKey(s => s.OriginStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.DestinationStation)
                .WithMany()
                .HasForeignKey(s => s.DestinationStationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Train).WithMany().HasForeignKey(s => s.TrainId);

            modelBuilder.Entity<Price>()
                .HasOne(p => p.Schedule).WithMany(s => s.Prices).HasForeignKey(p => p.ScheduleId);
            modelBuilder.Entity<Price>()
                .HasOne(p => p.Class).WithMany().HasForeignKey(p => p.ClassId);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Schedule).WithMany().HasForeignKey(b => b.ScheduleId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Class).WithMany().HasForeignKey(b => b.ClassId);
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User).WithMany(u => u.Bookings).HasForeignKey(b => b.UserId);

            modelBuilder.Entity<Passenger>()
                .HasOne(p => p.Booking).WithMany(b => b.Passengers).HasForeignKey(p => p.BookingId);

            // ความแม่นยำของราคา (สำหรับผู้ให้บริการที่รองรับ)
            modelBuilder.Entity<Price>().Property(p => p.PriceAmount).HasPrecision(10, 2);
            modelBuilder.Entity<Booking>().Property(b => b.TotalAmount).HasPrecision(10, 2);
        }
    }
}
