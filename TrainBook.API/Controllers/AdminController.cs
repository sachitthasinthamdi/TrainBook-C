using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainBook.API.Data;
using TrainBook.API.DTOs;
using TrainBook.API.Models;

namespace TrainBook.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly TrainBookContext _context;
        public AdminController(TrainBookContext context) { _context = context; }

        // ---- สรุปภาพรวม ----
        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            // SQLite รวม decimal ฝั่ง server ไม่ได้ จึงดึงค่ามารวมฝั่ง client
            var amounts = await _context.Bookings
                .Where(b => b.BookingStatus != "CANCELLED")
                .Select(b => b.TotalAmount).ToListAsync();

            return Ok(new
            {
                users = await _context.Users.CountAsync(u => u.Role == "user"),
                schedules = await _context.Schedules.CountAsync(),
                bookings = await _context.Bookings.CountAsync(),
                revenue = amounts.Sum()
            });
        }

        // ---- สมาชิกทั้งหมด ----
        [HttpGet("users")]
        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .OrderBy(u => u.UserId)
                .Select(u => new { u.UserId, u.Username, u.Email, u.FirstName, u.LastName, u.PhoneNumber, u.Role, u.CreatedAt })
                .ToListAsync();
            return Ok(users);
        }

        // ---- การจองทั้งหมด ----
        [HttpGet("bookings")]
        public async Task<IActionResult> Bookings()
        {
            var list = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Schedule).ThenInclude(s => s.Train)
                .Include(b => b.Schedule).ThenInclude(s => s.OriginStation)
                .Include(b => b.Schedule).ThenInclude(s => s.DestinationStation)
                .Include(b => b.Class)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.BookingId, b.BookingReference, b.TravelDate, b.NumberOfPassengers,
                    b.TotalAmount, b.BookingStatus, b.PaymentStatus,
                    userName = b.User != null ? b.User.Username : "-",
                    userEmail = b.User != null ? b.User.Email : "-",
                    trainName = b.Schedule.Train.TrainName, trainNumber = b.Schedule.Train.TrainNumber,
                    origin = b.Schedule.OriginStation.StationName, dest = b.Schedule.DestinationStation.StationName,
                    className = b.Class.ClassName
                })
                .ToListAsync();
            return Ok(list);
        }

        // เปลี่ยนสถานะการจอง (ยกเลิก/คืนสถานะ)
        [HttpPatch("bookings/{id}/status")]
        public async Task<IActionResult> UpdateBookingStatus(int id, [FromBody] UpdateStatusDTO dto)
        {
            var allowed = new[] { "CONFIRMED", "PENDING", "CANCELLED" };
            if (!allowed.Contains(dto.Status)) return BadRequest(new { message = "สถานะไม่ถูกต้อง" });
            var b = await _context.Bookings.FindAsync(id);
            if (b == null) return NotFound(new { message = "ไม่พบการจอง" });
            b.BookingStatus = dto.Status;
            b.PaymentStatus = dto.Status == "CANCELLED" ? "REFUNDED" : "PAID";
            b.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        // ---- ข้อมูลอ้างอิงสำหรับฟอร์มเพิ่มเที่ยวรถ ----
        [HttpGet("meta")]
        public async Task<IActionResult> Meta()
        {
            return Ok(new
            {
                trains = await _context.Trains.Select(t => new { t.TrainId, t.TrainNumber, t.TrainName }).ToListAsync(),
                stations = await _context.Stations.OrderBy(s => s.StationName)
                                .Select(s => new { s.StationId, s.StationName }).ToListAsync(),
                classes = await _context.TicketClasses.Select(c => new { c.ClassId, c.ClassName, c.ClassCode }).ToListAsync()
            });
        }

        // ---- เที่ยวรถทั้งหมด (schedule) ----
        [HttpGet("schedules")]
        public async Task<IActionResult> Schedules()
        {
            var list = await _context.Schedules
                .Include(s => s.Train).Include(s => s.OriginStation).Include(s => s.DestinationStation)
                .Include(s => s.Prices).ThenInclude(p => p.Class)
                .OrderBy(s => s.ScheduleId)
                .Select(s => new
                {
                    s.ScheduleId, trainNumber = s.Train.TrainNumber, trainName = s.Train.TrainName,
                    origin = s.OriginStation.StationName, dest = s.DestinationStation.StationName,
                    s.DepartureTime, s.ArrivalTime, s.IsActive,
                    prices = s.Prices.Select(p => new { className = p.Class.ClassName, p.PriceAmount, p.AvailableSeats })
                })
                .ToListAsync();
            return Ok(list);
        }

        // เพิ่มเที่ยวรถใหม่
        [HttpPost("schedules")]
        public async Task<IActionResult> CreateSchedule([FromBody] CreateScheduleDTO dto)
        {
            if (dto.TrainId == 0 || dto.OriginStationId == 0 || dto.DestinationStationId == 0)
                return BadRequest(new { message = "กรอกข้อมูลขบวน/สถานีให้ครบ" });
            if (dto.OriginStationId == dto.DestinationStationId)
                return BadRequest(new { message = "ต้นทางและปลายทางต้องไม่เหมือนกัน" });

            var s = new Schedule
            {
                TrainId = dto.TrainId,
                OriginStationId = dto.OriginStationId,
                DestinationStationId = dto.DestinationStationId,
                DepartureTime = TimeSpan.Parse(dto.DepartureTime),
                ArrivalTime = TimeSpan.Parse(dto.ArrivalTime),
                DurationMinutes = dto.DurationMinutes,
                IsActive = true
            };
            _context.Schedules.Add(s);
            await _context.SaveChangesAsync();

            foreach (var p in dto.Prices)
                if (p.ClassId != 0 && p.Price > 0)
                    _context.Prices.Add(new Price
                    {
                        ScheduleId = s.ScheduleId, ClassId = p.ClassId,
                        PriceAmount = p.Price, AvailableSeats = p.AvailableSeats,
                        ValidFrom = new DateTime(2020, 1, 1), ValidTo = new DateTime(2030, 12, 31)
                    });
            await _context.SaveChangesAsync();
            return Ok(new { ok = true, scheduleId = s.ScheduleId });
        }

        // เปิด/ปิดการขายเที่ยวรถ
        [HttpPatch("schedules/{id}/active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var s = await _context.Schedules.FindAsync(id);
            if (s == null) return NotFound(new { message = "ไม่พบเที่ยวรถ" });
            s.IsActive = !s.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { ok = true, isActive = s.IsActive });
        }

        // ลบเที่ยวรถ (พร้อมราคาและการจองที่เกี่ยวข้อง)
        [HttpDelete("schedules/{id}")]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var s = await _context.Schedules.FindAsync(id);
            if (s == null) return NotFound(new { message = "ไม่พบเที่ยวรถ" });
            _context.Schedules.Remove(s);   // Prices/Bookings ลบตาม (cascade)
            await _context.SaveChangesAsync();
            return Ok(new { ok = true });
        }
    }
}
