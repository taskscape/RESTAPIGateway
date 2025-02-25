using GenericTableAPI.Models;
using GenericTableAPI.Repositories;
using Polly;
using Polly.Retry;

namespace GenericTableAPI.Services
{
    public class DatabaseService
    {
        private readonly DapperRepository _repository;
        private readonly ILogger<DatabaseService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public DatabaseService(DapperRepository repository, ILogger<DatabaseService> logger, IConfiguration configuration)
        {
            int retryCount;
            if (!int.TryParse(configuration.GetSection("RetryPolicy:RetryCount").Value, out retryCount))
                retryCount = 0;

            int retryWait;
            if (!int.TryParse(configuration.GetSection("RetryPolicy:WaitDurationSeconds").Value, out retryWait))
                retryWait = 3;

            _repository = repository;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryWait),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {retryCount} due to exception: {exceptionMessage}. Waiting for {timeSpanTotalSeconds} seconds.", retryCount, exception.Message, timeSpan.TotalSeconds);
                    });
        }

        public async Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where, string? orderBy, int? limit)
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.GetAllAsync(tableName, where, orderBy, limit));
        }

        public async Task<dynamic?> GetByIdAsync(string tableName, string id, string? columnName = "")
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.GetByIdAsync(tableName, id, columnName));
        }

        public async Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string? columnName = "")
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.AddAsync(tableName, values, columnName));
        }

        public async Task<bool> UpdateAsync(string tableName, string id, IDictionary<string, object?> values, List<object> columns, string? columnName = "")
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.UpdateAsync(tableName, id, values, columns, columnName));
        }

        public async Task<bool> PatchAsync(string tableName, string id, IDictionary<string, object?> values, string? columnName = "")
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.PatchAsync(tableName, id, values, columnName));
        }

        public async Task<bool> DeleteAsync(string tableName, string id, string? columnName = "")
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.DeleteAsync(tableName, id, columnName));
        }

        public async Task<List<object>?> ExecuteAsync(string procedureName, IEnumerable<StoredProcedureParameter?> values)
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.ExecuteAsync(procedureName, values));
        }

        public async Task<List<object>?> GetColumnsAsync(string tableName)
        {
            return await _retryPolicy.ExecuteAsync(() => _repository.GetColumnsAsync(tableName));
        }
    }
}