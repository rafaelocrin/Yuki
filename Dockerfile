FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BloggingSystem.sln .
COPY src/BloggingSystem.Domain/BloggingSystem.Domain.csproj src/BloggingSystem.Domain/
COPY src/BloggingSystem.Application/BloggingSystem.Application.csproj src/BloggingSystem.Application/
COPY src/BloggingSystem.Infrastructure/BloggingSystem.Infrastructure.csproj src/BloggingSystem.Infrastructure/
COPY src/BloggingSystem.Api/BloggingSystem.Api.csproj src/BloggingSystem.Api/

RUN dotnet restore src/BloggingSystem.Api/BloggingSystem.Api.csproj

COPY src/ src/

RUN dotnet publish src/BloggingSystem.Api/BloggingSystem.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "BloggingSystem.Api.dll"]
