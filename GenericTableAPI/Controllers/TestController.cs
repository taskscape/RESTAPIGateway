using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenericTableAPI.Controllers
{
    [Route("api/test")]
    [ApiController]

    public class TestController
    {
        [HttpGet]
        [Route("no-auth")]
        public Task<ActionResult> Test()
        {
            return Task.FromResult<ActionResult>(new OkResult());
        }
        
        [HttpGet]
        [Route("basic-auth")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",BasicAuthentication")]
        public Task<ActionResult> TestBasicAuth()
        {
            return Task.FromResult<ActionResult>(new OkResult());
        }
    }
}