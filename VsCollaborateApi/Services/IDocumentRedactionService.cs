using System.Net.WebSockets;
using VsCollaborateApi.Helpers;
using VsCollaborateApi.Models;

namespace VsCollaborateApi.Services
{
    public interface IDocumentRedactionService
    {
        Task<DocumentEditSession> OpenDocument(Guid id, User user, WebSocket webSocket);
    }
}