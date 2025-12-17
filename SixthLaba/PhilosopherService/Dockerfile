FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ["PhilosopherService/PhilosopherService.csproj", "PhilosopherService/"]
COPY ["Philosophers.Shared/Philosophers.Shared.csproj", "Philosophers.Shared/"]

RUN dotnet restore "PhilosopherService/PhilosopherService.csproj"

COPY PhilosopherService/. ./PhilosopherService/
COPY Philosophers.Shared/. ./Philosophers.Shared/

WORKDIR /app/PhilosopherService
RUN dotnet publish "PhilosopherService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PhilosopherService.dll"]
