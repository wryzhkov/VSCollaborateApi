using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace VsCollaborateApi.Models.RGA
{
    public class RGA : IDisposable
    {
        private List<RgaOperation> _operations = new List<RgaOperation>();
        private List<RgaTextBlock> _text = new List<RgaTextBlock>() { new RgaTextBlock { Id = "root_0", Text = "", Visible = false } };
        private int _localCount = 0;
        private string _siteId;
        private Random _random = new Random();

        public event EventHandler<RgaOperation>? OnOperationApplied;

        public IEnumerable<RgaOperation> Operations => _operations.Select(x => x).ToArray();

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

        private (int, RgaTextBlock?) CharAtPosition(int position)
        {
            var i = -1;
            var j = -1;
            foreach (var c in _text)
            {
                j++;
                if (c.Visible)
                {
                    if (i++ == position)
                    {
                        return (j, c);
                    }
                }
            }
            return (-1, null);
        }

        public void Insert(int position, string text)
        {
            if (position < 0 || position > _text.Count)
            {
                throw new ArgumentException("Position out of bounds");
            }

            var (index, previousChar) = CharAtPosition(position - 1);

            if (index == -1 || previousChar == null)
            {
                throw new ArgumentException("Position out of bounds");
            }
            var opId = GenerateId();
            var operation = new RgaOperation
            {
                Type = "insert",
                Id = opId,
                Position = RgaId.FromString(previousChar.Id),
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

            var (index, previousChar) = CharAtPosition(position);

            if (index == -1 || previousChar == null)
            {
                throw new ArgumentException("Position out of bounds");
            }
            var rgaText = _text[index];

            var operation = new RgaOperation
            {
                Type = "delete",
                Id = RgaId.FromString(rgaText.Id)
            };

            _operations.Add(operation);
            //Apply locally
            rgaText.Visible = false;
            OnOperationApplied?.Invoke(this, operation);
        }

        public void ReceiveNetworkEvent(RgaOperation operation)
        {
            ApplyOperation(operation);
        }

        private void ApplyOperation(RgaOperation operation)
        {
            if (_operations.Contains(operation))
            {
                return;
            }
            _operations.Add(operation);
            if (operation.Type == RgaOperation.INSERT)
            {
                var opId = operation.Id.AsString();
                var existingIndex = _text.FindIndex((c) => c.Id == opId);
                if (existingIndex < 0)
                {
                    var positionId = operation.Position.AsString();
                    var previousCharPosition = _text.FindIndex(x => x.Id == positionId);
                    _text.Insert(previousCharPosition + 1, new RgaTextBlock { Text = operation.Char, Id = opId, Visible = true });
                    OnOperationApplied?.Invoke(this, operation);
                }
            }
            else if (operation.Type == RgaOperation.DELETE)
            {
                var opId = operation.Id.AsString();
                var existingIndex = _text.FindIndex((op) => op.Id == opId);

                if (existingIndex >= 0)
                {
                    var rgaChar = _text[existingIndex];
                    if (rgaChar.Visible)
                    {
                        rgaChar.Visible = false;
                        OnOperationApplied?.Invoke(this, operation);
                    }
                }
            }
        }

        public string GetText()
        {
            var stringBuilder = new StringBuilder();
            foreach (var c in _text)
            {
                if (c.Visible)
                {
                    stringBuilder.Append(c.Text.ToString());
                }
            }
            return stringBuilder.ToString();
        }

        public void Dispose()
        {
        }
    }
}