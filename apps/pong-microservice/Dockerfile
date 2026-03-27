FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files first for layer caching
COPY ["apps/pong-microservice/src/PongService.csproj", "apps/pong-microservice/src/"]
COPY ["packages/MessageClient/MessageClient.csproj", "packages/MessageClient/"]
COPY ["packages/Shared.Contracts/Shared.Contracts.csproj", "packages/Shared.Contracts/"]
RUN dotnet restore "apps/pong-microservice/src/PongService.csproj"

# Copy source — build context must be solution root
COPY ["apps/pong-microservice/src/", "apps/pong-microservice/src/"]
COPY ["packages/MessageClient/", "packages/MessageClient/"]
COPY ["packages/Shared.Contracts/", "packages/Shared.Contracts/"]

WORKDIR "/src/apps/pong-microservice/src"
RUN dotnet publish "./PongMicroservice.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PongMicroservice.dll"]