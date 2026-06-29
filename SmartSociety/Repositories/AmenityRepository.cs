using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class AmenityRepository : IAmenityRepository
    {
        private readonly string _connectionString;

        public AmenityRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<Amenity>> GetAllAmenitiesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Amenity>(
                "sp_Amenity_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateAmenityAsync(Amenity amenity)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Name", amenity.Name);
            parameters.Add("@Description", amenity.Description);
            parameters.Add("@Capacity", amenity.Capacity);
            parameters.Add("@OpenTime", amenity.OpenTime);
            parameters.Add("@CloseTime", amenity.CloseTime);
            parameters.Add("@PricePerHour", amenity.PricePerHour);
            parameters.Add("@IsActive", amenity.IsActive);
            parameters.Add("@AmenityId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_Amenity_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@AmenityId");
        }

        public async Task UpdateAmenityAsync(Amenity amenity)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AmenityId", amenity.AmenityId);
            parameters.Add("@Name", amenity.Name);
            parameters.Add("@Description", amenity.Description);
            parameters.Add("@Capacity", amenity.Capacity);
            parameters.Add("@OpenTime", amenity.OpenTime);
            parameters.Add("@CloseTime", amenity.CloseTime);
            parameters.Add("@PricePerHour", amenity.PricePerHour);
            parameters.Add("@IsActive", amenity.IsActive);

            await connection.ExecuteAsync("sp_Amenity_Update", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteAmenityAsync(int amenityId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Amenity_Delete",
                new { AmenityId = amenityId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<AmenityBooking>> GetAllBookingsAsync(string status = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<AmenityBooking>(
                "sp_Booking_GetAll",
                new { Status = status },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateBookingAsync(AmenityBooking booking)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AmenityId", booking.AmenityId);
            parameters.Add("@FlatId", booking.FlatId);
            parameters.Add("@UserId", booking.UserId);
            parameters.Add("@BookingDate", booking.BookingDate);
            parameters.Add("@StartTime", booking.StartTime);
            parameters.Add("@EndTime", booking.EndTime);
            parameters.Add("@Purpose", booking.Purpose);
            parameters.Add("@TotalAmount", booking.TotalAmount);
            parameters.Add("@PaymentStatus", booking.PaymentStatus);
            parameters.Add("@Status", booking.Status);
            parameters.Add("@BookingId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_Booking_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@BookingId");
        }

        public async Task UpdateBookingStatusAsync(int bookingId, string status, string remarks = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Booking_UpdateStatus",
                new { BookingId = bookingId, Status = status, Remarks = remarks },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<AmenityBooking>> GetBookingsByFlatIdAsync(int flatId)
        {
            using var connection = new SqlConnection(_connectionString);
            string query = @"
                SELECT 
                    b.BookingId, b.AmenityId, b.FlatId, b.UserId, b.BookingDate, 
                    b.StartTime, b.EndTime, b.Purpose, b.Status, b.Remarks, b.TotalAmount, b.PaymentStatus, b.CreatedAt,
                    a.Name AS AmenityName,
                    f.FlatNumber AS FlatNo,
                    u.FullName AS ResidentName,
                    u.PhoneNumber AS ResidentPhone
                FROM AmenityBookings b
                INNER JOIN Amenities a ON b.AmenityId = a.AmenityId
                INNER JOIN Flats f ON b.FlatId = f.FlatId
                INNER JOIN Users u ON b.UserId = u.UserId
                WHERE b.FlatId = @FlatId
                ORDER BY b.BookingDate DESC, b.StartTime DESC;";
            return await connection.QueryAsync<AmenityBooking>(query, new { FlatId = flatId });
        }

        public async Task UpdateBookingPaymentStatusAsync(int bookingId, string paymentStatus)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE AmenityBookings SET PaymentStatus = @PaymentStatus WHERE BookingId = @BookingId",
                new { BookingId = bookingId, PaymentStatus = paymentStatus });
        }
    }
}
