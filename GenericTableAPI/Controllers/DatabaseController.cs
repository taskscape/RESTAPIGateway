using GenericTableAPI.Extensions;
using GenericTableAPI.Models;
using GenericTableAPI.Services;
using GenericTableAPI.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace GenericTableAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class DatabaseController : ControllerBase
    {
        private readonly DatabaseService _service;
        private readonly ILogger<DatabaseController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;

        public DatabaseController(DatabaseService service, ILogger<DatabaseController> logger, IConfiguration configuration, IMemoryCache memoryCache)
        {
            _service = service;
            _logger = logger;
            _configuration = configuration;
            _cache = memoryCache;
        }

        [Route("tables/{tableName}")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll(string tableName, string? where = null, string? orderBy = null, int? limit = null, int? offset = null)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);
        
            if (!TableValidationUtility.CheckTablePermission(_configuration, tableName, "select", User))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with GET-all and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            if(_cache.TryGetCache($"{tableName}-GetAll-{where}-{orderBy}-{limit}-{offset}", out object? cacheResponse))
            {
                _logger.LogInformation("Getting all entities from {TableName} from cache. Timestamp: {TimeStamp}", tableName, timestamp);
                if (cacheResponse == null)
                    return _ = NotFound();
                if(cacheResponse is IEnumerable<dynamic> enumerable && !enumerable.Any())
                    return _ = Ok();
                return _ = Ok(cacheResponse);
            }
        
            try
            {
                _logger.LogInformation("Getting all entities from {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                IEnumerable<dynamic>? entities = await _service.GetAllAsync(tableName, where, orderBy, limit, offset).ConfigureAwait(false);

                _cache.SetCache($"{tableName}-GetAll-{where}-{orderBy}-{limit}-{offset}", entities);

                if (entities == null)
                {
                    _logger.LogInformation("Table {TableName} not found. Timestamp: {TimeStamp}", tableName, timestamp);
                    return responseObj = NotFound();
                }
                if (!entities.Any())
                {
                    _logger.LogInformation("No entities found for {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                    return responseObj = Ok();
                }
        
                _logger.LogInformation("Found {EntitiesCount} entities in {TableName}. Timestamp: {TimeStamp}", entities.Count(), tableName, timestamp);
                return responseObj = Ok(entities);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "An error occurred while getting entities from {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
            }
            finally
            {
                string responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
        }

        [Route("tables/{tableName}/{id}", Name = "GetById")]
        [HttpGet]
        public async Task<ActionResult<dynamic>> GetById(string tableName, [FromRoute] string id, string? primaryKeyColumnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"GET request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);
        
            if (!TableValidationUtility.CheckTablePermission(_configuration, tableName, "select", User))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with GET-ID command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            if (_cache.TryGetCache($"{tableName}-GetById-{id}-{primaryKeyColumnName}", out object? cacheResponse))
            {
                _logger.LogInformation("Getting entity with from table: {TableName} using identifier: {ID} from cache. Timestamp: {TimeStamp}", tableName, id, timestamp);
                if (cacheResponse == null)
                    return responseObj = NotFound();
                if (cacheResponse is IEnumerable<dynamic> enumerable && !enumerable.Any())
                    return responseObj = Ok();
                return responseObj = Ok(cacheResponse);
            }

            _logger.LogInformation("Getting entity with from table: {TableName} using identifier: {ID}. Timestamp: {TimeStamp}", tableName, id, timestamp);
        
            try
            {
                dynamic? entity = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName).ConfigureAwait(false);
                _cache.SetCache($"{tableName}-GetById-{id}-{primaryKeyColumnName}", (object?)entity);

                if (entity == null)
                {
                    _logger.LogInformation("No entity found with identifier={ID} in {TableName}. Request: {requestInfo}", id, tableName, requestInfo);
                    return responseObj = NotFound();
                }
        
                _logger.LogInformation("Found entity with identifier={ID} in {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return responseObj = Ok(entity);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {RequestInfo} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return responseObj = StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from {HttpContext.Request.Path}{HttpContext.Request.QueryString} with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        }

        [Route("tables/{tableName}")]
        [HttpPost]
        public async Task<ActionResult> Add(string tableName, [FromBody] IDictionary<string, object?> values, string? primaryKeyColumnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
            string requestInfo = $"POST request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(valuesDict)}. Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);
        
            if (!TableValidationUtility.CheckTablePermission(_configuration, tableName, "insert", User))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with POST command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }

            _logger.LogInformation("Adding a new entity to table: {TableName} using values: {Values}. Timestamp: {TimeStamp}", tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
        
            try
            {
                _logger.LogInformation("Adding a new entity to {TableName}: {Values}. Timestamp: {TimeStamp}", tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                object? id = await _service.AddAsync(tableName, values, primaryKeyColumnName).ConfigureAwait(false);
                if (id == null)
                {
                    _logger.LogInformation("Failed to establish new entity for table: {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                    return responseObj = StatusCode(StatusCodes.Status500InternalServerError, $"Failed to establish new entity for table: {tableName}");
                }
        
                dynamic? newItem = await _service.GetByIdAsync(tableName, id.ToString() ?? string.Empty, primaryKeyColumnName).ConfigureAwait(false);
                if (newItem == null)
                {
                    _logger.LogInformation("Failed to establish new entity for table: {TableName}. Timestamp: {TimeStamp}", tableName, timestamp);
                    return responseObj = StatusCode(StatusCodes.Status500InternalServerError, $"Failed to establish new entity for table: {tableName}");
                }
                else
                {
                    _logger.LogInformation("Added a new entity for table: {TableName} with identifier: {ID}. Timestamp: {TimeStamp}", tableName, id, timestamp);
                    return responseObj = CreatedAtRoute(nameof(GetById), new { tableName, id, primaryKeyColumnName }, newItem);
                }
        
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return responseObj = StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        }
        
        [Route("tables/{tableName}/{id}")]
        [HttpPatch]
        public async Task<ActionResult> Patch(string tableName, [FromRoute] string id, [FromBody] IDictionary<string, object?> values, string? primaryKeyColumnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
            string requestInfo = $"PATCH request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);
        
            if (!TableValidationUtility.CheckTablePermission(_configuration, tableName, "update", User))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with PATCH command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }
        
            _logger.LogInformation("Updating entity in a table: {TableName} with identifier: {ID} using values: {Values}. Timestamp: {TimeStamp}", tableName, id, JsonConvert.SerializeObject(valuesDict), timestamp);
        
            try
            {
                _logger.LogInformation("Updating entity with identifier={ID} in {TableName}: {Values}. Timestamp: {TimeStamp}", id, tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                await _service.PatchAsync(tableName, id, values, primaryKeyColumnName).ConfigureAwait(false);
                dynamic? updatedItem = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName).ConfigureAwait(false);
                if (updatedItem == null)
                {
                    _logger.LogInformation("No entity found with identifier={ID} in {TableName}. Request: {Request}. Timestamp: {TimeStamp}", id, tableName, requestInfo, timestamp);
                    return responseObj = NotFound();
                }
        
                _logger.LogInformation("Updated entity with identifier={ID} in {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return responseObj = Ok(updatedItem);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return responseObj = StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        }
        
        [Route("tables/{tableName}/{id}")]
        [HttpPut]
        public async Task<ActionResult> Update(string tableName, [FromRoute] string id, [FromBody] IDictionary<string, object?> values, string? primaryKeyColumnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            Dictionary<string, string?> valuesDict = values.ToDictionary(pair => pair.Key, pair => pair.Value?.ToString());
            string requestInfo = $"PUT request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);
        
            if (!TableValidationUtility.CheckTablePermission(_configuration, tableName, "update", User))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with PUT command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }
        
            _logger.LogInformation("Updating entity in a table: {TableName} with identifier: {ID} using values: {Values}. Timestamp: {TimeStamp}", tableName, id, JsonConvert.SerializeObject(valuesDict), timestamp);
        
            try
            {
                _logger.LogInformation("Updating entity with identifier={ID} in {TableName}: {Values}. Timestamp: {TimeStamp}", id, tableName, JsonConvert.SerializeObject(valuesDict), timestamp);
                List<object>? columns =  await _service.GetColumnsAsync(tableName).ConfigureAwait(false);
                await _service.UpdateAsync(tableName, id, values, columns, primaryKeyColumnName).ConfigureAwait(false);
                dynamic? updatedItem = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName).ConfigureAwait(false);
                if (updatedItem == null)
                {
                    _logger.LogInformation("No entity found with identifier={ID} in {TableName}. Request: {Request}. Timestamp: {TimeStamp}", id, tableName, requestInfo, timestamp);
                    return responseObj = NotFound();
                }
        
                _logger.LogInformation("Updated entity with identifier={ID} in {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return responseObj = Ok(updatedItem);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return responseObj = StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        }
        
        [Route("tables/{tableName}/{id}")]
        [HttpDelete]
        public async Task<ActionResult> Delete(string tableName, string id, string? primaryKeyColumnName)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"DELETE request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\". Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);
        
            if (!TableValidationUtility.CheckTablePermission(_configuration, tableName, "delete", User))
            {
                _logger.LogWarning("User {UserName} attempted to access table {TableName} with DELETE command and without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", tableName, timestamp);
                return Forbid();
            }
        
            _logger.LogInformation("Deleting entity from {TableName} using identifier={ID}. Timestamp: {TimeStamp}", tableName, id, timestamp);
        
            try
            {
                dynamic? deletedItem = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName).ConfigureAwait(false);
                if (deletedItem == null)
                {
                    _logger.LogInformation("No entity found with identifier={ID} in {TableName}. Request: {Request}. Timestamp: {TimeStamp}", id, tableName, requestInfo, timestamp);
                    return responseObj = NotFound();
                }

                await _service.DeleteAsync(tableName, id, primaryKeyColumnName).ConfigureAwait(false);
                deletedItem = await _service.GetByIdAsync(tableName, id, primaryKeyColumnName).ConfigureAwait(false);
                if (deletedItem != null)
                {
                    _logger.LogInformation("Failed to delete entity with identifier={ID} from {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                    return responseObj = StatusCode(StatusCodes.Status500InternalServerError);
                }
                
                _logger.LogInformation("Deleted entity with id {id} from {TableName}. Timestamp: {TimeStamp}", id, tableName, timestamp);
                return responseObj = Ok();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return responseObj = StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        }

        /// <summary>
        /// Executes a specified stored procedure. 
        /// </summary>
        /// <param name="procedureName">stored procedure name</param>
        /// <param name="values">stored procedure parameters</param>
        [Route("procedures/{procedureName}")]
        [HttpPost]
        public async Task<ActionResult> ExecuteProcedure(string procedureName, [FromBody] IEnumerable<StoredProcedureParameter?>? values)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            string requestInfo = $"POST request to \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" from \"{HttpContext.Connection.RemoteIpAddress}\" by user \"{User.Identity?.Name ?? "unknown"}\" with values: {JsonConvert.SerializeObject(values)}. Timestamp: {timestamp}";
            dynamic? responseObj = null;
            _logger.LogInformation("{RequestInfo}", requestInfo);

            if (!TableValidationUtility.CheckProcedurePermission(_configuration, procedureName, User))
            {
                _logger.LogWarning("User {UserName} attempted to access procedure {procedureName} without permission. Timestamp: {TimeStamp}", User.Identity?.Name ?? "unknown", procedureName, timestamp);
                return Forbid();
            }

            try
            {
                _logger.LogInformation("Executing stored procedure {ProcedureName} with values: {Values}. Timestamp: {TimeStamp}", procedureName, JsonConvert.SerializeObject(values), timestamp);
                List<object>? result = await _service.ExecuteAsync(procedureName, values);
                if (result == null)
                {
                    _logger.LogInformation("Failed to execute stored procedure: {ProcedureName}. Timestamp: {TimeStamp}", procedureName, timestamp);
                    return responseObj = NotFound();
                }
                
                _logger.LogInformation("Executed stored procedure {ProcedureName}. Timestamp: {TimeStamp}", procedureName, timestamp);
                if (result.Count == 0)
                {
                    return responseObj = NoContent();
                }
                
                return responseObj = Ok(result);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing request: {Request} - {Exception}. Timestamp: {TimeStamp}", requestInfo, exception.Message, timestamp);
                return responseObj = StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
            }
            finally
            {
                string responseInfo = $"Response returned from \"{HttpContext.Request.Path}{HttpContext.Request.QueryString}\" with status code {responseObj?.StatusCode}. Timestamp: {timestamp}";
                 _logger.LogInformation("{ResponseInfo}", responseInfo);
            }
        }
    }
}