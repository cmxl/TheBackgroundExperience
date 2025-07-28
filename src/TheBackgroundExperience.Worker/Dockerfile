FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/TheBackgroundExperience.Worker/TheBackgroundExperience.Worker.csproj", "src/TheBackgroundExperience.Worker/"]
COPY ["src/TheBackgroundExperience.Application/TheBackgroundExperience.Application.csproj", "src/TheBackgroundExperience.Application/"]
COPY ["src/TheBackgroundExperience.Domain/TheBackgroundExperience.Domain.csproj", "src/TheBackgroundExperience.Domain/"]
COPY ["src/TheBackgroundExperience.Infrastructure/TheBackgroundExperience.Infrastructure.csproj", "src/TheBackgroundExperience.Infrastructure/"]
RUN dotnet restore "src/TheBackgroundExperience.Worker/TheBackgroundExperience.Worker.csproj"
COPY . .
WORKDIR "/src/src/TheBackgroundExperience.Worker"
RUN dotnet build "./TheBackgroundExperience.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TheBackgroundExperience.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TheBackgroundExperience.Worker.dll"]
