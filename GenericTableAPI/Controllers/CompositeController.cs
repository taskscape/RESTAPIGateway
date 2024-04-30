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
        public CompositeController(ILogger<DapperController> logger, CompositeService service)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult> Post([FromBody] CompositeRequest compositeRequest)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            _logger.LogInformation("POST request to \"{Path}\" from \"{RemoteIpAddress}\" by user \"{UserName}\" with values: {Values}. Timestamp: {Timestamp}", HttpContext.Request.Path, HttpContext.Connection.RemoteIpAddress, User.Identity?.Name ?? "unknown", JsonConvert.SerializeObject(compositeRequest), timestamp);

            _service.AuthorizationHeader = HttpContext.Request.Headers.Authorization;
            var result = await _service.RunCompositeRequest(compositeRequest);

            return StatusCode(result.Code, result.Content);
        }


    }
}
