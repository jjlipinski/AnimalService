# AnimalService

A learning project demonstrating how to build a simple RESTful API service in .NET without relying on ASP.NET or other frameworks. This project uses pure C# with HttpListener to implement a basic animal tracking service with PostgreSQL database integration, providing a clear example of HTTP request handling, routing, and database operations from scratch.

## Project Overview

This application provides a REST API for tracking animals and their habitats. It demonstrates:

- Building a REST API without using frameworks like ASP.NET
- Custom routing implementation
- Database operations with PostgreSQL
- Basic CRUD operations
- Client-server architecture

## Database Requirements

This project requires a PostgreSQL database to be available with the following configuration:

- Host: localhost
- Port: 5432
- Username: postgres
- Password: postgres
- Database: animals_db

### Connection String

```
Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=animals_db
```

## Setup Instructions

1. Ensure PostgreSQL is installed and running
2. Create a database named `animals_db`
3. Run the SQL setup scripts located in the `Server` directory
4. Build and run the application:
   ```
   dotnet build
   dotnet run --project AnimalService
   ```
5. Access the application at `http://localhost:8080/`

## API Endpoints

- GET `/index` - Serves an HTML test page that allows you to test and exercise all API endpoints of the service
- GET `/animals` - Retrieves all animals
- GET `/animal/{id}` - Retrieves a specific animal by ID
- POST `/animals` - Adds a new animal
- DELETE `/animal` - Deletes an animal by ID

## Project Structure

- `AnimalService/` - Core C# code
- `Client/` - Web client files
- `Server/` - Database scripts and resources
