FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config .
COPY BloggingSystem.sln .

COPY src/Shared/Shared.Domain/Shared.Domain.csproj src/Shared/Shared.Domain/
COPY src/Shared/Shared.Application/Shared.Application.csproj src/Shared/Shared.Application/
COPY src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj src/Shared/Shared.Infrastructure/
COPY src/Modules/Authors/Authors.Contracts/Authors.Contracts.csproj src/Modules/Authors/Authors.Contracts/
COPY src/Modules/Posts/Posts.Domain/Posts.Domain.csproj src/Modules/Posts/Posts.Domain/
COPY src/Modules/Posts/Posts.Application/Posts.Application.csproj src/Modules/Posts/Posts.Application/
COPY src/Modules/Posts/Posts.Infrastructure/Posts.Infrastructure.csproj src/Modules/Posts/Posts.Infrastructure/
COPY src/Modules/Posts/Posts.Api/Posts.Api.csproj src/Modules/Posts/Posts.Api/

RUN dotnet restore src/Modules/Posts/Posts.Api/Posts.Api.csproj

COPY src/ src/

RUN dotnet publish src/Modules/Posts/Posts.Api/Posts.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8082
ENV ASPNETCORE_URLS=http://+:8082
ENTRYPOINT ["dotnet", "Posts.Api.dll"]
