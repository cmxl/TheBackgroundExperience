FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
EXPOSE 5001
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
COPY ["src/TheBackgroundExperience.NotificationsApi/TheBackgroundExperience.NotificationsApi.csproj", "src/TheBackgroundExperience.NotificationsApi/"]
COPY ["src/TheBackgroundExperience.Domain/TheBackgroundExperience.Domain.csproj", "src/TheBackgroundExperience.Domain/"]
COPY ["src/TheBackgroundExperience.Application/TheBackgroundExperience.Application.csproj", "src/TheBackgroundExperience.Application/"]
COPY ["src/TheBackgroundExperience.Infrastructure/TheBackgroundExperience.Infrastructure.csproj", "src/TheBackgroundExperience.Infrastructure/"]
RUN dotnet restore "./src/TheBackgroundExperience.NotificationsApi/TheBackgroundExperience.NotificationsApi.csproj"
COPY . .
WORKDIR "/src/src/TheBackgroundExperience.NotificationsApi"
RUN dotnet build "./TheBackgroundExperience.NotificationsApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TheBackgroundExperience.NotificationsApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "TheBackgroundExperience.NotificationsApi.dll"]