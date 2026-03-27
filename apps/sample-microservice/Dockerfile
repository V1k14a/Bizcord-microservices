FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["apps/sample-microservice/src/src.csproj", "apps/sample-microservice/src/"]
COPY ["packages/MessageClient/MessageClient.csproj", "packages/MessageClient/"]
RUN dotnet restore "apps/sample-microservice/src/src.csproj"

WORKDIR "/src"
COPY ["apps/sample-microservice/src/", "apps/sample-microservice/src/"]
COPY ["packages/MessageClient/", "packages/MessageClient/"]
WORKDIR "apps/sample-microservice/src/"
RUN dotnet build "./src.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./src.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleMicroservice.dll"]
