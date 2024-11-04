using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public interface IDocumentService
    {
        Task<Document> CreateDocumentAsync(string name, string owner);

        Task<Document?> GetDocumentAsync(string id);

        Task<List<Document>> ListDocumentsAsync();
    }
}