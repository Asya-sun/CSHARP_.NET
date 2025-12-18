# ---------- BUILD ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Копируем csproj вместе с зависимым проектом
COPY ["TableService/TableService.csproj", "TableService/"]
COPY ["Philosophers.Shared/Philosophers.Shared.csproj", "Philosophers.Shared/"]

# Восстанавливаем зависимости (путь к проектам должен совпадать с csproj)
RUN dotnet restore "TableService/TableService.csproj"

# Копируем исходники
COPY TableService/. ./TableService/
COPY Philosophers.Shared/. ./Philosophers.Shared/

# Публикуем
WORKDIR /app/TableService
RUN dotnet publish "TableService.csproj" -c Release -o /app/publish

# ---------- RUNTIME ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update || true \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "TableService.dll"]
