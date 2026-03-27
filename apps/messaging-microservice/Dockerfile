FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["nuget.config", "nuget.config"]
COPY ["apps/messaging-microservice/src/MessagingMicroservice.csproj", "apps/messaging-microservice/src/"]
COPY ["packages/MessageClient/MessageClient.csproj", "packages/MessageClient/"]
COPY ["packages/Shared.Contracts/Shared.Contracts.csproj", "packages/Shared.Contracts/"]
RUN dotnet restore "apps/messaging-microservice/src/MessagingMicroservice.csproj"

COPY ["apps/messaging-microservice/src/", "apps/messaging-microservice/src/"]
COPY ["packages/MessageClient/", "packages/MessageClient/"]
COPY ["packages/Shared.Contracts/", "packages/Shared.Contracts/"]
WORKDIR "apps/messaging-microservice/src/"
RUN dotnet build "./MessagingMicroservice.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MessagingMicroservice.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessagingMicroservice.dll"]
