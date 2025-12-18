FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["CoordinatorService/CoordinatorService.csproj", "CoordinatorService/"]
COPY ["Philosophers.Shared/Philosophers.Shared.csproj", "Philosophers.Shared/"]

RUN dotnet restore "CoordinatorService/CoordinatorService.csproj"

COPY CoordinatorService/. ./CoordinatorService/
COPY Philosophers.Shared/. ./Philosophers.Shared/

WORKDIR /app/CoordinatorService
RUN dotnet publish "CoordinatorService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN apt-get update || true \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "CoordinatorService.dll"]
