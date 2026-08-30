namespace TrainBook.API.Models
{
    public class Passenger
    {
        public int PassengerId { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? IdCardNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
    }
}
