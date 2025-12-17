FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NotificationSystem.Api/NotificationSystem.Api.csproj", "NotificationSystem.Api/"]
COPY ["src/NotificationSystem.Domain/NotificationSystem.Domain.csproj", "NotificationSystem.Domain/"]
COPY ["src/NotificationSystem.Infrastructure/NotificationSystem.Infrastructure.csproj", "NotificationSystem.Infrastructure/"]
COPY ["src/NotificationSystem.Shared/NotificationSystem.Shared.csproj", "NotificationSystem.Shared/"]
RUN dotnet restore "NotificationSystem.Api/NotificationSystem.Api.csproj"
COPY src/ .
WORKDIR "/src/NotificationSystem.Api"
RUN dotnet build "NotificationSystem.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationSystem.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationSystem.Api.dll"]
