using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text.Json.Nodes;
using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public class DocumentRedactionService : IDocumentRedactionService
    {
        public DocumentRedactionService(IDocumentService documentService, IDatabaseClient databaseClient)
        {
            _documentService = documentService;
            _databaseClient = databaseClient;
        }

        private ConcurrentDictionary<Guid, DocumentEditSession> _openedDocuments = new ConcurrentDictionary<Guid, DocumentEditSession>();
        private readonly IDocumentService _documentService;
        private readonly IDatabaseClient _databaseClient;

        public async Task<DocumentEditSession> OpenDocument(Guid id, User user, WebSocket webSocket)
        {
            var session = _openedDocuments.GetOrAdd(id, (id) => new DocumentEditSession(id, _databaseClient));
            await session.Initialize();
            session.AddUser(user, webSocket);
            session.OnSessionEnd += (sender, args) =>
            {
                if (_openedDocuments.TryRemove(args.Id, out var removedSession))
                {
                    if (removedSession != session)
                    {
                        _openedDocuments.TryAdd(args.Id, removedSession);
                    }
                    else
                    {
                        removedSession.Dispose();
                    }
                };
            };

            return session;
        }
    }
}