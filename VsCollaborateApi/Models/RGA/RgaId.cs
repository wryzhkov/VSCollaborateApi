using System.Text.Json.Serialization;

namespace VsCollaborateApi.Models.RGA
{
    [JsonConverter(typeof(RgaIdJsonConverter))]
    public struct RgaId
    {
        public string SiteId { get; set; }
        public int Id { get; set; }

        internal static RgaId FromString(string id)
        {
            var parts = id.Split("_");
            return new RgaId
            {
                Id = int.Parse(parts[1]),
                SiteId = parts[0],
            };
        }

        public string AsString()
        {
            return $"{SiteId}_{Id}";
        }

        public override bool Equals(object? obj)
        {
            return obj is RgaId id &&
                   SiteId == id.SiteId &&
                   Id == id.Id;
        }

        public static bool operator ==(RgaId a, RgaId b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(RgaId a, RgaId b)
        {
            return !a.Equals(b);
        }
    }
}