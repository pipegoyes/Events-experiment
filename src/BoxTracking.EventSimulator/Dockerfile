FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/BoxTracking.EventSimulator/BoxTracking.EventSimulator.csproj", "src/BoxTracking.EventSimulator/"]
COPY ["src/BoxTracking.Shared/BoxTracking.Shared.csproj", "src/BoxTracking.Shared/"]
RUN dotnet restore "src/BoxTracking.EventSimulator/BoxTracking.EventSimulator.csproj"
COPY . .
WORKDIR "/src/src/BoxTracking.EventSimulator"
RUN dotnet build "BoxTracking.EventSimulator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BoxTracking.EventSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BoxTracking.EventSimulator.dll"]
