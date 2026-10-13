# SmartX.Client (MVC dashboard) — multi-stage Docker build
# REFERENCE: Microsoft, 2026. Docker containers in ASP.NET Core.
# [online] Available at:
# https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-aspnet-apps
# [Accessed 13 October 2026].

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SmartX.Client/SmartX.Client.csproj SmartX.Client/
RUN dotnet restore SmartX.Client/SmartX.Client.csproj

COPY SmartX.Client/ SmartX.Client/
RUN dotnet publish SmartX.Client/SmartX.Client.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
# Compose overrides this to http://api:8080 — the default keeps local runs working.
ENV ApiSettings__BaseUrl=http://localhost:5042
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartX.Client.dll"]
