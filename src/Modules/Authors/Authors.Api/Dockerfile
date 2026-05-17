FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config .
COPY BloggingSystem.sln .

COPY src/Shared/Shared.Domain/Shared.Domain.csproj src/Shared/Shared.Domain/
COPY src/Shared/Shared.Application/Shared.Application.csproj src/Shared/Shared.Application/
COPY src/Shared/Shared.Infrastructure/Shared.Infrastructure.csproj src/Shared/Shared.Infrastructure/
COPY src/Modules/Authors/Authors.Contracts/Authors.Contracts.csproj src/Modules/Authors/Authors.Contracts/
COPY src/Modules/Authors/Authors.Domain/Authors.Domain.csproj src/Modules/Authors/Authors.Domain/
COPY src/Modules/Authors/Authors.Application/Authors.Application.csproj src/Modules/Authors/Authors.Application/
COPY src/Modules/Authors/Authors.Infrastructure/Authors.Infrastructure.csproj src/Modules/Authors/Authors.Infrastructure/
COPY src/Modules/Authors/Authors.Api/Authors.Api.csproj src/Modules/Authors/Authors.Api/

RUN dotnet restore src/Modules/Authors/Authors.Api/Authors.Api.csproj

COPY src/ src/

RUN dotnet publish src/Modules/Authors/Authors.Api/Authors.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8083
ENV ASPNETCORE_URLS=http://+:8083
ENTRYPOINT ["dotnet", "Authors.Api.dll"]
