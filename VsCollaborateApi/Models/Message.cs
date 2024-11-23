using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace VsCollaborateApi.Models
{
    public class Message
    {
        public string User { get; set; }
        public string Session { get; set; }
        public string Id { get; set; }
        public MessageType Type { get; set; }

        public JsonObject Data { get; set; }
    }

    public enum MessageType
    {
        Event = 0,
        Request = 1
    }
}