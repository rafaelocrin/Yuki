FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY NuGet.Config .
COPY BloggingSystem.sln .
COPY src/Gateway/Gateway.csproj src/Gateway/

RUN dotnet restore src/Gateway/Gateway.csproj

COPY src/Gateway/ src/Gateway/

RUN dotnet publish src/Gateway/Gateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081
ENTRYPOINT ["dotnet", "Gateway.dll"]
