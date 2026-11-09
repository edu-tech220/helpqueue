FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/HelpQueue.Api/HelpQueue.Api.csproj src/HelpQueue.Api/
RUN dotnet restore src/HelpQueue.Api/HelpQueue.Api.csproj

COPY src/HelpQueue.Api/ src/HelpQueue.Api/
RUN dotnet publish src/HelpQueue.Api/HelpQueue.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HelpQueue.Api.dll"]
