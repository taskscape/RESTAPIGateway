using GenericTableAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenericTableAPI.Controllers
{
    [Route("api/dapper/{tableName}")]
    [ApiController]
    public class DapperController : ControllerBase
    {
        //private readonly DapperRepository _repository;
        private readonly DapperService _service;

        public DapperController(DapperService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll(string tableName)
        {
            var entities = await _service.GetAllAsync(tableName);
            return Ok(entities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<dynamic>> GetById(string tableName, [FromRoute] string id)
        {
            dynamic? entity = await _service.GetByIdAsync(tableName, id);

            return entity == null ? NotFound() : (ActionResult<dynamic>)Ok(entity);
        }

        [HttpPost]
        public async Task<ActionResult> Add(string tableName, [FromBody] IDictionary<string, object> values)
        {
            var id = await _service.AddAsync(tableName, values);
            var newItem = await _service.GetByIdAsync(tableName, id.ToString());
            return Ok(newItem);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(string tableName, [FromRoute] string id, [FromBody] IDictionary<string, object> values)
        {
            await _service.UpdateAsync(tableName, id, values);
            var newItem = await _service.GetByIdAsync(tableName, id);
            return Ok(newItem);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string tableName, string id)
        {
            await _service.DeleteAsync(tableName, id);
            return Ok();
        }
    }
}