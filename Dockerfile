# syntax=docker/dockerfile:1

# Build stage. The SDK tag matches global.json (rollForward is disabled).
FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

# Restore first against just the manifests so the layer caches across source
# changes. Central package management and analyzer configuration need the root
# files present before publish.
COPY .editorconfig global.json Directory.Build.props Directory.Packages.props ./
COPY src/BrightPay.TakeHome.Core/BrightPay.TakeHome.Core.csproj src/BrightPay.TakeHome.Core/
COPY src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj src/BrightPay.TakeHome.Web/
RUN dotnet restore src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj

COPY src/ src/
RUN dotnet publish src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app

# Runtime stage.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./

# APP_UID is defined by the .NET base image; run as the non-root app user.
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "BrightPay.TakeHome.Web.dll"]
