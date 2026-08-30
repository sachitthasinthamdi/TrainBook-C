namespace TrainBook.API.Models
{
    public class Schedule
    {
        public int ScheduleId { get; set; }

        public int TrainId { get; set; }
        public Train Train { get; set; } = null!;

        public int OriginStationId { get; set; }
        public Station OriginStation { get; set; } = null!;

        public int DestinationStationId { get; set; }
        public Station DestinationStation { get; set; } = null!;

        public TimeSpan DepartureTime { get; set; }
        public TimeSpan ArrivalTime { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; } = true;

        public ICollection<Price> Prices { get; set; } = new List<Price>();
    }
}
