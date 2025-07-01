# REST API Gateway

This is the universal REST API Server, primarily used for bridging RESTful clients with any MSSQL or Oracle database.

## Description

REST API Gateway is a service that sits in front of a database and provides a REST API interface. It acts as an intermediary between clients, such as web or mobile applications, and your database (SQL Server, Oracle, or Sybase). The service includes additional features for security, scaling, and management. Its goal is to make the integration of different systems easier and more straightforward by offering an innovative bridging service that can be quickly deployed.

## Architecture

![[diagram.png]](diagram.png)

## Requirements

The services requires:

- Internet Information Services, version 7.0 or later
- .NET Hosting Bundle, version 8.0 or later [Download link](https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.410/dotnet-sdk-8.0.410-win-x64.exe)

## Installation

Please install Internet Information Services, following installation of the .NET Framework itself in order to make sure that the webserver is capable of hosting .net framework code.

The service must be installed by extracting the installation package (ZIP) into a folder mapped to any IIS website.

You need to manually configure a new website by pointing it to the root directory where the files have been extracted.

You need to manually configure the new website bindings by exposing service endpoints via HTTP or HTTP(s) interfaces.

## Basic usage

The service can be used by querying configured endpoints by providing database table name(s) with appropriate parameters and HTTP verbs, for example:

### `GET` `http://localhost/api/tables/{tablename}`

- Returns 200 HTTP code and JSON object in response body with all rows from the table `tablename`.

### `GET` `http://localhost/api/tables/{tablename}/{id}`

- Returns 200 HTTP code and JSON object in response body for a given `id` from a table `tablename` representing the row specified by the primary key.

### `POST` `http://localhost/api/tables/{tablename}` 

- Accepts JSON object as a parameter of request body and returns 201 HTTP code for a newly created primary key identifying created database row.

### `PUT` `http://localhost/api/tables/{tablename}/{id}`

- Accepts JSON object as a parameter of request body and returns 200 HTTP code along an updated JSON object for a given `id` from a table `tablename`. It completely replaces the record, setting all unspecified columns to blank.

### `PATCH` `http://localhost/api/tables/{tablename}/{id}`

- Accepts JSON object as a parameter of request body and returns 200 HTTP code along an updated JSON object for a given `id` from a table `tablename`. It updates only the specified columns, keeping the rest untouched.

### `DELETE` `http://localhost/api/tables/{tablename}/{id}`

- Returns 200 HTTP code and empty response body for a given `id` of a table `tablename` representing deletion of a specific row from a database.

### `POST` `http://localhost/api/procedures/{procedureName}`

- Accepts JSON object as a parameter of request body and returns 200 HTTP code for a successful procedure execution along with JSON object in response body. The request body must be a valid json object, for example:

```json
[
  {
    "name": "Parameter1",
    "value": "sampleValue1",
    "type": "string"
  },
  {
    "name": "Parameter2",
    "value": "10",
    "type": "int"
  }
]
 ```

- The only allowed types are `string`, `int`, `float` and `null`. For type `null`, `value` is not taken into account. For a parameterless procedure, leave empty brackets `[]`.

### Swagger UI  

Swagger UI is an interactive API documentation tool that allows developers to explore and test API endpoints directly from a web interface. It provides a user-friendly way to visualize API requests, responses, and schemas.  

**Default Endpoint:**  
The Swagger UI is accessible at:  

```
/swagger
```

**Authentication & Access Control**

If authentication is required (see the **Configuration** section), Swagger UI will display API endpoints and tables that the authenticated user has permission to access.  

- **Authenticated Users:** Only see tables they are authorized to view.  
- **Anonymous Users:** If anonymous access is allowed, tables with `"*"` (public) permissions will be displayed.  

## Composition

The service can be used to specify more complex composition requests that allow calling inner API methods in a sequential manner that allows accessing  return values using JSON Path and use as parameters for calling subsequent API methods. In this usage scenario user may want to perform multiple operations on multiple tables in a single API call.

### Structure Overview

The `/api/composite` endpoint follows this structure:

