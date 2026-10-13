# SmartX.API — multi-stage Docker build
# REFERENCE: Microsoft, 2026. Docker containers in ASP.NET Core.
# [online] Available at:
# https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/building-aspnet-apps
# [Accessed 13 October 2026].

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached between source changes.
COPY SmartX.API/SmartX.API.csproj SmartX.API/
RUN dotnet restore SmartX.API/SmartX.API.csproj

COPY SmartX.API/ SmartX.API/
RUN dotnet publish SmartX.API/SmartX.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# The container always listens on plain HTTP port 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SmartX.API.dll"]
