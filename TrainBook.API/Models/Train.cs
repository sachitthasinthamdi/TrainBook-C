namespace TrainBook.API.Models
{
    public class Train
    {
        public int TrainId { get; set; }
        public string TrainNumber { get; set; } = string.Empty;
        public string TrainName { get; set; } = string.Empty;
        public string? TrainType { get; set; }
    }
}
