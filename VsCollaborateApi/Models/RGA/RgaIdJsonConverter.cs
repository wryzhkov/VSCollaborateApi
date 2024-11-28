using System.Text.Json;
using System.Text.Json.Serialization;

namespace VsCollaborateApi.Models.RGA
{
    public class RgaIdJsonConverter : JsonConverter<RgaId>
    {
        public override RgaId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var rgaid = new RgaId();
            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException("Cannot parse RgaId");
            }
            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Cannot parse RgaId");
            }

            rgaid.SiteId = reader.GetString();
            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException("Cannot parse RgaId");
            }

            rgaid.Id = reader.GetInt32();
            reader.Read();
            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException("Cannot parse RgaId");
            }
            return rgaid;
        }

        public override void Write(Utf8JsonWriter writer, RgaId value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteStringValue(value.SiteId);
            writer.WriteNumberValue(value.Id);
            writer.WriteEndArray();
        }
    }
}