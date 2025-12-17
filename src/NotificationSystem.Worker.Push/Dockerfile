FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/NotificationSystem.Worker.Push/NotificationSystem.Worker.Push.csproj", "NotificationSystem.Worker.Push/"]
COPY ["src/NotificationSystem.Domain/NotificationSystem.Domain.csproj", "NotificationSystem.Domain/"]
COPY ["src/NotificationSystem.Shared/NotificationSystem.Shared.csproj", "NotificationSystem.Shared/"]
RUN dotnet restore "NotificationSystem.Worker.Push/NotificationSystem.Worker.Push.csproj"
COPY src/ .
WORKDIR "/src/NotificationSystem.Worker.Push"
RUN dotnet build "NotificationSystem.Worker.Push.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationSystem.Worker.Push.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationSystem.Worker.Push.dll"]
