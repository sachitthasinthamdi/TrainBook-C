namespace TrainBook.API.Models
{
    public class Price
    {
        public int PriceId { get; set; }

        public int ScheduleId { get; set; }
        public Schedule Schedule { get; set; } = null!;

        public int ClassId { get; set; }
        public TicketClass Class { get; set; } = null!;

        public decimal PriceAmount { get; set; }
        public int AvailableSeats { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
