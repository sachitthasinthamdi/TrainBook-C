namespace TrainBook.API.DTOs
{
    public class CreatePriceDTO
    {
        public int ClassId { get; set; }
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; } = 40;
    }

    public class CreateScheduleDTO
    {
        public int TrainId { get; set; }
        public int OriginStationId { get; set; }
        public int DestinationStationId { get; set; }
        public string DepartureTime { get; set; } = "00:00";
        public string ArrivalTime { get; set; } = "00:00";
        public int DurationMinutes { get; set; }
        public List<CreatePriceDTO> Prices { get; set; } = new List<CreatePriceDTO>();
    }

    public class UpdateStatusDTO
    {
        public string Status { get; set; } = string.Empty;
    }
}
