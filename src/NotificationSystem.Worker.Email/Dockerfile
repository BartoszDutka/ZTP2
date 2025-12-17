FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NotificationSystem.Worker.Email/NotificationSystem.Worker.Email.csproj", "NotificationSystem.Worker.Email/"]
COPY ["src/NotificationSystem.Domain/NotificationSystem.Domain.csproj", "NotificationSystem.Domain/"]
COPY ["src/NotificationSystem.Infrastructure/NotificationSystem.Infrastructure.csproj", "NotificationSystem.Infrastructure/"]
COPY ["src/NotificationSystem.Shared/NotificationSystem.Shared.csproj", "NotificationSystem.Shared/"]
RUN dotnet restore "NotificationSystem.Worker.Email/NotificationSystem.Worker.Email.csproj"
COPY src/ .
WORKDIR "/src/NotificationSystem.Worker.Email"
RUN dotnet build "NotificationSystem.Worker.Email.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationSystem.Worker.Email.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationSystem.Worker.Email.dll"]
