using TrainBook.API.DTOs;
using TrainBook.API.Models;

namespace TrainBook.API.Services
{
    public interface IBookingService
    {
        Task<BookingResponseDTO> CreateBookingAsync(CreateBookingDTO bookingDTO, int userId);
        Task<List<Booking>> GetUserBookingsAsync(int userId);
        Task<Booking?> GetBookingByReferenceAsync(string reference);
    }
}
