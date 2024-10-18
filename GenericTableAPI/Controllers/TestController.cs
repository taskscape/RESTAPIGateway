using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenericTableAPI.Controllers
{
    [Route("api/test")]
    [ApiController]

    public class TestController
    {
        [HttpGet]
        [Route("no-authentication")]
        public Task<ActionResult> Test()
        {
            return Task.FromResult<ActionResult>(new OkResult());
        }

        [HttpGet]
        [Route("dynamic-authentication")]
        [Authorize]
        public Task<ActionResult> TestBasicAuth()
        {
            return Task.FromResult<ActionResult>(new OkResult());
        }
    }
}