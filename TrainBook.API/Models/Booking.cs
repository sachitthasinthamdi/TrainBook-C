namespace TrainBook.API.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; } = string.Empty;

        public int UserId { get; set; }
        public User? User { get; set; }

        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; } = null!;

        public int ClassId { get; set; }
        public TicketClass Class { get; set; } = null!;

        public DateTime TravelDate { get; set; }
        public int NumberOfPassengers { get; set; }
        public decimal TotalAmount { get; set; }
        public string BookingStatus { get; set; } = "PENDING";
        public string PaymentStatus { get; set; } = "UNPAID";
        public string? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Passenger> Passengers { get; set; } = new List<Passenger>();
    }
}
