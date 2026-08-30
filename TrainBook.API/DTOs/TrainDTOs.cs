namespace TrainBook.API.DTOs
{
    public class TrainSearchDTO
    {
        public int OriginStationId { get; set; }
        public int DestinationStationId { get; set; }
        public DateTime TravelDate { get; set; }
        public string? ClassFilter { get; set; }
        public decimal? MaxPrice { get; set; }
    }

    public class ClassPriceDTO
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int AvailableSeats { get; set; }
    }

    public class TrainResultDTO
    {
        public int ScheduleId { get; set; }
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public string OriginStation { get; set; } = string.Empty;
        public string DestinationStation { get; set; } = string.Empty;
        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public int DurationMinutes { get; set; }
        public List<ClassPriceDTO> Classes { get; set; } = new List<ClassPriceDTO>();
    }
}
