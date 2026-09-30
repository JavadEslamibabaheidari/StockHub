FROM node:26-alpine AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY StockHub.sln ./
COPY backend/ ./backend/
RUN dotnet restore backend/src/StockHub.Api/StockHub.Api.csproj
RUN dotnet publish backend/src/StockHub.Api/StockHub.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install --no-install-recommends --yes curl libssl3t64 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=backend-build /app/publish ./
COPY --from=frontend-build /src/frontend/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=20s --retries=12 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1
USER $APP_UID
ENTRYPOINT ["dotnet", "StockHub.Api.dll"]
