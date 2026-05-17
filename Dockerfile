FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config .
COPY BloggingSystem.sln .

# Shared
COPY src/Shared/Shared.Domain/Shared.Domain.csproj src/Shared/Shared.Domain/
COPY src/Shared/Shared.Application/Shared.Application.csproj src/Shared/Shared.Application/
COPY src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj src/Shared/Shared.Infrastructure/

# Authors
COPY src/Modules/Authors/Authors.Contracts/Authors.Contracts.csproj src/Modules/Authors/Authors.Contracts/
COPY src/Modules/Authors/Authors.Domain/Authors.Domain.csproj src/Modules/Authors/Authors.Domain/
COPY src/Modules/Authors/Authors.Application/Authors.Application.csproj src/Modules/Authors/Authors.Application/
COPY src/Modules/Authors/Authors.Infrastructure/Authors.Infrastructure.csproj src/Modules/Authors/Authors.Infrastructure/

# Posts
COPY src/Modules/Posts/Posts.Domain/Posts.Domain.csproj src/Modules/Posts/Posts.Domain/
COPY src/Modules/Posts/Posts.Application/Posts.Application.csproj src/Modules/Posts/Posts.Application/
COPY src/Modules/Posts/Posts.Infrastructure/Posts.Infrastructure.csproj src/Modules/Posts/Posts.Infrastructure/

# API host
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
