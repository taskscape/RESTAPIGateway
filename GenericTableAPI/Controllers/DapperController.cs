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
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll(string tableName, string? where = null, string? orderBy = null, int? limit = null)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to {HttpContext.Request.Path}{HttpContext.Request.QueryString} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity?.Name ?? "unknown"}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "select"))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with GET-all and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            try
            {
                _logger.LogInformation("Getting all entities from {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                IEnumerable<dynamic>? entities = await _service.GetAllAsync(tableName, where, orderBy, limit);


                if (entities == null)
                {
                    _logger.LogInformation("No entities found for {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                    return NoContent();
                }
                else
                {
                    if (!entities.Any())
                    {
                        _logger.LogInformation("No entities found for {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                        return NoContent();
                    }
                }

                _logger.LogInformation("Found {EntitiesCount} entities in {TableName}. Timestamp: {TimeStamp}", entities.Count(), tableName, timestamp);

                Response.StatusCode = 200;
                return Ok(entities);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while getting entities from {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
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
        public async Task<ActionResult<dynamic>> GetById(string tableName, [FromRoute] string id, string? primaryKeyColumnName)
        {

            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to " + HttpContext.Request.Path + HttpContext.Request.QueryString + "from " + HttpContext.Connection.RemoteIpAddress + " by user " + User.Identity?.Name ?? "unknown" + ". Timestamp: " + timestamp;
            _logger.LogInformation(requestInfo);

            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "select"))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with GET-ID command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            _logger.LogInformation("Getting entity with from table: {TableName} using identifier: {ID}. Timestamp: {TimeStamp}", tableName, id, timestamp);

            try
            {
                dynamic? entity = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName);

                if (entity == null)
                {
                    _logger.LogInformation("No entity found with identifier={ID} in {TableName}. Request: {requestInfo}", id, tableName, requestInfo);
                    return NotFound();
                }

                _logger.LogInformation("Found entity with identifier={ID} in {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return Ok(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing request: {RequestInfo}. Timestamp: {TimeStamp}", requestInfo, timestamp);
                return StatusCode(500, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        [HttpPost]
        public async Task<ActionResult> Add(string tableName, [FromBody] IDictionary<string, object?> values, string? columnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
            string requestInfo = $"POST request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(valuesDict)}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "insert"))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with POST command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();

            }

            _logger.LogInformation("Adding a new entity to table: {TableName} using values: {Values}. Timestamp: {TimeStamp}", tableName, JsonConvert.SerializeObject(valuesDict), timestamp);

            try
            {
                _logger.LogInformation("Adding a new entity to {TableName}: {Values}. Timestamp: {TimeStamp}", tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                object? id = await _service.AddAsync(tableName, values, columnName);

                if (id == null)
                {
                    _logger.LogInformation("Failed to establish new entity for table: {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                    return Ok();
                }

                dynamic? newItem = await _service.GetByIdAsync(tableName, id.ToString() ?? string.Empty, columnName);

                if (newItem == null)
                {
                    _logger.LogInformation("Failed to establish new entity for table: {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                    return Ok();
                }
                else ;

                {
                    _logger.LogInformation("Added a new entity for table: {TableName} with identifier: {ID}. Timestamp: {TimeStamp}", tableName, id, timestamp);
                    return CreatedAtAction(nameof(GetById), new { id = id.ToString() }, newItem);
                }

            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request}. Timestamp: {TimeStamp}", requestInfo, timestamp);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}\" with status code {Response.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation(responseInfo);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string tableName, [FromRoute] string id, [FromBody] IDictionary<string, object?> values, string? columnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());

            string requestInfo = $"PUT request to \"{HttpContext.Request.Path}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "update"))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with PUT command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            _logger.LogInformation("Updating entity in a table: {TableName} with identifier: {ID} using values: {Values}. Timestamp: {TimeStamp}", tableName, id, JsonConvert.SerializeObject(valuesDict), timestamp);

            try
            {
                _logger.LogInformation("Updating entity with identifier={ID} in {TableName}: {Values}. Timestamp: {TimeStamp}", id, tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                await _service.UpdateAsync(tableName, id, values, columnName);

                dynamic? updatedItem = await _service.GetByIdAsync(tableName, id, columnName);

                if (updatedItem == null)
                {
                    _logger.LogInformation("No entity found with identifier={ID} in {TableName}. Request: {Request}. Timestamp: {TimeStamp}", id, tableName, requestInfo, timestamp);
                    return NotFound();
                }

                _logger.LogInformation("Updated entity with identifier={ID} in {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return Ok(updatedItem);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request}. Timestamp: {TimeStamp}", requestInfo, timestamp);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string tableName, string id, string? primaryKeyColumnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to {HttpContext.Request.Path} from {HttpContext.Connection.RemoteIpAddress} by user {User.Identity?.Name ?? "unknown."}. Timestamp: {timestamp}";
            _logger.LogInformation(requestInfo);

            if (!TableValidationUtility.ValidTablePermission(_configuration, tableName, "delete"))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with DELETE command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            _logger.LogInformation("Deleting entity from {TableName} using identifier={ID}. Timestamp: {TimeStamp}", tableName, id, timestamp);

            try
            {
                await _service.DeleteAsync(tableName, id, primaryKeyColumnName);
            }
            catch (Exception exception)
            {
                _logger.LogError("Error while processing request: {Request} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }

            dynamic? deletedItem = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName);

            if (deletedItem != null)
            {
                _logger.LogInformation("Failed to delete entity with identifier={ID} from {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            _logger.LogInformation("Deleted entity with id {id} from {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
            return Ok();
        }
    }
}