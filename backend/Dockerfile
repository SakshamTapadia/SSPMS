# ── Build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY src/SSPMS.Domain/SSPMS.Domain.csproj             src/SSPMS.Domain/
COPY src/SSPMS.Application/SSPMS.Application.csproj   src/SSPMS.Application/
COPY src/SSPMS.Infrastructure/SSPMS.Infrastructure.csproj src/SSPMS.Infrastructure/
COPY src/SSPMS.API/SSPMS.API.csproj                   src/SSPMS.API/

RUN dotnet restore src/SSPMS.API/SSPMS.API.csproj

COPY src/ src/

RUN dotnet publish src/SSPMS.API/SSPMS.API.csproj \
    --no-restore -c Release -o /app/publish

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SSPMS.API.dll"]
