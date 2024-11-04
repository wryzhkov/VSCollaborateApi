using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using VsCollaborateApi.Models;

namespace VsCollaborateApi.Helpers
{
    public class WebSocketHandler : IDisposable
    {
        private const int BUFFER_SIZE = 4 * 1024;
        private const int QUEUE_SLEEP_MS = 5000;

        private readonly ConcurrentQueue<EditEventData> _messageQueue = new ConcurrentQueue<EditEventData>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private WebSocket _webSocket;
        private readonly string _user;
        private ConcurrentQueue<EditEventData> _editEventsQueue;

        public bool Active => !_cts.IsCancellationRequested;

        public WebSocketHandler(WebSocket webSocket, string user, ConcurrentQueue<EditEventData> editEvents)
        {
            _webSocket = webSocket;
            _user = user;
            _editEventsQueue = editEvents;
        }

        public async Task ProcessWebSocket()
        {   // Start the two tasks
            Task receiveTask = Task.Run(() => ReadMessagesAsync(_cts.Token));
            Task sendTask = Task.Run(() => WriteMessagesAsync(_cts.Token));

            // Wait for both tasks to complete (they won't unless cancelled)
            await Task.WhenAll(receiveTask, sendTask);
        }

        private async Task ReadMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[BUFFER_SIZE];
            var message = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    WebSocketReceiveResult? result = null;
                    do
                    {
                        result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Console.WriteLine("WebSocket closed by the server.");
                            _cts.Cancel(); // Signal to stop the tasks
                            break;
                        }

                        message.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage && !cancellationToken.IsCancellationRequested);
                    var renderedMessage = message.ToString();
                    var editEvent = JsonSerializer.Deserialize<EditEventData>(renderedMessage);
                    editEvent.UserId = _user;

                    _editEventsQueue.Enqueue(editEvent);
                    Console.WriteLine($"Received message: {renderedMessage}");
                    message.Clear();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error receiving message: {ex.Message}");
                    _cts.Cancel(); // Signal to stop the tasks
                    break;
                }
            }
        }

        private async Task WriteMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[BUFFER_SIZE];

            while (!cancellationToken.IsCancellationRequested)
            {
                while (!_messageQueue.IsEmpty && _messageQueue.TryDequeue(out EditEventData? message))
                {
                    try
                    {
                        var jsonMessage = JsonSerializer.Serialize(message);
                        var encodedMessage = Encoding.UTF8.GetBytes(jsonMessage);
                        await _webSocket.SendAsync(new ArraySegment<byte>(encodedMessage), WebSocketMessageType.Text, true, cancellationToken);
                        Console.WriteLine($"Sent message: {jsonMessage}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending message: {ex.Message}");
                        _cts.Cancel(); // Signal to stop the tasks
                        break;
                    }
                }
                await Task.Delay(QUEUE_SLEEP_MS);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _webSocket.Dispose();
        }

        public void AddMessage(EditEventData message)
        {
            _messageQueue.Enqueue(message);
        }
    }
}