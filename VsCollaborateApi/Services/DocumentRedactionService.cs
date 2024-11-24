using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Nodes;
using VsCollaborateApi.Helpers;
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

    public class DocumentEditSession : IDisposable
    {
        public class SessionEndEventArgs
        {
            public Guid Id { get; set; }
        }

        private Guid _documentId;
        private readonly IDatabaseClient _databaseClient;
        private ConcurrentDictionary<User, WebSocketHandler> _users = new();

        private ConcurrentQueue<Message> _userEditEventsQueue = new();

        private ConcurrentQueue<Operation> _operationsQueue = new();
        private ConcurrentQueue<Operation> _incomingOperationsQueue = new();

        private Task _eventReaderTask;
        private Task _operationQueueTask;
        private Task _incomingOperationQueueTask;
        private RGA _replica;
        private bool _initialized;

        private object _lock = new object();

        public DocumentEditSession(Guid documentId, IDatabaseClient databaseClient)
        {
            _documentId = documentId;
            _databaseClient = databaseClient;
            _eventReaderTask = Task.Run(ProcessEventQueue);
            _operationQueueTask = Task.Run(ProcessOperationsQueue);
            _incomingOperationQueueTask = Task.Run(ProcessIncomingOperationsQueue);
        }

        public event EventHandler<SessionEndEventArgs> OnSessionEnd;

        public async Task Initialize()
        {
            if (_initialized)
            {
                return;
            }

            lock (_lock)
            {
                if (_initialized)
                {
                    return;
                }

                _replica = new RGA("SYSTEM");
                var operations = _databaseClient.GetDocumentOperations(_documentId).GetAwaiter().GetResult();
                foreach (var operation in operations)
                {
                    _replica.ReceiveNetworkEvent(operation);
                }

                _replica.OnOperationApplied += _replica_OnOperationApplied;
            }
        }

        private void _replica_OnOperationApplied(object? sender, Operation e)
        {
            _operationsQueue.Enqueue(e);
        }

        public void AddUser(User user, WebSocket webSocket)
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
            var operations = _replica.Operations;
            handler.AddMessage(new Message { Id = "0", Session = "SYSTEM", Type = MessageType.Response, ResponseType = "document", User = "SYSTEM", Data = JsonSerializer.SerializeToDocument(operations) });
        }

        private async Task ProcessEventQueue()
        {
            do
            {
                while (!_userEditEventsQueue.IsEmpty && _userEditEventsQueue.TryDequeue(out Message? eventData))
                {
                    try
                    {
                        if (eventData.Type == MessageType.Event && eventData != null)
                        {
                            var operation = eventData.Data.Deserialize<Operation>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            _incomingOperationsQueue.Enqueue(operation);
                        }
                        foreach (var user in _users)
                        {
                            if (user.Key.SessionId != eventData.Session)
                            {
                                user.Value.AddMessage(eventData);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                await Task.Delay(200);
            } while (true);
        }

        private async Task ProcessOperationsQueue()
        {
            do
            {
                while (!_operationsQueue.IsEmpty && _operationsQueue.TryDequeue(out Operation? operation))
                {
                    var result = await _databaseClient.StoreDocumentOperation(_documentId, operation);
                }
                await Task.Delay(200);
            } while (true);
        }

        private async Task ProcessIncomingOperationsQueue()
        {
            do
            {
                while (!_incomingOperationsQueue.IsEmpty && _incomingOperationsQueue.TryDequeue(out Operation? operation))
                {
                    _replica.ReceiveNetworkEvent(operation);
                }
                await Task.Delay(200);
            } while (true);
        }

        public Task WaitForEnd(User user)
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

                OnSessionEnd?.Invoke(this, new SessionEndEventArgs { Id = _documentId });
            });
        }

        public void Dispose()
        {
            foreach (var d in OnSessionEnd.GetInvocationList())
            {
                OnSessionEnd -= (EventHandler<SessionEndEventArgs>)d;
            }
            if (_replica != null)
            {
                _replica.OnOperationApplied -= _replica_OnOperationApplied;

                _replica.Dispose();
            }
        }
    }
}