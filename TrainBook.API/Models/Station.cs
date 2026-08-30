namespace TrainBook.API.Models
{
    public class Station
    {
        public int StationId { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string? StationCode { get; set; }
    }
}
