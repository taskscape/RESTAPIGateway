using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenericTableAPI.Controllers
{
    [Route("api/test")]
    [ApiController]

    public class TestController
    {
        [HttpGet]
        [Route("api/test")]
        public Task<ActionResult> Test()
        {
            return Task.FromResult<ActionResult>(new OkResult());
        }
        
        [HttpGet]
        [Route("api/test/basic")]
        [Authorize]
        public Task<ActionResult> TestBasicAuth()
        {
            return Task.FromResult<ActionResult>(new OkResult());
        }
    }
}