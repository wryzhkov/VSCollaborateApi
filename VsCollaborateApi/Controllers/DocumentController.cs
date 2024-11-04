using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using VsCollaborateApi.Helpers;
using VsCollaborateApi.Models;
using VsCollaborateApi.Services;

namespace VsCollaborateApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentRedactionService _documentRedactionService;
        private readonly IIdentityService _identityService;

        public DocumentController(IDocumentService documentService, IDocumentRedactionService documentRedactionService, IIdentityService identityService)
        {
            _documentService = documentService;
            _documentRedactionService = documentRedactionService;
            _identityService = identityService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Document>>> ListDocuments([FromQuery] string? name, [FromQuery] string? owner)
        {
            var docs = await _documentService.ListDocumentsAsync();
            return Ok(docs);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateDocument([FromBody] CreateDocumentRequest createDocumentRequest)
        {
            var user = _identityService.Authenticate(HttpContext);

            if (string.IsNullOrEmpty(createDocumentRequest.Name))
            {
                createDocumentRequest.Name = "Unnnamed document";
            }
            var document = await _documentService.CreateDocumentAsync(createDocumentRequest.Name, user.Email);
            return Ok(ApiResponse.Ok(document));
        }

        [HttpGet("{id}/open")]
        public async Task<ActionResult<ApiResponse>> OpenDocument([FromRoute] string id)
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                return BadRequest(ApiResponse.Fail("Cannot open a document over HTTP. Please, use WebSocket api instead"));
            }
            var user = _identityService.Authenticate(HttpContext);
            var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            var document = await _documentService.GetDocumentAsync(id);
            if (document == null)
            {
                return NotFound(ApiResponse.Fail("Document not found"));
            }
            var session = _documentRedactionService.OpenDocument(document.Id, user.Email, webSocket);
            await session.WaitForEnd(user.Email);
            return Ok(ApiResponse.Ok("Edit session closed..."));
        }
    }

    public class CreateDocumentRequest
    {
        public string? Name { get; set; }
    }
}