﻿using GenericTableAPI.Models;
using GenericTableAPI.Repositories;
namespace GenericTableAPI.Services;

public class DatabaseService
{
    private readonly DapperRepository _repository;

    public DatabaseService(DapperRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<dynamic>?> GetAllAsync(string tableName, string? where, string? orderBy, int? limit)
    {
        return _repository.GetAllAsync(tableName, where, orderBy, limit);
    }

    public Task<dynamic?> GetByIdAsync(string tableName, string id, string? columnName = "")
    {
        return _repository.GetByIdAsync(tableName, id, columnName);
    }

    public Task<object?> AddAsync(string tableName, IDictionary<string, object?> values, string? columnName = "")
    {
        return _repository.AddAsync(tableName, values, columnName);
    }

    public Task<bool> UpdateAsync(string tableName, string id, IDictionary<string, object?> values, string? columnName = "")
    {
        return _repository.UpdateAsync(tableName, id, values, columnName);
    }

    public Task<bool> DeleteAsync(string tableName, string id, string? columnName = "")
    {
        return _repository.DeleteAsync(tableName, id, columnName);
    }
    
    public Task<List<object>?> ExecuteAsync(string procedureName, IEnumerable<StoredProcedureParameter?> values)
    {
        return _repository.ExecuteAsync(procedureName, values);
    }
}