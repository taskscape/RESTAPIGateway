To create a C# ASP.NET Core Web API that exposes an API controller for any table in a database, you'll first need to set up a new project and install the required packages. Then, you can create a generic repository and service layer that interfaces with the database, and finally, a generic API controller that exposes CRUD operations for any table.

Here's a step-by-step guide to help you create the project:

Create a new ASP.NET Core Web API project:

```
dotnet new webapi -n GenericTableAPI
cd GenericTableAPI
```

Install the required NuGet packages:

```
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Define a connection string in the appsettings.json file:

```
"ConnectionStrings":
{
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=YourDatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

Create a DbContext class:

```csharp
// In a new folder named 'Data', create a new file named 'AppDbContext.cs'
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;

namespace YourNamespace.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Add a DbSet for each table in your database
        public DbSet<YourModel> YourModels { get; set; }
    }
}
```

Update Startup.cs to add the DbContext:

```csharp
using YourNamespace.Data;

// Inside the ConfigureServices method
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
```

Create a generic repository:

```csharp
// In a new folder named 'Repositories', create a new file named 'GenericRepository.cs'
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Data;

namespace YourNamespace.Repositories
{
    public class GenericRepository<TEntity> where TEntity : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        // CRUD operations go here
        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<TEntity> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<TEntity> UpdateAsync(TEntity entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task DeleteAsync(object id)
        {
            TEntity entityToDelete = await _dbSet.FindAsync(id);
            _dbSet.Remove(entityToDelete);
            await _context.SaveChangesAsync();
        }
    }
 }
```

Create a generic service:

```csharp
// In a new folder named 'Services', create a new file named 'GenericService.cs'
using System.Collections.Generic;
using System.Threading.Tasks;
using YourNamespace.Repositories;

namespace YourNamespace.Services
{
    public class GenericService<TEntity> where TEntity : class
    {
        private readonly GenericRepository<TEntity> _repository;

        public GenericService(GenericRepository<TEntity> repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return _repository.GetAll
        }

        public Task<TEntity> GetByIdAsync(object id)
        {
            return _repository.GetByIdAsync(id);
        }

        public Task<TEntity> AddAsync(TEntity entity)
        {
            return _repository.AddAsync(entity);
        }

        public Task<TEntity> UpdateAsync(TEntity entity)
        {
            return _repository.UpdateAsync(entity);
        }

        public Task DeleteAsync(object id)
        {
            return _repository.DeleteAsync(id);
        }
    }
}
```

Create a generic API controller:

```csharp
// In the 'Controllers' folder, create a new file named 'GenericController.cs'
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Services;

namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenericController<TEntity> : ControllerBase where TEntity : class
    {
        private readonly GenericService<TEntity> _service;

        public GenericController(GenericService<TEntity> service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TEntity>>> GetAll()
        {
            var entities = await _service.GetAllAsync();
            return Ok(entities);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TEntity>> GetById(object id)
        {
            var entity = await _service.GetByIdAsync(id);

            if (entity == null)
            {
                return NotFound();
            }

            return Ok(entity);
        }

        [HttpPost]
        public async Task<ActionResult<TEntity>> Add(TEntity entity)
        {
            await _service.AddAsync(entity);
            return CreatedAtAction("GetById", new { id = entity }, entity);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TEntity>> Update(object id, TEntity entity)
        {
            if (id != entity.Id)
            {
                return BadRequest();
            }

            try
            {
                await _service.UpdateAsync(entity);
            }
            catch
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(object id)
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
```

Now you have a generic API controller that exposes CRUD operations for any table in your database. To use the generic controller for a specific table, you'll need to create a specific controller that inherits from the generic controller and register the corresponding repository and service in the Startup.cs. Here's an example using a Product model:

Create a model for the table:

```csharp
// In a new folder named 'Models', create a new file named 'Product.cs'
namespace YourNamespace.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}
```

Add the DbSet for the Product model in the AppDbContext:

```csharp
public DbSet<Product> Products { get; set; }
```

Create a specific controller for the Product model:

```csharp
// In the 'Controllers' folder, create a new file named 'ProductController.cs'
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Models;
using YourNamespace.Services;

namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : GenericController<Product>
    {
    public ProductController(GenericService<Product> service) : base(service) { }
    }
}
```

4. Register the specific repository and service for the `Product` model in `Startup.cs`:

```csharp
using YourNamespace.Models;
using YourNamespace.Repositories;
using YourNamespace.Services;

// Inside the ConfigureServices method
services.AddScoped<GenericRepository<Product>>();
services.AddScoped<GenericService<Product>>();
```

Now your API should be able to handle CRUD operations for the Product model at the /api/product endpoint. You can repeat these steps for any other table in your database. Just create a new model, add the DbSet, create a specific controller, and register the repository and service.

While it's possible to generate models dynamically at runtime using reflection or emitting IL code, it's not recommended for most scenarios because it can lead to complex and hard-to-maintain code. Instead, you should consider using Entity Framework Core's "Database First" approach to generate your models from your database schema.

The "Database First" approach is useful when you have an existing database and want to generate the models based on its schema. Here's how you can achieve this:

Install the required NuGet package for the Scaffold-DbContext command:

```
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

Generate the models using the Scaffold-DbContext command:

```
dotnet ef dbcontext scaffold "Server=(localdb)\\MSSQLLocalDB;Database=YourDatabaseName;Trusted_Connection=True;MultipleActiveResultSets=true" Microsoft.EntityFrameworkCore.SqlServer -o Models
```

Replace "YourDatabaseName" with the name of your database. The -o Models option specifies that the generated models should be placed in a folder named "Models". This command will generate a model class for each table in the database and a DbContext class.

After generating the models, follow the previous instructions to create a generic repository, service, and API controller.

Keep in mind that using the "Database First" approach requires regenerating the models whenever the database schema changes. This may not provide the flexibility of a fully dynamic model generation at runtime, but it offers better maintainability, type safety, and readability.

However, if you still want to work with fully dynamic models at runtime, you can consider using a library like [Dapper](https://github.com/DapperLib/Dapper) to execute SQL queries and map the results to dynamically created objects. Note that this approach will not provide you with the compile-time type safety and features of Entity Framework Core, and it may be more complex to implement and maintain.

Here's an example of using Dapper to query a table and return dynamic objects:

Install the Dapper NuGet package:

```
dotnet add package Dapper
Create a generic Dapper repository:
```

```csharp
// In a new folder named 'Repositories', create a new file named 'DapperRepository.cs'
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.SqlClient;

namespace YourNamespace.Repositories
{
    public class DapperRepository
    {
        private readonly string _connectionString;

        public DapperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<IEnumerable<dynamic>> GetAllAsync(string tableName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var result = await connection.QueryAsync($"SELECT * FROM {tableName}");
                return result;
            }
        }
    }
}
```

Update the Startup.cs to inject the DapperRepository:

```csharp
using YourNamespace.Repositories;

