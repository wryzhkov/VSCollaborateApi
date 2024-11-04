using System.Text.Json.Serialization;

namespace VsCollaborateApi.Controllers
{
    public class ApiResponse
    {
        public ApiResponse(bool success, object payload)
        {
            Success = success;
            Payload = payload;
        }

        public static ApiResponse Ok(object payload)
        {
            return new ApiResponse(true, payload);
        }

        public static ApiResponse Fail(string error)
        {
            return new ApiResponse(false, new object()) { Error = error };
        }

        public bool Success { get; set; }
        public object? Payload { get; set; }
        public string? Error { get; set; }
    }
}