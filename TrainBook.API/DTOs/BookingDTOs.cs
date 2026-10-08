namespace TrainBook.API.DTOs
{
    public class PassengerDTO
    {
        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? IdCardNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? SeatCode { get; set; }
    }

    public class CreateBookingDTO
    {
        public int ScheduleId { get; set; }
        public int ClassId { get; set; }
        public DateTime TravelDate { get; set; }
        public string? PaymentMethod { get; set; }
        public List<PassengerDTO> Passengers { get; set; } = new List<PassengerDTO>();
    }

    public class BookingResponseDTO
    {
        public string BookingReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string QrCodeUrl { get; set; } = string.Empty;
        public string ETicketUrl { get; set; } = string.Empty;
    }
}
