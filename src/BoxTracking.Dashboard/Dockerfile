FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY src/BoxTracking.Shared/BoxTracking.Shared.csproj BoxTracking.Shared/
COPY src/BoxTracking.Shared/ BoxTracking.Shared/

# Copy dashboard project
COPY src/BoxTracking.Dashboard/BoxTracking.Dashboard.csproj BoxTracking.Dashboard/
RUN dotnet restore BoxTracking.Dashboard/BoxTracking.Dashboard.csproj

COPY src/BoxTracking.Dashboard/ BoxTracking.Dashboard/
RUN dotnet publish BoxTracking.Dashboard/BoxTracking.Dashboard.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BoxTracking.Dashboard.dll"]
