using System.Net.WebSockets;
using VsCollaborateApi.Helpers;

namespace VsCollaborateApi.Services
{
    public interface IDocumentRedactionService
    {
        DocumentEditSession OpenDocument(Guid id, string user, WebSocket webSocket);
    }
}