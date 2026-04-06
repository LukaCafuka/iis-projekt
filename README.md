# IIS projekt

Interoperability demo project built with ASP.NET Core and Blazor Server.

This solution demonstrates the same domain through multiple technologies:
- REST API
- GraphQL
- SOAP
- gRPC
- XML and JSON schema validation
- JWT authentication with role-based authorization

## Solution structure

- src/IIS.Api: backend API (REST, GraphQL, SOAP, gRPC, auth, validation)
- src/IIS.Client: Blazor Server frontend
- src/IIS.Contracts: shared gRPC contracts (Protobuf)

## Prerequisites

- .NET SDK 10.0 (project targets net10.0)
- Windows, Linux, or macOS

## Ports

The API uses fixed localhost ports:
- 5136: HTTP/1.1 (REST, GraphQL, SOAP)
- 5137: HTTP/2 cleartext (gRPC)

The client runs on:
- 5147: Blazor Server UI

## Run the project

Open two terminals in the repository root.

1. Start API

~~~powershell
dotnet run --project src/IIS.Api/IIS.Api.csproj
~~~

2. Start client

~~~powershell
dotnet run --project src/IIS.Client/IIS.Client.csproj
~~~

Then open:
- API Swagger: http://localhost:5136/swagger
- Client app: http://localhost:5147

## First startup behavior

On API startup, the app:
- Applies Entity Framework Core migrations
- Seeds roles: Reader and Full
- Seeds users:
	- reader@iis.local / Reader123!
	- full@iis.local / Full123!

## Authentication and roles

Auth endpoints:
- POST /api/auth/register
- POST /api/auth/login
- POST /api/auth/refresh

Role permissions:
- Reader
	- Can read tasks (REST + GraphQL queries)
- Full
	- Can create/update/delete tasks
	- Can use import, validation, SOAP, and gRPC features

## Main features

### REST tasks

- Endpoint base: /api/tasks
- Supports GET, POST, PUT, DELETE
- Controlled by server-side provider switch:
	- Custom: local SQLite via EF Core
	- Public: external MockAPI

Provider config endpoint:
- GET /api/config/task-api

### GraphQL

- Endpoint: /graphql
- Queries for Reader and Full
- Mutations for Full only

### SOAP

- Endpoint: /soap/TaskSearch.svc
- Full role required
- Flow:
	- Loads tasks from public API
	- Builds XML
	- Validates XML against XSD
	- Filters with XPath

### gRPC weather

- Service from IIS.Contracts/Protos/weather.proto
- Endpoint uses port 5137 (HTTP/2 cleartext)
- Full role required
- Fetches weather XML from DHMZ and returns matching cities

### XML and JSON validation/import

Validation endpoints:
- POST /api/validation/xml
- POST /api/validation/json

Import endpoints:
- POST /api/tasks/import/xml
- POST /api/tasks/import/xml/upload
- POST /api/tasks/import/json
- POST /api/tasks/import/json/upload

Schemas:
- src/IIS.Api/Schemas/tasks.xsd
- src/IIS.Api/Schemas/tasks-import.schema.json

Sample payloads:
- samples/tasks-import-valid.xml
- samples/tasks-import-valid.json
- samples/tasks-import-single-object.json

## Configuration

Main API settings are in:
- src/IIS.Api/appsettings.json

Important keys:
- ConnectionStrings:Default
- TaskApi:Provider (Custom or Public)
- TaskApi:PublicBaseUrl
- Jwt:Key
- Jwt:Issuer
- Jwt:Audience
- Cors:Origins

Client settings are in:
- src/IIS.Client/appsettings.json

Important keys:
- Api:BaseUrl
- Api:GrpcUrl

## Useful scripts

- scripts/check-api-ports.ps1
	- Prevents building API if dev ports are already occupied
- scripts/build-api.ps1
	- Frees API dev ports and builds IIS.Api

## Quick classroom demo flow

1. Login as Reader and show:
	 - GET tasks works
	 - POST task is forbidden
2. Login as Full and show:
	 - REST create/update/delete
	 - GraphQL query and mutation
	 - JSON/XML validation
	 - JSON/XML import
	 - SOAP search
	 - gRPC weather
3. Show refresh token call via /api/auth/refresh

## Troubleshooting

- Build fails because API is still running:
	- Stop running API process or run:

~~~powershell
dotnet build -p:SkipApiRunningCheck=true src/IIS.Api/IIS.Api.csproj
~~~

- gRPC call issues:
	- Ensure API is running on port 5137
	- Ensure client points Api:GrpcUrl to http://localhost:5137

- 401 or 403 responses:
	- Verify login succeeded
	- Verify role (Reader vs Full)
	- Verify Authorization Bearer token is being sent
