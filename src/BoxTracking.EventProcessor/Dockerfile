FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY ../BoxTracking.Shared/BoxTracking.Shared.csproj BoxTracking.Shared/
COPY ../BoxTracking.Shared/ BoxTracking.Shared/

# Copy processor project
COPY BoxTracking.EventProcessor.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BoxTracking.EventProcessor.dll"]