// Inside the ConfigureServices method
services.AddSingleton(new Dapper

Repository(Configuration.GetConnectionString("DefaultConnection")));
```

4. Create a Dapper API controller:

```csharp
// In the 'Controllers' folder, create a new file named 'DapperController.cs'
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YourNamespace.Repositories;

namespace YourNamespace.Controllers
{
    [Route("api/dapper/{tableName}")]
    [ApiController]
    public class DapperController : ControllerBase
    {
        private readonly DapperRepository _repository;

        public DapperController(DapperRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<dynamic>>> GetAll(string tableName)
        {
            var entities = await _repository.GetAllAsync(tableName);
            return Ok(entities);
        }
    }
}
```

Now your API should be able to query any table in your database and return the results as dynamic objects. Note that this example demonstrates only the "GetAll" operation, but you can extend the DapperRepository to include other CRUD operations as well. Here are examples of how to implement other CRUD operations in the DapperRepository:

```csharp
public async Task<dynamic> GetByIdAsync(string tableName, string idColumn, object id)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();
        var result = await connection.QuerySingleOrDefaultAsync($"SELECT * FROM {tableName} WHERE {idColumn} = @Id", new { Id = id });
        return result;
    }
}

public async Task AddAsync(string tableName, IDictionary<string, object> values)
{
    var columns = string.Join(", ", values.Keys);
    var parameters = string.Join(", ", values.Keys.Select(k => $"@{k}"));

    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync($"INSERT INTO {tableName} ({columns}) VALUES ({parameters})", values);
    }
}

public async Task UpdateAsync(string tableName, string idColumn, object id, IDictionary<string, object> values)
{
    var setClauses = string.Join(", ", values.Keys.Select(k => $"{k} = @{k}"));

    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync($"UPDATE {tableName} SET {setClauses} WHERE {idColumn} = @Id", new { Id = id });
    }
}

public async Task DeleteAsync(string tableName, string idColumn, object id)
{
    using (var connection = new SqlConnection(_connectionString))
    {
        await connection.OpenAsync();
        await connection.ExecuteAsync($"DELETE FROM {tableName} WHERE {idColumn} = @Id", new { Id = id });
    }
}
```

Keep in mind that these examples use string interpolation to build SQL queries, which may expose your application to SQL injection attacks. Make sure to properly sanitize and validate any user input used to build queries.

You can extend the `DapperController` to support these CRUD operations by adding corresponding methods and calling the appropriate methods in the `DapperRepository`.

Remember that using dynamic models and Dapper might not provide you with the compile-time type safety, IntelliSense support, and other features that Entity Framework Core offers. While it may give you more flexibility, it can also lead to more complex and harder-to-maintain code.

If you want to include the newly implemented CRUD operations in your DapperController, you can add the following methods:

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<dynamic>> GetById(string tableName, string idColumn, object id)
{
    var entity = await _repository.GetByIdAsync(tableName, idColumn, id);

    if (entity == null)
    {
        return NotFound();
    }

    return Ok(entity);
}

[HttpPost]
public async Task<ActionResult> Add(string tableName, [FromBody] IDictionary<string, object> values)
{
    await _repository.AddAsync(tableName, values);
    return CreatedAtAction("GetById", new { tableName, idColumn = values.Keys.First(), id = values.Values.First() });
}

[HttpPut("{id}")]
public async Task<ActionResult> Update(string tableName, string idColumn, object id, [FromBody] IDictionary<string, object> values)
{
    await _repository.UpdateAsync(tableName, idColumn, id, values);
    return NoContent();
}

[HttpDelete("{id}")]
public async Task<ActionResult> Delete(string tableName, string idColumn, object id)
{
    await _repository.DeleteAsync(tableName, idColumn, id);
    return NoContent();
}
```

