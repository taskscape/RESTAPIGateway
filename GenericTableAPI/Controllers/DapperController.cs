using GenericTableAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using GenericTableAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GenericTableAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",BasicAuthentication")]
    [Route("api/{tableName}")]
    [ApiController]
    public class DapperController : ControllerBase
    {
        private readonly DapperService _service;
        private readonly ILogger<DapperController> _logger;
        private readonly IConfiguration _configuration;

        public DapperController(DapperService service, ILogger<DapperController> logger, IConfiguration configuration)
        {
            _service = service;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll(string tableName)
        {
            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "select"))
                return Forbid();

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to {HttpContext.Request.Path}{HttpContext.Request.QueryString} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity?.Name ?? "unknown"}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            try
            {
                _logger.LogInformation("Getting all entities from {0}. Timestamp: {1}", tableName, timestamp);
                IEnumerable<dynamic>? entities = await _service.GetAllAsync(tableName);

                IEnumerable<dynamic> enumerable = entities as dynamic[] ?? entities.ToArray();
                if (!enumerable.Any())
                {
                    _logger.LogInformation("No entities found for {0}. Timestamp: {1}", tableName, timestamp);
                    return NoContent();
                }

                _logger.LogInformation("Found {0} entities from {1}. Timestamp: {2}", enumerable.Count(), tableName, timestamp);

                Response.StatusCode = 200;
                return Ok(entities);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while getting entities from {0}. Timestamp: {1}", tableName, timestamp);
                Response.StatusCode = 500;
            }
            finally
            {
                string responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }

            return StatusCode(500, "An error occurred while processing your request");

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetById(string tableName, [FromRoute] string id)
        {
            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "select"))
                return Forbid();

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to {HttpContext.Request.Path}{HttpContext.Request.QueryString} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity?.Name ?? "unknown"}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);
            _logger.LogInformation("Getting entity with id \"{id}\" from \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);

            try
            {
                dynamic? entity = await _service.GetByIdAsync(tableName, id);

                if (entity == null)
                {
                    _logger.LogInformation($"No entity found with id \"{id}\" in \"{tableName}\". Request: {requestInfo}");
                    return NotFound();
                }

                _logger.LogInformation("Found entity with id \"{id}\" in \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);
                return Ok(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while processing request: {requestInfo}. Timestamp: {timestamp}");
                return StatusCode(500, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Add(string tableName, [FromBody] IDictionary<string, object?> values)
        {
            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "insert"))
                return Forbid();

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

            string requestInfo = $"POST request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(valuesDict)}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            try
            {
                _logger.LogInformation("Adding a new entity to \"{tableName}\": {values}. Timestamp: {timestamp}", tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                object? id = await _service.AddAsync(tableName, values);

                if (id == null)
                {
                    _logger.LogInformation("Failed to add a new entity to \"{tableName}\". Timestamp: {timestamp}", tableName, timestamp);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                dynamic? newItem = await _service.GetByIdAsync(tableName, id.ToString());

                _logger.LogInformation("Added a new entity with id \"{id}\" to \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);
                return Ok(newItem);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while processing request: {requestInfo}. Timestamp: {timestamp}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}\" with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string tableName, [FromRoute] string id, [FromBody] IDictionary<string, object?> values)
        {
            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "update"))
                return Forbid();

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

            string requestInfo = $"PUT request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            try
            {
                _logger.LogInformation("Updating entity with id \"{id}\" in \"{tableName}\": \"{values}\". Timestamp: {timestamp}", id, tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                await _service.UpdateAsync(tableName, id, values);

                dynamic? updatedItem = await _service.GetByIdAsync(tableName, id);

                if (updatedItem == null)
                {
                    _logger.LogInformation($"No entity found with id \"{id}\" in \"{tableName}\". Request: {requestInfo}. Timestamp: {timestamp}");
                    return NotFound();
                }

                _logger.LogInformation("Updated entity with id \"{id}\" in \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);
                return Ok(updatedItem);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while processing request: {requestInfo}. Timestamp: {timestamp}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string tableName, string id)
        {
            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "delete"))
                return Forbid();

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to {HttpContext.Request.Path} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity?.Name ?? "unknown."}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            _logger.LogInformation("Deleting entity with id {id} from {tableName}. Timestamp: {timestamp}", id, tableName, timestamp);

            try
            {
                await _service.DeleteAsync(tableName, id);
            }
            catch (Exception exception)
            {
                _logger.LogError($"Error while processing request: {requestInfo} - {exception}. Timestamp: {timestamp}");
                throw;
            }

            dynamic? deletedItem = await _service.GetByIdAsync(tableName, id);

            if (deletedItem != null)
            {
                _logger.LogInformation("Failed to delete entity with id {id} from {tableName}. Timestamp: {timestamp}", id, tableName, timestamp);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Deleted entity with id {id} from {tableName}. Timestamp: {timestamp}", id, tableName, timestamp);
            return Ok();
        }
    }
}