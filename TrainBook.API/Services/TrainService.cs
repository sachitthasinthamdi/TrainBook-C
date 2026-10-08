using Microsoft.EntityFrameworkCore;
using TrainBook.API.Data;
using TrainBook.API.DTOs;

namespace TrainBook.API.Services
{
    public class TrainService : ITrainService
    {
        private readonly TrainBookContext _context;

        public TrainService(TrainBookContext context)
        {
            _context = context;
        }

        public async Task<List<Models.Station>> GetAllStationsAsync()
        {
            return await _context.Stations
                .OrderBy(s => s.StationName)
                .ToListAsync();
        }

        public async Task<List<TrainResultDTO>> SearchTrainsAsync(TrainSearchDTO searchDTO)
        {
            var schedules = await _context.Schedules
                .Include(s => s.Train)
                .Include(s => s.OriginStation)
                .Include(s => s.DestinationStation)
                .Where(s => s.OriginStationId == searchDTO.OriginStationId &&
                            s.DestinationStationId == searchDTO.DestinationStationId &&
                            s.IsActive)
                .ToListAsync();

            var results = new List<TrainResultDTO>();

            foreach (var schedule in schedules)
            {
                var prices = await _context.Prices
                    .Include(p => p.Class)
                    .Where(p => p.ScheduleId == schedule.ScheduleId &&
                                p.ValidFrom <= searchDTO.TravelDate &&
                                p.ValidTo >= searchDTO.TravelDate &&
                                p.AvailableSeats > 0)
                    .ToListAsync();

                if (!string.IsNullOrEmpty(searchDTO.ClassFilter))
                    prices = prices.Where(p => p.Class.ClassCode == searchDTO.ClassFilter).ToList();

                if (searchDTO.MaxPrice.HasValue)
                    prices = prices.Where(p => p.PriceAmount <= searchDTO.MaxPrice.Value).ToList();

                results.Add(new TrainResultDTO
                {
                    ScheduleId = schedule.ScheduleId,
                    TrainNumber = schedule.Train.TrainNumber,
                    TrainName = schedule.Train.TrainName,
                    OriginStation = schedule.OriginStation.StationName,
                    DestinationStation = schedule.DestinationStation.StationName,
                    DepartureTime = schedule.DepartureTime,
                    ArrivalTime = schedule.ArrivalTime,
                    DurationMinutes = schedule.DurationMinutes,
                    Classes = prices.Select(p => new ClassPriceDTO
                    {
                        ClassId = p.ClassId,
                        ClassName = p.Class.ClassName,
                        ClassCode = p.Class.ClassCode,
                        Price = p.PriceAmount,
                        AvailableSeats = p.AvailableSeats
                    }).ToList()
                });
            }

            return results;
        }

        public async Task<Models.TicketClass?> GetClassByIdAsync(int classId)
        {
            return await _context.TicketClasses.FindAsync(classId);
        }

        // ที่นั่งที่ถูกจองแล้ว ของเที่ยวรถ+ชั้น+วันเดินทางที่ระบุ (สำหรับล็อกที่นั่ง)
        public async Task<List<string>> GetTakenSeatsAsync(int scheduleId, int classId, DateTime date)
        {
            var next = date.Date.AddDays(1);
            return await _context.Passengers
                .Where(p => p.SeatCode != null
                    && p.Booking.ScheduleId == scheduleId
                    && p.Booking.ClassId == classId
                    && p.Booking.TravelDate >= date.Date && p.Booking.TravelDate < next
                    && p.Booking.BookingStatus != "CANCELLED")
                .Select(p => p.SeatCode!)
                .ToListAsync();
        }
    }
}
