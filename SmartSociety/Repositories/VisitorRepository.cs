using System.Data;
using Dapper;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class VisitorRepository : IVisitorRepository
    {
        private readonly IDbConnection _dbConnection;

        public VisitorRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<int> EntryVisitorAsync(Visitor visitor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FullName", visitor.FullName);
            parameters.Add("@PhoneNumber", visitor.PhoneNumber);
            parameters.Add("@VisitorType", visitor.VisitorType);
            parameters.Add("@VehicleNumber", visitor.VehicleNumber);
            parameters.Add("@Purpose", visitor.Purpose);
            parameters.Add("@FlatId", visitor.FlatId);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Visitors_Entry", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task ApproveVisitorAsync(int visitorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VisitorId", visitorId);

            await _dbConnection.ExecuteAsync(
                "sp_Visitors_Approve", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task RejectVisitorAsync(int visitorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VisitorId", visitorId);

            await _dbConnection.ExecuteAsync(
                "sp_Visitors_Reject", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task CheckoutVisitorAsync(int visitorId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@VisitorId", visitorId);

            await _dbConnection.ExecuteAsync(
                "sp_Visitors_Checkout", 
                parameters, 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Visitor>> GetTodayVisitorsAsync()
        {
            return await _dbConnection.QueryAsync<Visitor>(
                "sp_Visitors_GetToday", 
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Visitor>> GetVisitorHistoryAsync(DateTime startDate, DateTime endDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@StartDate", startDate.Date);
            parameters.Add("@EndDate", endDate.Date);

            return await _dbConnection.QueryAsync<Visitor>(
                "sp_Visitors_GetHistory", 
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Visitor>> GetByFlatIdAsync(int flatId)
        {
            string query = @"
                SELECT 
                    v.*, 
                    f.FlatNumber, 
                    b.BlockName 
                FROM Visitors v
                INNER JOIN Flats f ON v.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                WHERE v.FlatId = @FlatId
                ORDER BY v.InTime DESC;";
            return await _dbConnection.QueryAsync<Visitor>(query, new { FlatId = flatId });
        }

        public async Task<int> PreRegisterVisitorAsync(Visitor visitor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FullName", visitor.FullName);
            parameters.Add("@PhoneNumber", visitor.PhoneNumber);
            parameters.Add("@VisitorType", visitor.VisitorType);
            parameters.Add("@VehicleNumber", visitor.VehicleNumber);
            parameters.Add("@Purpose", visitor.Purpose);
            parameters.Add("@FlatId", visitor.FlatId);
            parameters.Add("@InTime", visitor.InTime);
            parameters.Add("@Status", "Pending");

            string query = @"
                INSERT INTO Visitors (FullName, PhoneNumber, VisitorType, VehicleNumber, Purpose, FlatId, InTime, Status)
                VALUES (@FullName, @PhoneNumber, @VisitorType, @VehicleNumber, @Purpose, @FlatId, @InTime, @Status);
                SELECT SCOPE_IDENTITY();";
            return await _dbConnection.QuerySingleAsync<int>(query, parameters);
        }

        public async Task<Visitor?> GetVisitorByIdAsync(int visitorId)
        {
            string query = "SELECT * FROM Visitors WHERE VisitorId = @VisitorId";
            return await _dbConnection.QueryFirstOrDefaultAsync<Visitor>(query, new { VisitorId = visitorId });
        }

        // Invite system implementations
        public async Task<Visitor?> GetVisitorByInviteCodeAsync(string code)
        {
            string query = @"
                SELECT v.*, f.FlatNumber, b.BlockName
                FROM Visitors v
                INNER JOIN Flats f ON v.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                WHERE v.InviteCode = @InviteCode;";
            return await _dbConnection.QueryFirstOrDefaultAsync<Visitor>(query, new { InviteCode = code });
        }

        public async Task<int> PreRegisterVisitorWithInviteAsync(Visitor visitor)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FullName", visitor.FullName);
            parameters.Add("@PhoneNumber", visitor.PhoneNumber);
            parameters.Add("@VisitorType", visitor.VisitorType);
            parameters.Add("@VehicleNumber", visitor.VehicleNumber);
            parameters.Add("@Purpose", visitor.Purpose);
            parameters.Add("@FlatId", visitor.FlatId);
            parameters.Add("@InTime", visitor.InTime);
            parameters.Add("@Status", "Pending");
            parameters.Add("@InviteCode", visitor.InviteCode);
            parameters.Add("@ExpiryDate", visitor.ExpiryDate);

            string query = @"
                INSERT INTO Visitors (FullName, PhoneNumber, VisitorType, VehicleNumber, Purpose, FlatId, InTime, Status, InviteCode, ExpiryDate)
                VALUES (@FullName, @PhoneNumber, @VisitorType, @VehicleNumber, @Purpose, @FlatId, @InTime, @Status, @InviteCode, @ExpiryDate);
                SELECT SCOPE_IDENTITY();";
            return await _dbConnection.QuerySingleAsync<int>(query, parameters);
        }

        // Delivery management implementations
        public async Task<IEnumerable<Delivery>> GetDeliveriesByFlatIdAsync(int flatId)
        {
            string query = @"
                SELECT d.*, f.FlatNumber, b.BlockName
                FROM Deliveries d
                INNER JOIN Flats f ON d.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                WHERE d.FlatId = @FlatId
                ORDER BY d.LoggedAt DESC;";
            return await _dbConnection.QueryAsync<Delivery>(query, new { FlatId = flatId });
        }

        public async Task<IEnumerable<Delivery>> GetTodayDeliveriesAsync()
        {
            string query = @"
                SELECT d.*, f.FlatNumber, b.BlockName, u.FullName AS ResidentName
                FROM Deliveries d
                INNER JOIN Flats f ON d.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                LEFT JOIN Users u ON (f.TenantId = u.UserId OR (f.TenantId IS NULL AND f.OwnerId = u.UserId))
                WHERE CAST(d.LoggedAt AS DATE) = CAST(GETDATE() AS DATE)
                   OR d.Status = 'LoggedAtGate'
                ORDER BY d.LoggedAt DESC;";
            return await _dbConnection.QueryAsync<Delivery>(query);
        }

        public async Task<int> InsertDeliveryAsync(Delivery delivery)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", delivery.FlatId);
            parameters.Add("@Company", delivery.Company);
            parameters.Add("@DeliveryAgentName", delivery.DeliveryAgentName);
            parameters.Add("@DeliveryAgentPhone", delivery.DeliveryAgentPhone);
            parameters.Add("@Status", "LoggedAtGate");
            parameters.Add("@ReceiptPhoto", delivery.ReceiptPhoto);

            string query = @"
                INSERT INTO Deliveries (FlatId, Company, DeliveryAgentName, DeliveryAgentPhone, Status, ReceiptPhoto, LoggedAt)
                VALUES (@FlatId, @Company, @DeliveryAgentName, @DeliveryAgentPhone, @Status, @ReceiptPhoto, GETDATE());
                SELECT SCOPE_IDENTITY();";
            return await _dbConnection.QuerySingleAsync<int>(query, parameters);
        }

        public async Task CollectDeliveryAsync(int deliveryId)
        {
            string query = @"
                UPDATE Deliveries 
                SET Status = 'Collected', CollectedAt = GETDATE()
                WHERE DeliveryId = @DeliveryId;";
            await _dbConnection.ExecuteAsync(query, new { DeliveryId = deliveryId });
        }

        public async Task<Delivery?> GetDeliveryByIdAsync(int deliveryId)
        {
            string query = "SELECT * FROM Deliveries WHERE DeliveryId = @DeliveryId";
            return await _dbConnection.QueryFirstOrDefaultAsync<Delivery>(query, new { DeliveryId = deliveryId });
        }

        // Child Safety implementations
        public async Task<int> InsertChildExitRequestAsync(ChildExitRequest request)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@FlatId", request.FlatId);
            parameters.Add("@FamilyMemberId", request.FamilyMemberId);
            parameters.Add("@Status", "Pending");
            parameters.Add("@GuardRemarks", request.GuardRemarks);

            string query = @"
                INSERT INTO ChildExitRequests (FlatId, FamilyMemberId, Status, GuardRemarks, CreatedAt)
                VALUES (@FlatId, @FamilyMemberId, @Status, @GuardRemarks, GETDATE());
                SELECT SCOPE_IDENTITY();";
            return await _dbConnection.QuerySingleAsync<int>(query, parameters);
        }

        public async Task<IEnumerable<ChildExitRequest>> GetTodayChildExitRequestsAsync()
        {
            string query = @"
                SELECT c.*, fm.FullName AS ChildName, f.FlatNumber, b.BlockName
                FROM ChildExitRequests c
                INNER JOIN FamilyMembers fm ON c.FamilyMemberId = fm.FamilyMemberId
                INNER JOIN Flats f ON c.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                WHERE CAST(c.CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                   OR c.Status = 'Pending'
                ORDER BY c.CreatedAt DESC;";
            return await _dbConnection.QueryAsync<ChildExitRequest>(query);
        }

        public async Task<IEnumerable<ChildExitRequest>> GetChildExitRequestsByFlatIdAsync(int flatId)
        {
            string query = @"
                SELECT c.*, fm.FullName AS ChildName, f.FlatNumber, b.BlockName
                FROM ChildExitRequests c
                INNER JOIN FamilyMembers fm ON c.FamilyMemberId = fm.FamilyMemberId
                INNER JOIN Flats f ON c.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                WHERE c.FlatId = @FlatId
                ORDER BY c.CreatedAt DESC;";
            return await _dbConnection.QueryAsync<ChildExitRequest>(query, new { FlatId = flatId });
        }

        public async Task<ChildExitRequest?> GetChildExitRequestByIdAsync(int requestId)
        {
            string query = @"
                SELECT c.*, fm.FullName AS ChildName, f.FlatNumber, b.BlockName
                FROM ChildExitRequests c
                INNER JOIN FamilyMembers fm ON c.FamilyMemberId = fm.FamilyMemberId
                INNER JOIN Flats f ON c.FlatId = f.FlatId
                INNER JOIN Blocks b ON f.BlockId = b.BlockId
                WHERE c.RequestId = @RequestId;";
            return await _dbConnection.QueryFirstOrDefaultAsync<ChildExitRequest>(query, new { RequestId = requestId });
        }

        public async Task UpdateChildExitRequestStatusAsync(int requestId, string status)
        {
            string query = @"
                UPDATE ChildExitRequests 
                SET Status = @Status, ActionedAt = GETDATE()
                WHERE RequestId = @RequestId;";
            await _dbConnection.ExecuteAsync(query, new { RequestId = requestId, Status = status });
        }
    }
}
