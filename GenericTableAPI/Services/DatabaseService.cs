using GenericTableAPI.Models;
using GenericTableAPI.Repositories;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace GenericTableAPI.Services;

public class DatabaseService(DapperRepository repository, IOptions<RetryPolicyOptions> options)
{
    private readonly RetryPolicyOptions _settings = options.Value;

    public Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where, string? orderBy, int? limit, int? offset) => 
        ExecuteAsync(() => repository.GetAllAsync(tableName, where, orderBy, limit, offset));

    public Task<dynamic?> GetByIdAsync(string tableName, string id, string? columnName = "") =>
        ExecuteAsync(() => repository.GetByIdAsync(tableName, id, columnName));

    public Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string? columnName = "") =>
        ExecuteAsync(() => repository.AddAsync(tableName, values, columnName));

    public Task<bool> UpdateAsync(string tableName, string id, IDictionary<string, object?> values, List<object> columns, string? columnName = "") =>
        ExecuteAsync(() => repository.UpdateAsync(tableName, id, values, columns, columnName));
    
    public Task<bool> PatchAsync(string tableName, string id, IDictionary<string, object?> values, string? columnName = "") =>
        ExecuteAsync(() => repository.PatchAsync(tableName, id, values, columnName));

    public Task<bool> DeleteAsync(string tableName, string id, string? columnName = "") =>
        ExecuteAsync(() => repository.DeleteAsync(tableName, id, columnName));
    
    public Task<List<object>?> ExecuteAsync(string procedureName, IEnumerable<StoredProcedureParameter?>? values) =>
        ExecuteAsync(() => repository.ExecuteAsync(procedureName, values));
    
    public Task<List<object>?> GetColumnsAsync(string tableName) =>
        ExecuteAsync(() => repository.GetColumnsAsync(tableName));
    

    private Task<T?> ExecuteAsync<T>(Func<Task<T?>> operation)
    {
        AsyncRetryPolicy<T?>? retryPolicy = Policy<T?>
            .Handle<Exception>()
            .OrResult(r => r is null)
            .WaitAndRetryAsync(
                retryCount: _settings.RetryCount,
                sleepDurationProvider: _ => TimeSpan.FromMilliseconds(_settings.WaitTimeMilliseconds));

        return retryPolicy.ExecuteAsync(operation);
    }
}