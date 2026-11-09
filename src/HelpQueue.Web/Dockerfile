FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/HelpQueue.Web/HelpQueue.Web.csproj src/HelpQueue.Web/
RUN dotnet restore src/HelpQueue.Web/HelpQueue.Web.csproj

COPY src/HelpQueue.Web/ src/HelpQueue.Web/
RUN dotnet publish src/HelpQueue.Web/HelpQueue.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HelpQueue.Web.dll"]
