FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared project
COPY src/BoxTracking.Shared/BoxTracking.Shared.csproj BoxTracking.Shared/
COPY src/BoxTracking.Shared/ BoxTracking.Shared/

# Copy processor project
COPY src/BoxTracking.EventProcessor/BoxTracking.EventProcessor.csproj BoxTracking.EventProcessor/
RUN dotnet restore BoxTracking.EventProcessor/BoxTracking.EventProcessor.csproj

COPY src/BoxTracking.EventProcessor/ BoxTracking.EventProcessor/
RUN dotnet publish BoxTracking.EventProcessor/BoxTracking.EventProcessor.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BoxTracking.EventProcessor.dll"]
