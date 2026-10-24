# Products API: Clean Architecture + CQRS (MediatR)

Double-click `run.bat` (needs .NET 8 SDK + SQL Server LocalDB). API: http://localhost:5080/api/products

## Endpoints
- POST /api/products        body: { "name": "Laptop", "price": 1200.50 }  -> 201
- GET  /api/products?page=1&pageSize=10                                   -> 200
- GET  /api/products/{id}                                                 -> 200 / 404

## Structure
- Products.Domain          Product.cs
- Products.Application     Abstractions.cs (interfaces), ProductFeatures.cs (DTO, commands, queries, handlers, validators, pipeline)
- Products.Infrastructure  Persistence/Persistence.cs (DbContext, EF config, repository, DI)
- Products.API             Program.cs (startup + error middleware), ProductsController.cs

Connection string: Products.API/appsettings.json (database `ProductsSlimDb`).
