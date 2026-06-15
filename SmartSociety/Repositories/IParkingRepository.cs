using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IParkingRepository
    {
        Task<IEnumerable<ParkingSlot>> GetAllParkingSlotsAsync();
        Task<ParkingSlot> GetParkingSlotByIdAsync(int id);
        Task<int> UpsertParkingSlotAsync(ParkingSlot parkingSlot);
        Task DeleteParkingSlotAsync(int id);
    }
}
