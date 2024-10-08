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

example of a composite of three API methods:

- First is a `GET` method, obtaining `name` variable from the `FullName` property of the last element of the returned JSON object, as well as the `number` parameter from the "PhoneNumber" of the 16th element of the returned JSON object.
- Second is a `POST` method creating new record using the `number` and `name` variables to create a new record in the `tablename` table and returning the new record object, as well as `Id` value of a new record and assigning it to `newId` variable.
- Third is a `DELETE` method removing newly created record using the `newId` variable as an input parameter used in the method path in order to perform delete operation on the underlying table for the newly created record. 

```json
{
  "requests": [
    {
      "method": "get",
      "endpoint": "https://localhost/api/tables/tablename",
      "returns": {
        "name": "[-1:].FullName",
        "number": "[16].PhoneNumber"
      },
      {
        "method": "post",
        "endpoint": "https://localhost/api/tables/tablename",
        "parameters": {
          "phone": "{number}",
          "fullname": "{name} - edited"
        },
        "returns": {
          "new": "$",
          "newId": "Id"
        }
      },
      {
        "method": "delete",
        "endpoint": "https://localhost/api/tables/tablename/{newId}"
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

### Security

The solution supports authentication using either basic auth or JWT token auth. The exact security model supported depends on whether each of the security models is configured.

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

If none of the sections (`JwtSettings` or `BasicAuthSettings`) are provided, the exposed endpoints will require no authentication.

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