```json
{
  "debug": true, // Optional
  "response": "...",  // Optional
  "requests": [ 
    {
      "method": "...",
      "endpoint": "...",
      "foreach": "...", // Optional
      "parameters": { 
        // Optional
      },
      "variables": { 
        // Optional
      }
    },
    {
      // Subsequent requests follow the same structure
    }
  ]
}
```

### Explanation of Each Field

**1. `debug` (Boolean, Optional)**

  Enables or disables debug information in the response. When set to `true`, the endpoint returns additional debug information in the response body. This can help in troubleshooting issues or understanding request behavior.

**2. `response` (Object, Optional)**

  Defines the structure of the response. You can use placeholders `{variables}` within the object that will be dynamically replaced with corresponding data.

  If no `response` object is provided, then all variables will be included in the response automatically.

  **Example**

  ```json
  "response": {
    "user": "{username}",
    "password": "{userPassword}",
    "userId": "{example-var}"
  }
  ```

  In this example:
  - `{username}` will be replaced with the user's name.
  - `{userPassword}` will be replaced with the user's password.
  - `{example-var}` will be replaced with the value of a variable named example-var.

**3. `requests` (Array)**

  The top-level array named `requests` contains all the individual requests you wish to execute. The requests will be processed sequentially, from the first item to the last.

**4. Request Object**

  Each request in the "requests" array contains the following fields:

  - `method` (String):

    Specifies the HTTP method for the request. Supported methods include:

      - `GET`
      - `POST`
      - `PATCH`
      - `PUT`
      - `DELETE`
  
    **Example**
    
    ```json
    "method": "POST"
    ```

  - `endpoint` (String):

    The API endpoint to which the request will be sent. You can include variables (defined earlier in the sequence) within curly brackets `{}`.

    **Example**
    
    ```json
    "endpoint": "/api/users/{example-var}"
    ```

  - `foreach` (String, Optional):

    If provided, the request will be executed for each element in the specified variable. The variable should reference a list, and the current element in the iteration will replace any placeholders in the `"endpoint"` or `"parameters"`.

    **Example**
    
    ```json
    "foreach": "{userList}"
    ```

    - In this case, the request will execute for each element in `userList`.

  - `parameters` (Object, Optional):

    Specifies the body parameters for the request. The parameters should be key-value pairs, where the key is the parameter name and the value is its corresponding value. If the value should come from a variable defined in a previous request, enclose the variable name in curly brackets `{}`.

    **Example**
    
    ```json
    "parameters": {
      "username": "johndoe",
      "password": "securepassword123",
      "userId": "{example-var}"
    }
    ```
  
  - `variables` (Object, Optional):

    Defines variables that will be stored from the response of this request, for use in subsequent requests. The keys are the variable names, and the values are the JSON paths or specific response fields to be saved.

    ```json
    "variables": {
      "userId": "Id",
      "lastUserName": "[-1:].FullName"
    }
    ```

### Example usage

Here’s an example configuration that demonstrates the syntax and how variables can be used across multiple requests.

```json
{
  "requests": [
    {
      "method": "POST",
      "endpoint": "/api/users",
      "parameters": {
        "firstName": "John",
        "lastName": "Doe",
        "email": "john.doe@example.com"
      },
      "variables": {
        "userId": "Id"
      }
    },
    {
      "method": "GET",
      "endpoint": "/api/users/{userId}",
      "parameters": {
        "expand": "details"
      }
    },
    {
      "method": "DELETE",
      "endpoint": "/api/users/{userId}"
    }
  ]
}
```

### How Variables Work

Variables defined in the `"variables"` section can be used in subsequent requests:

1. **Defining Variables:**

    In the first request, the `"variables"` section saves the value returned under the `"Id"` field as `"userId"`.

2. **Using Variables:**

    In the second request, `{userId}` is used within the endpoint and can also be used in the parameters. The value is replaced with the `"Id"` obtained from the first request.

### JSONPath Syntax for `variables`

- You can use JSONPath to specify which part of the response should be stored.

- For example, `"JSON-Path-var": "[-1:].FullName"` would select the `FullName` of the last item in a list.

