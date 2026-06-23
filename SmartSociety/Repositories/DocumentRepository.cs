using Dapper;
using SmartSociety.Models;
using System.Data;

namespace SmartSociety.Repositories
{
    public class DocumentRepository : IDocumentRepository
    {
        private readonly IDbConnection _dbConnection;

        public DocumentRepository(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<IEnumerable<SocietyDocument>> GetAllDocumentsAsync()
        {
            return await _dbConnection.QueryAsync<SocietyDocument>(
                "sp_Documents_GetAll",
                commandType: CommandType.StoredProcedure);
        }

        public async Task<SocietyDocument?> GetDocumentByIdAsync(int documentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@DocumentId", documentId);

            return await _dbConnection.QueryFirstOrDefaultAsync<SocietyDocument>(
                "sp_Documents_GetById",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> InsertDocumentAsync(SocietyDocument document)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Title", document.Title);
            parameters.Add("@Category", document.Category);
            parameters.Add("@FilePath", document.FilePath);
            parameters.Add("@IsVisibleToResidents", document.IsVisibleToResidents);

            return await _dbConnection.QuerySingleAsync<int>(
                "sp_Documents_Insert",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateDocumentAsync(SocietyDocument document)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@DocumentId", document.DocumentId);
            parameters.Add("@Title", document.Title);
            parameters.Add("@Category", document.Category);
            parameters.Add("@FilePath", document.FilePath);
            parameters.Add("@IsVisibleToResidents", document.IsVisibleToResidents);

            await _dbConnection.ExecuteAsync(
                "sp_Documents_Update",
                parameters,
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteDocumentAsync(int documentId)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@DocumentId", documentId);

            await _dbConnection.ExecuteAsync(
                "sp_Documents_Delete",
                parameters,
                commandType: CommandType.StoredProcedure);
        }
    }
}
