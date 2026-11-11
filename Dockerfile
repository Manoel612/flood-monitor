# Imagem única: o ASP.NET Core serve a API e o build do Vue (wwwroot).
#   docker build -t flood-monitor .
#   docker run --env-file .env.production -p 8080:8080 -v flood-data:/app/App_Data flood-monitor

# 1) Interface (Vue + Vite) → FloodMonitor.Server/wwwroot
FROM node:22-alpine AS client
WORKDIR /src/FloodMonitor.Client
COPY FloodMonitor.Client/package.json FloodMonitor.Client/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY FloodMonitor.Client/ ./
RUN npm run build


# 2) API (.NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS server
WORKDIR /src/FloodMonitor.Server
COPY FloodMonitor.Server/FloodMonitor.Server.csproj ./
RUN dotnet restore
COPY FloodMonitor.Server/ ./
COPY --from=client /src/FloodMonitor.Server/wwwroot ./wwwroot
RUN dotnet publish -c Release -o /app --no-restore

# 3) Execução
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=server /app ./
# Banco SQLite e fotos ficam aqui: monte um volume persistente neste caminho
RUN mkdir -p /app/App_Data && chown "$APP_UID" /app/App_Data
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "FloodMonitor.Server.dll"]
