using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using VsCollaborateApi.Helpers;
using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public class DocumentRedactionService : IDocumentRedactionService
    {
        public DocumentRedactionService(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        private ConcurrentDictionary<Guid, DocumentEditSession> _openedDocuments = new ConcurrentDictionary<Guid, DocumentEditSession>();
        private readonly IDocumentService _documentService;

        public DocumentEditSession OpenDocument(Guid id, string user, WebSocket webSocket)
        {
            var session = _openedDocuments.GetOrAdd(id, (id) => new DocumentEditSession(id));
            session.AddUser(user, webSocket);
            //We need to find a way to remove empty sessions to avoid swarming this dictionary with all docs on server
            // not needed now
            return session;
        }
    }

    public class DocumentEditSession
    {
        private Guid _documentId;
        private ConcurrentDictionary<string, WebSocketHandler> _users = new();

        private ConcurrentQueue<EditEventData> _userEditEventsQueue = new();

        private Task _eventReaderTask;

        public DocumentEditSession(Guid documentId)
        {
            _documentId = documentId;
            _eventReaderTask = Task.Run(ProcessEventQueue);
        }

        public void AddUser(string user, WebSocket webSocket)
        {
            var handler = new WebSocketHandler(webSocket, user, _userEditEventsQueue);
            if (_users.ContainsKey(user))
            {
                _users[user].Dispose();
            }
            _users[user] = handler;
            handler.ProcessWebSocket().ContinueWith((t) =>
            {
                _users.TryRemove(user, out _);
                handler.Dispose();
            });
        }

        private async Task ProcessEventQueue()
        {
            do
            {
                while (!_userEditEventsQueue.IsEmpty && _userEditEventsQueue.TryDequeue(out EditEventData? eventData))
                {
                    foreach (var user in _users)
                    {
                        if (user.Key != eventData.UserId)
                        {
                            user.Value.AddMessage(eventData);
                        }
                    }
                }
                await Task.Delay(1000);
            } while (true);
        }

        public Task WaitForEnd(string user)
        {
            return Task.Run(async () =>
            {
                do
                {
                    if (_users.TryGetValue(user, out var handler))
                    {
                        if (!handler.Active)
                        {
                            if (_users.TryRemove(user, out var _))
                            {
                                handler.Dispose();
                            }
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                    await Task.Delay(5000);
                } while (true);
            });
        }
    }
}