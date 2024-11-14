using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public interface IDatabaseClient
    {
        Task<bool> CreateDocument(Document document);

        Task DeleteDocumentAsync(Guid id);

        Task<List<Document>> ListDocumentsAsync();

        Task<Document?> FindDocumentAsync(Guid id);

        Task UpdateDocumentAsync(Document document);

        Task<User?> FindUserAsync(string email);

        Task<bool> CreateUser(User user, string passwordHash);

        Task<bool> CheckPassword(string email, string passwordHash);

        Task<string> GetPassword(User user);
    }
}