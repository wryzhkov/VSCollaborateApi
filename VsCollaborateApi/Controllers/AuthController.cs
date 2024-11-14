using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VsCollaborateApi.Models;
using VsCollaborateApi.Services;

namespace VsCollaborateApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost("sign-up")]
        public async Task<ActionResult<ApiResponse>> SignUp([FromBody] RegisterParams registerParams)
        {
            try
            {
                var user = await _identityService.CreateUserAsync(new User(registerParams.Email, registerParams.Name), registerParams.Password);
                return Ok(ApiResponse.Ok(user));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("auth")]
        public async Task<ActionResult<ApiResponse>> Auth([FromBody] LoginParams loginParams)
        {
            try
            {
                var token = await _identityService.LoginAsync(loginParams.Email, loginParams.Password);

                return Ok(ApiResponse.Ok(new AuthToken { Token = token }));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<ApiResponse>> RefreshToken()
        {
            try
            {
                var user = await _identityService.AuthenticateAsync(HttpContext);
                var newToken = _identityService.RefreshToken(user);
                return Ok(ApiResponse.Ok(new AuthToken { Token = newToken }));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse.Fail(ex.Message));
            }
        }
    }

    public class RegisterParams
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string Password { get; set; }
    }

    public class LoginParams
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AuthToken
    {
        public string Token { get; set; }
    }
}