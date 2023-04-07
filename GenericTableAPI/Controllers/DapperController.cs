using GenericTableAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace GenericTableAPI.Controllers
{
    [Authorize]
    [Route("api/dapper/{tableName}")]
    [ApiController]
    public class DapperController : ControllerBase
    {
        private readonly DapperService _service;
        private readonly ILogger<DapperController> _logger;

        public DapperController(DapperService service, ILogger<DapperController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll(string tableName)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var requestInfo = $"GET request to {HttpContext.Request.Path}{HttpContext.Request.QueryString} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity.Name ?? "unknown"}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);
            
            try
            {
                _logger.LogInformation("Getting all entities from {tableName}. Timestamp: {timestamp}", tableName, timestamp);
                var entities = await _service.GetAllAsync(tableName);

                if (!entities.Any())
                {
                    _logger.LogInformation("No entities found for {tableName}. Timestamp: {timestamp}", tableName, timestamp);
                    return NoContent();
                }

                _logger.LogInformation("Found {count} entities from {tableName}. Timestamp: {timestamp}", entities.Count(), tableName, timestamp);
                return Ok(entities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting entities from {tableName}. Timestamp: {timestamp}", tableName, timestamp);
                return StatusCode(500, "An error occurred while processing your request");
            }
            finally
            {
                var responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetById(string tableName, [FromRoute] string id)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var requestInfo = $"GET request to {HttpContext.Request.Path}{HttpContext.Request.QueryString} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity.Name ?? "unknown"}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);
            _logger.LogInformation("Getting entity with id \"{id}\" from \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);

            try
            {
                var entity = await _service.GetByIdAsync(tableName, id);

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
                var responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }


        [HttpPost]
        public async Task<ActionResult> Add(string tableName, [FromBody] IDictionary<string, object?> values)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

            var requestInfo = $"POST request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(valuesDict)}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            try
            {
                _logger.LogInformation("Adding a new entity to \"{tableName}\": {values}. Timestamp: {timestamp}", tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                var id = await _service.AddAsync(tableName, values);

                if (id == null)
                {
                    _logger.LogInformation("Failed to add a new entity to \"{tableName}\". Timestamp: {timestamp}", tableName, timestamp);
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }

                var newItem = await _service.GetByIdAsync(tableName, id.ToString());

                _logger.LogInformation("Added a new entity with id \"{id}\" to \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);
                return Ok(newItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while processing request: {requestInfo}. Timestamp: {timestamp}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                var responseInfo = $"Response returned from \"{HttpContext.Request.Path}\" with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string tableName, [FromRoute] string id, [FromBody] IDictionary<string, object?> values)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

            var requestInfo = $"PUT request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity.Name ?? "unknown"}\". Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            try
            {
                _logger.LogInformation("Updating entity with id \"{id}\" in \"{tableName}\": \"{values}\". Timestamp: {timestamp}", id, tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                await _service.UpdateAsync(tableName, id, values);

                var updatedItem = await _service.GetByIdAsync(tableName, id);

                if (updatedItem == null)
                {
                    _logger.LogInformation($"No entity found with id \"{id}\" in \"{tableName}\". Request: {requestInfo}. Timestamp: {timestamp}");
                    return NotFound();
                }

                _logger.LogInformation("Updated entity with id \"{id}\" in \"{tableName}\". Timestamp: {timestamp}", id, tableName, timestamp);
                return Ok(updatedItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while processing request: {requestInfo}. Timestamp: {timestamp}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string tableName, string id)
        {
            var timestamp = DateTimeOffset.UtcNow;
            var requestInfo = $"GET request to {HttpContext.Request.Path} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity.Name ?? "unknown."}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            _logger.LogInformation("Deleting entity with id {id} from {tableName}. Timestamp: {timestamp}", id, tableName, timestamp);

            try
            {
                await _service.DeleteAsync(tableName, id);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error while processing request: {requestInfo} - {ex}. Timestamp: {timestamp}");
                throw;
            }

            var deletedItem = await _service.GetByIdAsync(tableName, id);

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