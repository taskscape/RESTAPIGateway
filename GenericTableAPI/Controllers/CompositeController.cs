using GenericTableAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using GenericTableAPI.Services;

namespace GenericTableAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",BasicAuthentication")]
    [Route("api/composite")]
    [ApiController]
    public class CompositeController : ControllerBase
    {
        private readonly CompositeService _service;
        private readonly ILogger<DapperController> _logger;
        private readonly IConfiguration _configuration;
        public CompositeController(ILogger<DapperController> logger, IConfiguration configuration, CompositeService service)
        {
            _service = service;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CompositeRequestModel values)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"POST request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(values)}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            _service.AuthorizationHeader = HttpContext.Request.Headers.Authorization;
            var result = await _service.RunCompositeRequest(values);

            return StatusCode(result.Code, result.Content);
        }

       
    }
}
