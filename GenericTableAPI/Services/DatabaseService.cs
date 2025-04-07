using GenericTableAPI.Models;
using GenericTableAPI.Repositories;
namespace GenericTableAPI.Services;

public class DatabaseService(DapperRepository repository)
{
    public Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where, string? orderBy, int? limit, int? offset)
    {
        return repository.GetAllAsync(tableName, where, orderBy, limit, offset);
    }

    public Task<dynamic?> GetByIdAsync(string tableName, string id, string? columnName = "")
    {
        return repository.GetByIdAsync(tableName, id, columnName);
    }

    public Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string? columnName = "")
    {
        return repository.AddAsync(tableName, values, columnName);
    }

    public Task<bool> UpdateAsync(string tableName, string id, IDictionary<string, object?> values, List<object> columns, string? columnName = "")
    {
        return repository.UpdateAsync(tableName, id, values, columns, columnName);
    }
    
    public Task<bool> PatchAsync(string tableName, string id, IDictionary<string, object?> values, string? columnName = "")
    {
        return repository.PatchAsync(tableName, id, values, columnName);
    }

    public Task<bool> DeleteAsync(string tableName, string id, string? columnName = "")
    {
        return repository.DeleteAsync(tableName, id, columnName);
    }
    
    public Task<List<object>?> ExecuteAsync(string procedureName, IEnumerable<StoredProcedureParameter?>? values)
    {
        return repository.ExecuteAsync(procedureName, values);
    }
    
    public Task<List<object>?> GetColumnsAsync(string tableName)
    {
        return repository.GetColumnsAsync(tableName);
    }
}