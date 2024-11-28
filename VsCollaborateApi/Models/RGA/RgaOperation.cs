using System.Text.Json.Serialization;

namespace VsCollaborateApi.Models.RGA
{
    public class RgaOperation
    {
        public static readonly string INSERT = "insert";
        public static readonly string DELETE = "delete";

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public RgaId Id { get; set; }

        [JsonPropertyName("position")]
        public RgaId Position { get; set; }

        [JsonPropertyName("char")]
        public string Char { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is RgaOperation operation &&
                   Type == operation.Type &&
                   EqualityComparer<RgaId>.Default.Equals(Id, operation.Id) &&
                   Position == operation.Position &&
                   Char == operation.Char;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Id, Position, Char);
        }
    }
}