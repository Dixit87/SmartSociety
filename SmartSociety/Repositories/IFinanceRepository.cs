using System.Collections.Generic;
using System.Threading.Tasks;
using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IFinanceRepository
    {
        Task<IEnumerable<ExpenseCategory>> GetExpenseCategoriesAsync();
        
        Task<IEnumerable<Expense>> GetExpensesAsync(int? month = null, int? year = null);
        Task<int> CreateExpenseAsync(Expense expense);
        Task UpdateExpenseAsync(Expense expense);
        Task DeleteExpenseAsync(int expenseId);
        
        Task<IEnumerable<OtherIncome>> GetOtherIncomesAsync(int? month = null, int? year = null);
        Task<int> CreateOtherIncomeAsync(OtherIncome income);
        Task UpdateOtherIncomeAsync(OtherIncome income);
        Task DeleteOtherIncomeAsync(int incomeId);
        
        Task<FinanceDashboardStats> GetDashboardStatsAsync(int? month = null, int? year = null);
        Task<IEnumerable<ChartDataPoint>> GetExpenseChartDataAsync(int? month = null, int? year = null);

        // Fixed Deposits
        Task<IEnumerable<FixedDeposit>> GetFixedDepositsAsync();
        Task<int> CreateFixedDepositAsync(FixedDeposit fd);
        Task UpdateFixedDepositStatusAsync(int fdId, string status);

        // Sinking Fund
        Task<IEnumerable<SinkingFundTransaction>> GetSinkingFundTransactionsAsync();
        Task<int> CreateSinkingFundTransactionAsync(SinkingFundTransaction txn);
        Task<decimal> GetSinkingFundBalanceAsync();
    }
}
