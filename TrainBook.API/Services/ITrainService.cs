using TrainBook.API.DTOs;
using TrainBook.API.Models;

namespace TrainBook.API.Services
{
    public interface ITrainService
    {
        Task<List<Station>> GetAllStationsAsync();
        Task<List<TrainResultDTO>> SearchTrainsAsync(TrainSearchDTO searchDTO);
        Task<TicketClass?> GetClassByIdAsync(int classId);
        Task<List<string>> GetTakenSeatsAsync(int scheduleId, int classId, DateTime date);
    }
}
