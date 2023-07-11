using GenericTableAPI.Repositories;
namespace GenericTableAPI.Services;

public class DapperService
{
    private readonly DapperRepository _repository;

    public DapperService(DapperRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where, string? orderBy, int? limit)
    {
        return _repository.GetAllAsync(tableName, where, orderBy, limit);
    }

    public Task<dynamic?> GetByIdAsync(string tableName, string id, string primaryKeyColumnName = "")
    {
        return _repository.GetByIdAsync(tableName, id, primaryKeyColumnName);
    }

    public Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string primaryKeyColumnName = "")
    {
        return _repository.AddAsync(tableName, values, primaryKeyColumnName);
    }

    public Task<bool> UpdateAsync(string tableName, string id, IDictionary<string, object?> values, string primaryKeyColumnName = "")
    {
        return _repository.UpdateAsync(tableName, id, values, primaryKeyColumnName);
    }

    public Task<bool> DeleteAsync(string tableName, string id, string primaryKeyColumnName = "")
    {
        return _repository.DeleteAsync(tableName, id, primaryKeyColumnName);
    }
}