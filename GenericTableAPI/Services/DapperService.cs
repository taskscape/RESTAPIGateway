using GenericTableAPI.Repositories;

namespace GenericTableAPI.Services;

public class DapperService
{
    private readonly DapperRepository _repository;

    public DapperService(DapperRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<dynamic>> GetAllAsync(string tableName)
    {
        return _repository.GetAllAsync(tableName);
    }

    public Task<dynamic?> GetByIdAsync(string tableName, string id)
    {
        return _repository.GetByIdAsync(tableName, id);
    }

    public Task<object?> AddAsync(string tableName, IDictionary<string, object?> values)
    {
        return _repository.AddAsync(tableName, values);
    }

    public Task UpdateAsync(string tableName, string id, IDictionary<string, object?> values)
    {
        return _repository.UpdateAsync(tableName, id, values);
    }

    public Task DeleteAsync(string tableName, string id)
    {
        return _repository.DeleteAsync(tableName, id);
    }
}