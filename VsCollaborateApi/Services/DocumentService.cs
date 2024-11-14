using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IDatabaseClient _databaseClient;

        public DocumentService(IDatabaseClient databaseClient)
        {
            _databaseClient = databaseClient;
        }

        public async Task<List<Document>> ListDocumentsAsync()
        {
            return await _databaseClient.ListDocumentsAsync();
        }

        public async Task<Document?> GetDocumentAsync(string id)
        {
            return await _databaseClient.FindDocumentAsync(Guid.Parse(id));
        }

        public async Task<Document> CreateDocumentAsync(string name, string owner)
        {
            var doc = new Document(Guid.NewGuid(), name, owner);

            var result = await _databaseClient.CreateDocument(doc);

            return doc;
        }
    }
}