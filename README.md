# HelpQueue

IT support ticketing system built with ASP.NET Core 8, EF Core and SQL Server, split into a Web API
microservice and a Razor Pages frontend.

> Work in progress — full documentation lands in a later commit.

## Solution layout

- `src/HelpQueue.Api` — Web API microservice (business rules, persistence, auth).
- `src/HelpQueue.Web` — Razor Pages frontend, consumes the API via `IHttpClientFactory`.
- `tests/HelpQueue.Api.Tests` — xUnit test suite for the API.

## Run locally

```bash
dotnet build HelpQueue.sln
dotnet test HelpQueue.sln
```
