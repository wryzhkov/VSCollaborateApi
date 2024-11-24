using System.Text.Json;

namespace VsCollaborateApi.Models
{
    public class Message
    {
        public string User { get; set; }
        public string Session { get; set; }
        public string Id { get; set; }
        public string ResponseType { get; set; }
        public MessageType Type { get; set; }

        public JsonDocument Data { get; set; }
    }

    public enum MessageType
    {
        Event = 0,
        Request = 1,
        Response = 2
    }
}