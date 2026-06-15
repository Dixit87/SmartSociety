using System.Data;
using Dapper;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class ParkingRepository : IParkingRepository
    {
        private readonly IDbConnection _dbConnection;

        public ParkingRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<ParkingSlot>> GetAllParkingSlotsAsync()
        {
            return await _dbConnection.QueryAsync<ParkingSlot>(
                "sp_ParkingSlots_GetAll", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<ParkingSlot> GetParkingSlotByIdAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SlotId", id);

            return await _dbConnection.QuerySingleOrDefaultAsync<ParkingSlot>(
                "sp_ParkingSlots_GetById", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> UpsertParkingSlotAsync(ParkingSlot parkingSlot)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SlotId", parkingSlot.SlotId);
            parameters.Add("@SlotNumber", parkingSlot.SlotNumber);
            parameters.Add("@VehicleType", parkingSlot.VehicleType);
            parameters.Add("@FlatId", parkingSlot.FlatId);
            parameters.Add("@VehicleNumber", parkingSlot.VehicleNumber);
            parameters.Add("@VehicleMakeModel", parkingSlot.VehicleMakeModel);
            parameters.Add("@StickerNumber", parkingSlot.StickerNumber);
            parameters.Add("@IsVisitorSlot", parkingSlot.IsVisitorSlot);
            parameters.Add("@IsActive", parkingSlot.IsActive);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_ParkingSlots_Upsert", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteParkingSlotAsync(int id)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@SlotId", id);

            await _dbConnection.ExecuteAsync(
                "sp_ParkingSlots_Delete", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }
    }
}
