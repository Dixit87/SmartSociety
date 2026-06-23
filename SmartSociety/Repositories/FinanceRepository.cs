using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public class FinanceRepository : IFinanceRepository
    {
        private readonly string _connectionString;

        public FinanceRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ExpenseCategory>(
                "sp_ExpenseCategory_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<Expense>> GetExpensesAsync(int? month = null, int? year = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Expense>(
                "sp_Expense_GetAll",
                new { Month = month, Year = year },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateExpenseAsync(Expense expense)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@CategoryId", expense.CategoryId);
            parameters.Add("@Title", expense.Title);
            parameters.Add("@Amount", expense.Amount);
            parameters.Add("@DateIncurred", expense.DateIncurred);
            parameters.Add("@PaidTo", expense.PaidTo);
            parameters.Add("@PaymentMethod", expense.PaymentMethod);
            parameters.Add("@ReferenceNo", expense.ReferenceNo);
            parameters.Add("@ReceiptUrl", expense.ReceiptUrl);
            parameters.Add("@Notes", expense.Notes);
            parameters.Add("@ExpenseId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_Expense_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@ExpenseId");
        }

        public async Task UpdateExpenseAsync(Expense expense)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ExpenseId", expense.ExpenseId);
            parameters.Add("@CategoryId", expense.CategoryId);
            parameters.Add("@Title", expense.Title);
            parameters.Add("@Amount", expense.Amount);
            parameters.Add("@DateIncurred", expense.DateIncurred);
            parameters.Add("@PaidTo", expense.PaidTo);
            parameters.Add("@PaymentMethod", expense.PaymentMethod);
            parameters.Add("@ReferenceNo", expense.ReferenceNo);
            parameters.Add("@Notes", expense.Notes);

            await connection.ExecuteAsync("sp_Expense_Update", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteExpenseAsync(int expenseId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Expense_Delete",
                new { ExpenseId = expenseId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<OtherIncome>> GetOtherIncomesAsync(int? month = null, int? year = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<OtherIncome>(
                "sp_Income_GetAll",
                new { Month = month, Year = year },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateOtherIncomeAsync(OtherIncome income)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Source", income.Source);
            parameters.Add("@Title", income.Title);
            parameters.Add("@Amount", income.Amount);
            parameters.Add("@DateReceived", income.DateReceived);
            parameters.Add("@ReceivedFrom", income.ReceivedFrom);
            parameters.Add("@PaymentMethod", income.PaymentMethod);
            parameters.Add("@ReferenceNo", income.ReferenceNo);
            parameters.Add("@ReceiptUrl", income.ReceiptUrl);
            parameters.Add("@Notes", income.Notes);
            parameters.Add("@IncomeId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync("sp_Income_Create", parameters, commandType: CommandType.StoredProcedure);
            return parameters.Get<int>("@IncomeId");
        }

        public async Task UpdateOtherIncomeAsync(OtherIncome income)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@IncomeId", income.IncomeId);
            parameters.Add("@Source", income.Source);
            parameters.Add("@Title", income.Title);
            parameters.Add("@Amount", income.Amount);
            parameters.Add("@DateReceived", income.DateReceived);
            parameters.Add("@ReceivedFrom", income.ReceivedFrom);
            parameters.Add("@PaymentMethod", income.PaymentMethod);
            parameters.Add("@ReferenceNo", income.ReferenceNo);
            parameters.Add("@Notes", income.Notes);

            await connection.ExecuteAsync("sp_Income_Update", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteOtherIncomeAsync(int incomeId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_Income_Delete",
                new { IncomeId = incomeId },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<FinanceDashboardStats> GetDashboardStatsAsync(int? month = null, int? year = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<FinanceDashboardStats>(
                "sp_Finance_GetDashboardStats",
                new { Month = month, Year = year },
                commandType: CommandType.StoredProcedure) ?? new FinanceDashboardStats();
        }

        public async Task<IEnumerable<ChartDataPoint>> GetExpenseChartDataAsync(int? month = null, int? year = null)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<ChartDataPoint>(
                "sp_Expense_GetChartData",
                new { Month = month, Year = year },
                commandType: CommandType.StoredProcedure);
        }
    }
}
