FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY src/BoxTracking.Shared/BoxTracking.Shared.csproj BoxTracking.Shared/
COPY src/BoxTracking.Shared/ BoxTracking.Shared/

# Copy API project
COPY src/BoxTracking.Api/BoxTracking.Api.csproj BoxTracking.Api/
RUN dotnet restore BoxTracking.Api/BoxTracking.Api.csproj

COPY src/BoxTracking.Api/ BoxTracking.Api/
RUN dotnet publish BoxTracking.Api/BoxTracking.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BoxTracking.Api.dll"]
