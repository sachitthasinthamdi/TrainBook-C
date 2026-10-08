using Microsoft.AspNetCore.Mvc;
using TrainBook.API.DTOs;
using TrainBook.API.Services;

namespace TrainBook.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainsController : ControllerBase
    {
        private readonly ITrainService _trainService;

        public TrainsController(ITrainService trainService)
        {
            _trainService = trainService;
        }

        [HttpGet("stations")]
        public async Task<IActionResult> GetStations()
        {
            var stations = await _trainService.GetAllStationsAsync();
            return Ok(stations);
        }

        [HttpPost("search")]
        public async Task<IActionResult> SearchTrains([FromBody] TrainSearchDTO searchDTO)
        {
            var results = await _trainService.SearchTrainsAsync(searchDTO);
            return Ok(results);
        }

        // ที่นั่งที่ถูกจองแล้ว (สำหรับหน้าเลือกที่นั่ง)
        [HttpGet("schedules/{scheduleId}/classes/{classId}/taken-seats")]
        public async Task<IActionResult> TakenSeats(int scheduleId, int classId, [FromQuery] DateTime date)
        {
            var seats = await _trainService.GetTakenSeatsAsync(scheduleId, classId, date);
            return Ok(seats);
        }
    }
}
