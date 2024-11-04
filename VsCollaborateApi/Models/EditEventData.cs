using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace VsCollaborateApi.Models
{
    public class EditEventData
    {
        public string UserId { get; set; }
        public string Id { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonObject> Data { get; set; }
    }
}