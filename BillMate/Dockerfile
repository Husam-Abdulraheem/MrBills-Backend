FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["BillMate.csproj", "."]
RUN dotnet restore "BillMate.csproj" --no-cache

COPY . .

RUN dotnet build "BillMate.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "BillMate.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Optional Traefik labels (configure with your own reverse proxy domain)
# LABEL traefik.enable="true"
# LABEL traefik.http.routers.billmate-api.rule="Host(`api.yourdomain.com`)"
# LABEL traefik.http.routers.billmate-api.entrypoints="websecure"
# LABEL traefik.http.routers.billmate-api.tls.certresolver="letsencrypt"
# LABEL traefik.http.services.billmate-api.loadbalancer.server.port="3000"

ENTRYPOINT ["dotnet", "BillMate.dll"]
