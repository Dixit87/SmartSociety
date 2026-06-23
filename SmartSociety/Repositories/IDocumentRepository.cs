using SmartSociety.Models;

namespace SmartSociety.Repositories
{
    public interface IDocumentRepository
    {
        Task<IEnumerable<SocietyDocument>> GetAllDocumentsAsync();
        Task<SocietyDocument?> GetDocumentByIdAsync(int documentId);
        Task<int> InsertDocumentAsync(SocietyDocument document);
        Task UpdateDocumentAsync(SocietyDocument document);
        Task DeleteDocumentAsync(int documentId);
    }
}
