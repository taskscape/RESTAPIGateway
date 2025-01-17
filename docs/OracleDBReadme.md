# Oracle Database Setup and Configuration

## Prerequisites
Before proceeding, ensure you have the following installed:
- [Docker](https://www.docker.com/get-started)

## 1. Run Oracle Database in Docker
Execute the following command in your terminal or command prompt to start an Oracle database container:

```sh
docker run -d --name oracle-db -p 1521:1521 -p 5500:5500 -e ORACLE_PASSWORD=oracle123 gvenzl/oracle-free
```

## 2. Verify the Container is Running
Check if the Oracle database container is running with:

```sh
docker ps
```

If the container is not listed, ensure Docker is running and retry the command.

## 3. Connection to Oracle Database
You can connect to the database using an SQL client such as SQL*Plus or DBeaver. Use the following credentials:

- **Username:** `SYSTEM`
- **Password:** `oracle123`
- **Host:** `localhost`
- **Port:** `1521`

## 4. Run the Table Creation Script
Before executing this step, ensure you are in the same directory as the `init.sql` file.

Execute the following commands to copy and run the script inside the Oracle container:

```sh
docker cp init.sql oracle-db:/tmp/init.sql
docker exec -it oracle-db sqlplus SYSTEM/oracle123@//localhost:1521 @/tmp/init.sql
```

## 5. Configure Database Connection in Application
Set the `DefaultConnection` in the `appsettings.json` file as follows:

```json
"ConnectionStrings": {
  "DefaultConnection": "User Id=SYSTEM;Password=oracle123;Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1521)))"
}
```

## 6. Run the GenericTableAPI Project
Once the database is configured, start the `GenericTableAPI` project to interact with the database.