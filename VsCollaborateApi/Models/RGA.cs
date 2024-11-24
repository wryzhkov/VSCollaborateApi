using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VsCollaborateApi.Models
{
    public class RGA : IDisposable
    {
        private List<Operation> _operations = new List<Operation>();
        private List<RgaTextBlock> _text = new List<RgaTextBlock>();
        private int _localCount = 0;
        private string _siteId;
        private Random _random = new Random();

        public event EventHandler<Operation>? OnOperationApplied;

        public IEnumerable<Operation> Operations => _operations.Select(x => x).ToArray();

        public IEnumerable<RgaTextBlock> Text => _text.Select(x => x).ToArray();

        public RGA(string siteId)
        {
            _siteId = siteId;
        }

        public RGA()
        {
            _siteId = GenerateSiteId();
        }

        private string GenerateSiteId()
        {
            int length = 14;
            string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var siteId = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());

            return "site-" + siteId;
        }

        private RgaId GenerateId()
        {
            return new RgaId { Id = _localCount++, SiteId = _siteId };
        }

        public void Insert(int position, string text)
        {
            if (position < 0 || position > _text.Count)
            {
                throw new ArgumentException("Position out of bounds");
            }

            var opId = GenerateId();

            var operation = new Operation
            {
                Type = "insert",
                Id = opId,
                Position = position,
                Char = text
            };

            _operations.Add(operation);

            //Apply locally
            _text.Insert(position, new RgaTextBlock { Id = opId.AsString(), Text = text });
            OnOperationApplied?.Invoke(this, operation);
        }

        public void Delete(int position)
        {
            if (position < 0 || position >= _text.Count)
            {
                throw new ArgumentException("Position out of bounds");
            }

            var rgaText = _text[position];

            var operation = new Operation
            {
                Type = "delete",
                Id = RgaId.FromString(rgaText.Id)
            };

            _operations.Add(operation);
            //Apply locally
            _text.RemoveAt(position);
            OnOperationApplied?.Invoke(this, operation);
        }

        public void ReceiveNetworkEvent(Operation operation)
        {
            ApplyOperation(operation);
        }

        private void ApplyOperation(Operation operation)
        {
            if (_operations.Contains(operation))
            {
                return;
            }
            _operations.Add(operation);
            if (operation.Type == Operation.INSERT)
            {
                var opId = operation.Id.AsString();
                var existingIndex = _text.FindIndex((op) => op.Id == opId);
                if (existingIndex < 0)
                {
                    _text.Insert(operation.Position, new RgaTextBlock { Text = operation.Char, Id = opId });
                    OnOperationApplied?.Invoke(this, operation);
                }
            }
            else if (operation.Type == Operation.DELETE)
            {
                var opId = operation.Id.AsString();
                var existingIndex = _text.FindIndex((op) => op.Id == opId);

                if (existingIndex >= 0)
                {
                    _text.RemoveAt(existingIndex);
                    OnOperationApplied?.Invoke(this, operation);
                }
            }
        }

        public string GetText()
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in _text)
            {
                stringBuilder.Append(c.Text.ToString());
            }
            return stringBuilder.ToString();
        }

        public void Dispose()
        {
        }
    }

    public class Operation
    {
        public static readonly string INSERT = "insert";
        public static readonly string DELETE = "delete";

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("id")]
        public RgaId Id { get; set; }

        [JsonPropertyName("position")]
        public int Position { get; set; }

        [JsonPropertyName("char")]
        public string Char { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Operation operation &&
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

    public struct RgaTextBlock
    {
        public string Id { get; set; }
        public string Text { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is RgaTextBlock block &&
                   Id == block.Id &&
                   Text == block.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Text);
        }
    }

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
    }

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