using Microsoft.EntityFrameworkCore;
using Net.Codecrete.QrCodeGenerator;
using TrainBook.API.Data;
using TrainBook.API.DTOs;
using TrainBook.API.Models;

namespace TrainBook.API.Services
{
    public class BookingService : IBookingService
    {
        private readonly TrainBookContext _context;
        private readonly IWebHostEnvironment _environment;

        public BookingService(TrainBookContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<BookingResponseDTO> CreateBookingAsync(CreateBookingDTO bookingDTO, int userId)
        {
            string bookingReference = GenerateBookingReference();

            var price = await _context.Prices
                .FirstOrDefaultAsync(p => p.ScheduleId == bookingDTO.ScheduleId &&
                                          p.ClassId == bookingDTO.ClassId &&
                                          p.ValidFrom <= bookingDTO.TravelDate &&
                                          p.ValidTo >= bookingDTO.TravelDate);

            if (price == null)
                throw new Exception("ไม่พบราคาสำหรับชั้นโดยสารและวันที่ที่เลือก");

            decimal totalAmount = price.PriceAmount * bookingDTO.Passengers.Count;

            var booking = new Booking
            {
                UserId = userId,
                BookingReference = bookingReference,
                ScheduleId = bookingDTO.ScheduleId,
                ClassId = bookingDTO.ClassId,
                TravelDate = bookingDTO.TravelDate,
                NumberOfPassengers = bookingDTO.Passengers.Count,
                TotalAmount = totalAmount,
                BookingStatus = "CONFIRMED",
                PaymentStatus = "PAID",
                PaymentMethod = bookingDTO.PaymentMethod,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            foreach (var passengerDTO in bookingDTO.Passengers)
            {
                _context.Passengers.Add(new Passenger
                {
                    BookingId = booking.BookingId,
                    Title = passengerDTO.Title,
                    FirstName = passengerDTO.FirstName,
                    LastName = passengerDTO.LastName,
                    IdCardNumber = passengerDTO.IdCardNumber,
                    PhoneNumber = passengerDTO.PhoneNumber,
                    Email = passengerDTO.Email
                });
            }

            // ลดจำนวนที่นั่งว่าง
            price.AvailableSeats -= bookingDTO.Passengers.Count;
            await _context.SaveChangesAsync();

            // โหลดข้อมูลเต็มเพื่อสร้างตั๋ว
            var fullBooking = await GetBookingByReferenceAsync(bookingReference);

            string qrCodeUrl = await GenerateQrCodeAsync(bookingReference);
            string eTicketUrl = await GenerateETicketAsync(fullBooking!);

            return new BookingResponseDTO
            {
                BookingReference = bookingReference,
                Status = booking.BookingStatus,
                TotalAmount = totalAmount,
                QrCodeUrl = qrCodeUrl,
                ETicketUrl = eTicketUrl
            };
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Train)
                .Include(b => b.Schedule).ThenInclude(s => s.OriginStation)
                .Include(b => b.Schedule).ThenInclude(s => s.DestinationStation)
                .Include(b => b.Class)
                .Include(b => b.Passengers)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<Booking?> GetBookingByReferenceAsync(string reference)
        {
            return await _context.Bookings
                .Include(b => b.Schedule).ThenInclude(s => s.Train)
                .Include(b => b.Schedule).ThenInclude(s => s.OriginStation)
                .Include(b => b.Schedule).ThenInclude(s => s.DestinationStation)
                .Include(b => b.Class)
                .Include(b => b.Passengers)
                .FirstOrDefaultAsync(b => b.BookingReference == reference);
        }

        private string GenerateBookingReference()
        {
            return "TRN" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private async Task<string> GenerateQrCodeAsync(string bookingReference)
        {
            var qr = QrCode.EncodeText(bookingReference, QrCode.Ecc.Medium);
            var svg = qr.ToSvgString(4);

            var qrDirectory = Path.Combine(_environment.WebRootPath, "qrcodes");
            Directory.CreateDirectory(qrDirectory);

            var fileName = $"{bookingReference}.svg";
            await File.WriteAllTextAsync(Path.Combine(qrDirectory, fileName), svg);

            return $"/qrcodes/{fileName}";
        }

        private async Task<string> GenerateETicketAsync(Booking booking)
        {
            var ticketDirectory = Path.Combine(_environment.WebRootPath, "tickets");
            Directory.CreateDirectory(ticketDirectory);

            var fileName = $"{booking.BookingReference}.html";
            var passengerRows = string.Join("", booking.Passengers.Select(p =>
                $"<div class='passenger'><strong>{p.Title} {p.FirstName} {p.LastName}</strong></div>"));

            var htmlContent = $@"<!DOCTYPE html>
<html lang='th'>
<head>
    <meta charset='UTF-8'>
    <title>E-Ticket - {booking.BookingReference}</title>
    <style>
        body {{ font-family: 'Sarabun', Arial, sans-serif; padding: 20px; background: #f5f5f5; }}
        .ticket {{ background: #fff; border: 2px solid #0066cc; padding: 30px; max-width: 700px; margin: 0 auto; border-radius: 10px; }}
        .header {{ text-align: center; border-bottom: 3px solid #0066cc; padding-bottom: 20px; margin-bottom: 30px; }}
        .header h1 {{ color: #0066cc; margin: 0; }}
        .info-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .passenger {{ background: #f9f9f9; padding: 12px; margin: 8px 0; border-radius: 5px; }}
        .qr-code {{ text-align: center; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='ticket'>
        <div class='header'>
            <h1>การรถไฟแห่งประเทศไทย</h1>
            <h3>E-Ticket</h3>
        </div>
        <div class='info-row'><span><strong>รหัสการจอง:</strong></span><span>{booking.BookingReference}</span></div>
        <div class='info-row'><span><strong>ขบวน:</strong></span><span>{booking.Schedule?.Train?.TrainName} ({booking.Schedule?.Train?.TrainNumber})</span></div>
        <div class='info-row'><span><strong>ต้นทาง:</strong></span><span>{booking.Schedule?.OriginStation?.StationName}</span></div>
        <div class='info-row'><span><strong>ปลายทาง:</strong></span><span>{booking.Schedule?.DestinationStation?.StationName}</span></div>
        <div class='info-row'><span><strong>วันเดินทาง:</strong></span><span>{booking.TravelDate:dd/MM/yyyy}</span></div>
        <div class='info-row'><span><strong>เวลาออก:</strong></span><span>{booking.Schedule?.DepartureTime:hh\:mm}</span></div>
        <div class='info-row'><span><strong>เวลาถึง:</strong></span><span>{booking.Schedule?.ArrivalTime:hh\:mm}</span></div>
        <div class='info-row'><span><strong>ชั้นโดยสาร:</strong></span><span>{booking.Class?.ClassName}</span></div>
        <div class='info-row'><span><strong>ผู้โดยสาร:</strong></span><span>{booking.NumberOfPassengers} คน</span></div>
        <div class='info-row'><span><strong>ราคารวม:</strong></span><span style='color:#0066cc;font-weight:bold;'>฿{booking.TotalAmount:0.00}</span></div>
        <h3 style='margin-top:24px;'>ข้อมูลผู้โดยสาร</h3>
        {passengerRows}
        <div class='qr-code'>
            <img src='/qrcodes/{booking.BookingReference}.svg' alt='QR Code' style='width:200px;' />
            <p style='color:#666;'>แสดง QR Code นี้ที่สถานี</p>
        </div>
    </div>
</body>
</html>";

            await File.WriteAllTextAsync(Path.Combine(ticketDirectory, fileName), htmlContent);
            return $"/tickets/{fileName}";
        }
    }
}
