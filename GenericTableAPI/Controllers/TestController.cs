using GenericTableAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GenericTableAPI.Controllers
{
    [Route("api/test")]
    [ApiController]

    public class TestController : Controller
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

        [Authorize]
        [Route("GetUserRoles")]
        [HttpGet]
        public ActionResult GetUserRoles()
        {
            return Ok(TableValidationUtility.GetUserRoles(User));
        }
    }
}