- You can read more about JSONPath [here](https://support.smartbear.com/alertsite/docs/monitors/api/endpoint/jsonpath.html)

### Notes

- Requests will be executed in the order provided.

- If a request fails, subsequent requests may not execute

- Variables are accessible in all following requests after they have been defined.

### Practical Example - Data Aggregation and Reporting
  
This example retrieves data from multiple endpoints, aggregates it, and then sends a report to an administrator.

```json
{
  "requests": [
    {
      "method": "GET",
      "endpoint": "/api/users",
      "variables": {
        "userCount": "$.length"
      }
    },
    {
      "method": "GET",
      "endpoint": "/api/orders",
      "variables": {
        "orderCount": "$.length"
      }
    },
    {
      "method": "POST",
      "endpoint": "/api/reports",
      "parameters": {
        "title": "Daily Summary",
        "body": "Users: {userCount}, Orders: {orderCount}",
        "recipient": "admin@example.com"
      }
    }
  ]
}
```

## Configuration

The service needs the database connection to be configured in the appsettings.json file manually by configuring the following sections:

### Logging

The section allows to configure the log verbosity for both the service and the framework itself. Allowed values for the following configuration section(s) are: "Information", "Warning", "Error":

```text
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
```

### Allowed hosts

This sections allows to configure which external hosts can be permitted to connecto to the service. This can be used to lock exposure of service's functionality to particular hosts within the internal network:

```text
  "AllowedHosts": "*",
```

### Connection strings

This section allows to configure connection to the database used to perform REST operations. 

The allowed connection parameters cover either connection to a Microsoft SQL Server instance, for example:

```text
  "ConnectionStrings": {
    "DefaultConnection": "Server=[database_host],[optional_database_port];Database=[database_name];User Id=[user_name]; Password=[password];TrustServerCertificate=True"
  },
```

Alternatively the connection parameter can be adjusted to support Oracle database server:

```text
  "ConnectionStrings": {
    "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=[database_host])(PORT=[database_port]))(CONNECT_DATA=(SERVICE_NAME=[service_name])));User Id=[user_name];Password=[password];"
  },
```

You need to substitute tokens denoted by square brackets with actual values (without square brackets). In case of doubts, please follow the official instructions for alternative connection string syntax, if needed.

### Cache Duration Seconds

This section allows you to configure how long cached data should remain in memory before expiring. This setting is useful for optimizing performance and reducing unnecessary database or API calls.

```text
  "CacheDurationSeconds": [Cache_duration_in_seconds],
```

The configured duration determines how long objects stored in memory will persist before being removed automatically. If set to 0 or a negative value, caching is disabled.

### Retry Policy

This section allows you to configure the retry policy in case of database failures.

```json
    "RetryPolicy": {
        "RetryCount": 3,
        "WaitTimeMilliseconds": 200
},
```
- `RetryCount` - Number of retries.
- `WaitTimeMilliseconds` - Time between retries in milliseconds.

### Rate Limiting

This section allows you to configure rate limiting.

#### Configuration Structure
Modify your configuration file to enable and customize rate limiting:

```json
  "RateLimiting": {
    "Type": "FixedWindow", // Options: FixedWindow, SlidingWindow, TokenBucket, Concurrency, null (to disable rate limiting)
    "Mode": "PerUser", // Options: PerIP, PerUser. Default: PerIP
    "PermitLimit": 3,
    "WindowSeconds": 30,
    "SegmentsPerWindow": 2,
    "QueueLimit": 2,
    "TokensPerPeriod": 1,
    "ReplenishmentPeriodSeconds": 5
  }
```

#### Explanation of Parameters
| Parameter                   | Description |
|-----------------------------|-------------|
| `Type`                      | The type of rate limiting to use. Options include `FixedWindow`, `SlidingWindow`, `TokenBucket`, `Concurrency`. Set to `null` to disable rate limiting. |
| `Mode`                      | Determines if rate limiting is based on IP (`PerIP`) or User (`PerUser`). Default is `PerIP`. |
| `PermitLimit`               | The maximum number of requests allowed within the rate limit window. Required for all strategies. |
| `WindowSeconds`             | The duration of the rate limit window in seconds (used in `FixedWindow` and `SlidingWindow` strategies). |
| `SegmentsPerWindow`         | Number of segments within a window for `SlidingWindow` strategy to allow smoother rate limiting. |
| `QueueLimit`                | Maximum number of requests that can be queued before rejection (used in `TokenBucket` and `Concurrency` strategies). |
| `TokensPerPeriod`           | Number of tokens refilled per period in the `TokenBucket` strategy. |
| `ReplenishmentPeriodSeconds`| The time interval (in seconds) to replenish tokens in the `TokenBucket` strategy. |

By configuring these settings in `appsettings.json`, you can fully control the rate-limiting behavior of your .NET 8 application.



### Enable Swagger

In this section, you can enable Swagger by setting the following option:

```text
  "EnableSwagger": true,
```

**Note**: For production environments, it is recommended to disable Swagger to enhance security.

### Enabling Basic Authentication for Swagger  

You can enable Basic Authentication for the Swagger UI by setting the following option in your configuration:  

```json
"BasicAuthForSwagger": true
```

When enabled (`true`), users must enter a valid username and password before accessing the Swagger UI. Only the credentials specified in `BasicAuthSettings` (see the **Authentication** section below) will be accepted.  

**Default Setting:** This option is disabled by default (`false`).

### Enable Exception Page

You can enable exception page by setting the environment variable `DOTNET_ENVIRONMENT` to `Development`

```text
set DOTNET_ENVIRONMENT=Production
```

**Note**: For production environments, it is recommended to disable exception page to enhance security.

### Authentication

The solution supports authentication using Basic Auth, JWT Token Auth (Bearer Authentication), or Windows Authentication. 

**Note**: All of these authentication methods can be configured and used simultaneously. For example, if both Basic Auth and Bearer Authentication (JWT) are enabled, a user can authenticate using either method. Similarly, if Windows Authentication is also configured, it can be used alongside the other methods without conflict. This flexibility allows for seamless integration into various security environments.

**In order to enable JWT token based authentication for API endpoints, the following values need to be configured:**

```text
  "JwtSettings": {
    "Key": "[secret_key]",
    "Issuer": "[host_name]",
    "Audience": "[host_name]",
    "Subject": "JWTServiceAccessToken",
    "Users": [
      {
        "Username": "[user_name]",
        "Password": "[password]",
        "Role": "[role]", //OPTIONAL
        "Roles": [ "[role1]", "[role2]", "[role2]" ] //OPTIONAL
      }
      // Subsequent users follow the same structure
    ]
  },
```
- You can specify as many users as needed (at least one), with each user assigned one or more roles for authorization purposes. The system supports both a single role (using "Role") or multiple roles (using "Roles"). However, it is recommended to use "Roles" for clarity and better readability.

- If both "Role" and "Roles" are specified for a user, their roles will be combined, and the user will have access to all roles listed.

- Each username must be unique to ensure proper authentication and authorization for each user.

- If above section is configured, only users with a valid JWT token will be permitted to use the API endpoints.

**In order to enable BASIC authentication for exposed API endpoints, the following values need to be configured:**

```text
"BasicAuthSettings": [
  {
    "Username": "[user_name]",
    "Password": "[password]",
    "Role": "[role]", //OPTIONAL
    "Roles": [ "[role1]", "[role2]", "[role3]" ] //OPTIONAL
  }
  // Subsequent users follow the same structure
],

```

- You can specify as many users as needed (at least one), with each user assigned one or more roles for authorization purposes. The system supports both a single role (using "Role") or multiple roles (using "Roles"). However, it is recommended to use "Roles" for clarity and better readability.

- If both "Role" and "Roles" are specified for a user, their roles will be combined, and the user will have access to all roles listed.

- Each username must be unique to ensure proper authentication and authorization for each user.

- If above section is configured, only users with a valid combination of username and password will be permitted to use the API endpoints.

**In order to enable WINDOWS authentication, the following value needs to be set to true.**

```text
  "NTLMAuthentication": true,
```

And the IIS has to be configured to use 'Windows Authentication' as well.

If none of the sections (`JwtSettings` or `BasicAuthSettings` or `NTLMAuthentication`) are provided, the exposed endpoints will require no authentication.

You need to substitute tokens denoted by square brackets with actual values (without square brackets).

**Configuring Users Across Authentication Methods**

Users configured for Basic Auth are distinct from users configured for JWT Bearer Auth. This means that you can define separate users for each authentication method. For instance:
- A user defined in the `BasicAuthSettings` section will have access only via Basic Authentication.
- A user defined in the `JWTSettings` section will have access only via Bearer Authentication.

This separation allows for granular control over authentication and user access, enabling different authentication methods for different users if needed.

### Auditing

The auditing capabilities provided by a event listener can be configured. The following example configuration provides rolling text file logging functionality.

```text
"Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
```

Please consult [https://github.com/serilog/serilog-settings-configuration](documentation) for alternative configuration in order to support persistence of logs in a database or other data sinks.

### Table Authorization

The `tablesettings.json` file defines permissions for database tables and stored procedures, specifying which actions are allowed for each table and procedure and who has access based on user roles and individual users. This setup allows for granular control over data access at both the table and procedure levels.

**Note** This file is required to run the service.

#### Structure of `tablesettings.json`

The configuration file is structured under the `Database` key, containing `Tables` and `Procedures` sections where access settings can be defined in detail.

### **Tables Section**
Each table entry supports defining actions: `select`, `insert`, `update`, and `delete`, along with access specifications for each action.

#### **Default Permissions for Tables**
The **Default Permissions** can be specified using `*` (asterisk) instead of a table name. This will define global permissions that apply to all tables. Permissions are specified for the actions `select`, `insert`, `update`, and `delete`.

**Example:**

```json
{
  "Database": {
    "Tables": {
      "*": {
        "select": ["*"], // Default: All users can select.
        "insert": ["rolename:AdminRole"], // Default: AdminRole can insert.
        "update": [], // Default: No update access.
        "delete": [] // Default: No delete access.
      }
      // Every explicitly specified table will use both the default and specified permissions
    }
  }
}
```

The **Default Permissions** support both **Simple Access** and **Role-Based/User-Based Access**.

#### Simple Access

Simple permissions can be assigned by listing the allowed actions as an array. Any authenticated user will have those permissions.

```json
{
  "Database": {
    "Tables": {
      "TableName": [ PERMISSIONS in string array ]
    }
  }
}
```

**Example**

```json
{
  "Database": {
    "Tables": {
      "MyTable1": [ "select", "insert" ]
    }
  }
}
```
In this example, any user can `select` and `insert` records in `MyTable1`.

#### Role-Based and User-Based Access

Roles or Usernames can be specified for each action to limit access to users with specific roles or usernames.

```json
{
  "Database": {
    "Tables": {
      "TableName": {
        "select": [ PERMISSIONS ],
        "update": [ PERMISSIONS ],
        "delete": [ PERMISSIONS ],
        "insert": [ PERMISSIONS ]
      }
    }
  }
}
```

**Roles**

Roles can be specified for each action to limit access to users with specific roles.

Although roles can be specified without any prefix, using `rolename:` allows for clear differentiation in cases where naming conventions could be ambiguous.

```json
"select": [ "rolename:Role1", "Role2" ]
```
Here, only users with Role1 or Role2 can perform select actions.

**Usernames**

Specific users can be granted access by specifying their usernames with the prefix `username:`.

```json
"select": [ "username:User1", "username:User2" ]
```
Here, only users with User1 or User2 can perform select actions.

**Wildcard Access**

Using `*` allows all users to access a specific action.

```json
"select": [ "*" ]
```
This grants `select` permission to all authenticated users.

**Example**

```json
{
  "Database": {
    "Tables": {
      "Table1": {
        "select": [ "*" ],
        "update": [ "Role1", "rolename:Role2" ],
        "delete": [ "username:user3" ],
        "insert": ["username:user1", "Role2", "rolename:Role3", "username:user3"]
      }
    }
  }
}
```
In this example for `Table1`:
- Everyone can `select`.
- Only users with `Role1` or `Role2` can `update`.
- Only `user3` can `delete`.
- `insert` is allowed for `user1`, `user3`, and roles `Role2` and `Role3`.

### **Procedures Section**

The `Procedures` section defines access permissions for stored procedures. Just like the `Tables` section, it allows specifying default permissions and individual overrides for specific procedures.

#### **Default Permissions for Procedures**
Using `*` (asterisk) instead of a procedure name defines global permissions that apply to all procedures.

#### **Example:**
```json
{
  "Database": {
    "Procedures": {
      "*": ["Admin"], // Default: Only Admins can execute every procedure.
      "MyProcedureName": ["username:user1"], // Admin (From Default [*] permission) and user1 have access.
      "PublicProcedure": ["*"] // Everyone has access.
    }
  }
}
```

### **Key Notes**
- Every explicitly specified table or procedure **will inherit default permissions**.
- The `Tables` section supports four actions: `select`, `insert`, `update`, and `delete`.
- The `Procedures` section only requires listing allowed users or roles, as procedures typically have execute permissions.
- Role-based (`rolename:RoleName`) and user-based (`username:UserName`) access control is supported for both tables and procedures.
- The configuration allows a mix of simple and detailed permissions within the same file.

This structure ensures fine-grained control over database access while providing sensible defaults.

Here is a more professional and polished version of your **Status Codes** section, ready to be integrated into your README:

---

## **HTTP Status Codes**

The following table outlines the standard HTTP status codes returned by the REST API Gateway for various scenarios:

| **Scenario**                                        | **HTTP Code**               |
| --------------------------------------------------- | --------------------------- |
| Successful GET request (data found or empty result) | `200 OK`                    |
| Resource created successfully (POST)                | `201 Created`               |
| Successful update or delete operation               | `200 OK`                    |
| Stored procedure executed with no return data       | `204 No Content`            |
| Missing or invalid authentication credentials       | `401 Unauthorized`          |
| Authenticated but lacking required permissions      | `403 Forbidden`             |
| Resource not found (table, row, or procedure)       | `404 Not Found`             |
| Unhandled server error or exception                 | `500 Internal Server Error` |


#### Explanation of Status Codes

* **2xx – Success**

  * `200 OK`: The request was successful. Applies to GET, PUT, PATCH, and DELETE operations—even when the result set is empty.
  * `201 Created`: The resource was successfully created. Returned by POST endpoints.
  * `204 No Content`: The request was successful, but no content is returned (typically from stored procedures).

* **4xx – Client Errors**

  * `401 Unauthorized`: The request lacks valid authentication credentials. Applies when no token or invalid token is provided.
  * `403 Forbidden`: Authentication was successful, but the user does not have permission to access the resource or operation.
  * `404 Not Found`: The requested resource (such as a table, row, or procedure) does not exist.

* **5xx – Server Errors**

  * `500 Internal Server Error`: An unexpected error occurred on the server while processing the request.


## Maintenance

The service produces rolling logs in the \logs folder, recording every external and internal operation(s).
The logs are rotating automatically.

## Support

The owner of the service is responsible for maintaining the service.

## Licence

This software is available under dual licensing options:

- Open Source License: GNU Affero General Public License (AGPL)
You can use, modify, and distribute the software for free under the terms of the GNU Affero General Public License (AGPL), which is included in the LICENSE file of this repository. This option is ideal for developers who wish to use the software in other open source projects or for personal use.

- Commercial License:
If you want to use this software in a commercial application or require additional features and support not available under the open source license, you must obtain a commercial license. The commercial license allows for private modifications and grants you access to premium features and support services.

### Obtaining a Commercial License

To obtain a commercial license or to inquire about pricing and terms, please contact us at [RESTAPIGateway.com](https://restapigateway.com).

### Why Dual Licensing?

Dual licensing allows us to support the open source community while also providing a commercial offering that meets the needs of businesses requiring advanced features and dedicated support. This model helps fund the continued development and maintenance of the software.

### Contributions

Contributions to this project are welcome under the open source license terms. By contributing, you agree to your code being licensed under the same open source license. If you're contributing under a commercial agreement, different terms may apply as agreed upon.
