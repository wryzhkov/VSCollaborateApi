using Microsoft.AspNetCore.Mvc.Diagnostics;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using VsCollaborateApi.Helpers;
using VsCollaborateApi.Models;
using VsCollaborateApi.Models.RGA;

namespace VsCollaborateApi.Services
{
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

        private ConcurrentQueue<RgaOperation> _operationsQueue = new();
        private ConcurrentQueue<Message> _incomingOperationsQueue = new();

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
                Console.WriteLine("RGA initial state: " + _replica.GetText());

                _replica.OnOperationApplied += _replica_OnOperationApplied;
                _initialized = true;
            }
        }

        private void _replica_OnOperationApplied(object? sender, RgaOperation e)
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
                    if (eventData.Type == MessageType.Event && eventData != null)
                    {
                        _incomingOperationsQueue.Enqueue(eventData);
                    }
                    foreach (var user in _users)
                    {
                        if (user.Key.SessionId != eventData.Session)
                        {
                            user.Value.AddMessage(eventData);
                        }
                    }
                }
                await Task.Delay(200);
            } while (true);
        }

        private async Task ProcessOperationsQueue()
        {
            do
            {
                while (!_operationsQueue.IsEmpty && _operationsQueue.TryDequeue(out RgaOperation? operation))
                {
                    try
                    {
                        var result = await _databaseClient.StoreDocumentOperation(_documentId, operation);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception during storing operation: " + ex);
                        _operationsQueue.Enqueue(operation);
                    }
                }
                await Task.Delay(200);
            } while (true);
        }

        private async Task ProcessIncomingOperationsQueue()
        {
            do
            {
                while (!_incomingOperationsQueue.IsEmpty && _incomingOperationsQueue.TryDequeue(out Message? message))
                {
                    try
                    {
                        var operation = message.Data.Deserialize<RgaOperation>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (operation != null && operation.Type != null)
                        {
                            _replica.ReceiveNetworkEvent(operation);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex); ;
                    }
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