The DapperController now exposes endpoints for all CRUD operations. Note that these endpoints require you to pass the table name, ID column, and other relevant information as URL parameters or request bodies. This approach is more flexible but less type-safe and harder to maintain compared to the Entity Framework Core solution.

In summary, while working with dynamic models and Dapper can provide greater flexibility, it may not be the best solution for most scenarios due to potential maintenance challenges and lack of compile-time type safety. If you have an existing database, consider using the Entity Framework Core "Database First" approach to generate models based on the database schema. This approach offers better maintainability, type safety, and readability.

For highly dynamic scenarios where you need to work with tables that are not known at compile-time, you may use Dapper as demonstrated above. However, be aware of the trade-offs in terms of code complexity and maintainability. Make sure to properly validate and sanitize user inputs to avoid potential security vulnerabilities such as SQL injection attacks.

In addition to the aforementioned solutions, another approach you can consider is creating an abstraction layer for your database schema that allows you to work with tables and columns dynamically while still taking advantage of Entity Framework Core features.

Here's an example of how to create an abstraction layer:

Define a base entity class with a dictionary to store column values:

```csharp
public abstract class DynamicEntity
{
    public IDictionary<string, object> Columns { get; set; } = new Dictionary<string, object>();
}
```

Implement a custom DbSet to work with dynamic entities:

```csharp
public class DynamicDbSet : DbSet<DynamicEntity>
{
    // Custom implementation of DbSet to handle DynamicEntity instances
}
```

Update your AppDbContext to include a DynamicDbSet for each table:

```csharp
public class AppDbContext : DbContext
{
    public DynamicDbSet Table1 { get; set; }
    public DynamicDbSet Table2 { get; set; }
    // Add more DynamicDbSet properties as

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the entity model for each table using modelBuilder
        modelBuilder.Entity<DynamicEntity>(entity =>
        {
            entity.HasNoKey();
            // Define columns for Table1
            entity.Property(e => e.Columns["Column1"]).HasColumnName("Column1").HasColumnType("nvarchar(50)");
            entity.Property(e => e.Columns["Column2"]).HasColumnName("Column2").HasColumnType("int");
            // Add more column definitions as needed for other columns
        });

        modelBuilder.Entity<DynamicEntity>(entity =>
        {
            entity.HasNoKey();
            // Define columns for Table2
            entity.Property(e => e.Columns["ColumnA"]).HasColumnName("ColumnA").HasColumnType("nvarchar(50)");
            entity.Property(e => e.Columns["ColumnB"]).HasColumnName("ColumnB").HasColumnType("decimal");
            // Add more column definitions as needed for other columns
        });

        // Add more entity configurations for other tables
    }
}
```

Implement a generic service and controller that use the `DynamicDbSet` properties from the `AppDbContext` to perform CRUD operations on any table.

Please note that this approach requires significant customization of Entity Framework Core's default behavior, and it may not be suitable for all scenarios. In addition, it may introduce performance overhead and potential security risks if not implemented correctly. Use this approach only if you need maximum flexibility and are willing to invest time in building a custom solution.

In conclusion, there are multiple approaches to working with dynamic tables in a database using C# ASP.NET:

Entity Framework Core Database First: This approach is recommended for most scenarios when you have an existing database with known schema. It generates models based on the schema and offers type safety, maintainability, and readability.

Dapper: This is a more flexible but less type-safe approach that allows you to work with tables and columns dynamically at runtime. This method might be more complex to implement and maintain compared to using Entity Framework Core.

Custom Abstraction Layer: This approach involves creating a custom abstraction layer for your database schema and working with tables and columns dynamically while still using Entity Framework Core. This option requires significant customization and may introduce performance overhead and potential security risks if not implemented correctly.

Each approach has its trade-offs in terms of flexibility, complexity, and maintainability. Choose the one that best fits your requirements and constraints. Keep in mind that properly sanitizing and validating user inputs is crucial when working with dynamic table and column names to avoid potential security vulnerabilities such as SQL injection attacks.