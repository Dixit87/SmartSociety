using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IAmenityRepository
    {
        Task<IEnumerable<Amenity>> GetAllAmenitiesAsync();
        Task<int> CreateAmenityAsync(Amenity amenity);
        Task UpdateAmenityAsync(Amenity amenity);
        Task DeleteAmenityAsync(int amenityId);

        Task<IEnumerable<AmenityBooking>> GetAllBookingsAsync(string status = null);
        Task<int> CreateBookingAsync(AmenityBooking booking);
        Task UpdateBookingStatusAsync(int bookingId, string status, string remarks = null);
        Task<IEnumerable<AmenityBooking>> GetBookingsByFlatIdAsync(int flatId);
        Task UpdateBookingPaymentStatusAsync(int bookingId, string paymentStatus);
    }
}
