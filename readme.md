# REST API Gateway

This is the universal REST API Server, primarily used for bridging RESTful clients with any MSSQL or Oracle database.

## Description

REST API Gateway is a service that sits in front of a database and provides a REST API interface. It acts as an intermediary between clients, such as web or mobile applications, and your database (SQL Server, Oracle, or Sybase). The service includes additional features for security, scaling, and management. Its goal is to make the integration of different systems easier and more straightforward by offering an innovative bridging service that can be quickly deployed.

## Architecture

![[diagram.png]]

## Requirements

The services requires:

- Internet Information Services, version 7.0 or later
- Microsoft .NET Framework, version 6.0 or later

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

## Composition

The service can be used to specify more complex composition requests that allow calling inner API methods in a sequential manner that allows accessing  return values using JSON Path and use as parameters for calling subsequent API methods. In this usage scenario user may want to perform multiple operations on multiple tables in a single API call.

### Structure Overview

The `/api/composite` endpoint follows this structure:

```json
{
  "requests": [ 
    {
      "method": "...",
      "endpoint": "...",
      "parameters": { 
        // Optional
      },
      "returns": { 
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

**1. `requests` (Array)**

  The top-level array named `requests` contains all the individual requests you wish to execute. The requests will be processed sequentially, from the first item to the last.

**2. Request Object**

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
  
  - `returns` (Object, Optional):

    Defines variables that will be stored from the response of this request, for use in subsequent requests. The keys are the variable names, and the values are the JSON paths or specific response fields to be saved.

    ```json
    "returns": {
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
      "returns": {
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

Variables defined in the `"returns"` section can be used in subsequent requests:

1. **Defining Variables:**

    In the first request, the `"returns"` section saves the value returned under the `"Id"` field as `"userId"`.

2. **Using Variables:**

    In the second request, `{userId}` is used within the endpoint and can also be used in the parameters. The value is replaced with the `"Id"` obtained from the first request.

### JSONPath Syntax for `returns`

- You can use JSONPath to specify which part of the response should be stored.

- For example, `"JSON-Path-var": "[-1:].FullName"` would select the `FullName` of the last item in a list.

- You can read more about JSONPAth [here](https://support.smartbear.com/alertsite/docs/monitors/api/endpoint/jsonpath.html)

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
      "returns": {
        "userCount": "$.length"
      }
    },
    {
      "method": "GET",
      "endpoint": "/api/orders",
      "returns": {
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

### Authentication

The solution supports authentication using either basic auth, JWT token auth or Windows authentication. The exact security model supported depends on whether each of the security models is configured.

In order to enable JWT token based authentication for API endpoints, the following values need to be configured:

```text
  "JwtSettings": {
    "Key": "[secret_key]",
    "Issuer": "[host_name]",
    "Audience": "[host_name]",
    "Subject": "JWTServiceAccessToken"
  },
```

If above section is configured, only users with a valid JWT token will be permitted to use the API endpoints.

In order to enable BASIC authentication for exposed API endpoints, the following values need to be configured:

```text
  "BasicAuthSettings": {
    "Username": "[user_name]",
    "Password": "[password]"
  },
```

If above section is configured, only users with a valid combination of username and password will be permitted to use the API endpoints.

In order to enable WINDOWS authentication, the following value needs to be set to true.

```text
  "NTLMAuthentication": true,
```

And the IIS has to be configured to use 'Windows Authentication' as well.

If none of the sections (`JwtSettings` or `BasicAuthSettings` or `NTLMAuthentication`) are provided, the exposed endpoints will require no authentication.

You need to substitute tokens denoted by square brackets with actual values (without square brackets).

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

### Tables

The database tables need to be specified inside the file `tablesettings.json`. There, you can pick what action is permitted for each table separately. 
Additionally, you can also specify which Active Directory roles have access to each action.

Here is an example:
```json
{
  "Database": {
    "Tables": {
      "TestTable": [ "select", "insert" ],
      "TestTableAD": {
        "select": [ "*" ],
        "update": [ "Role1", "Role2" ],
        "delete": [ "Role1" ],
        "insert": [ "Role1", "Role2", "Role3" ]
      }
    }
  }
}
```
The asterisk `*` means that every role is permitted for that action.

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
