using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public interface IDatabaseClient
    {
        Task<bool> CreateDocument(Document document);

        Task DeleteDocumentAsync(Guid id);

        Task<List<Document>> ListDocumentsAsync();

        Task<Document?> FindDocumentsAsync(Guid id);

        Task UpdateDocumentAsync(Document document);
    }
}