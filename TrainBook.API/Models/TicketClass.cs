namespace TrainBook.API.Models
{
    public class TicketClass
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
