# syntax=docker/dockerfile:1

# Build stage. The SDK tag matches global.json (rollForward is disabled).
FROM mcr.microsoft.com/dotnet/sdk:10.0.203 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

# Warm the package cache against just the manifests so this layer is reused
# across source-only changes. Central package management and analyzer
# configuration need the root files present.
COPY .editorconfig global.json NuGet.Config Directory.Build.props Directory.Packages.props ./
COPY src/BrightPay.TakeHome.Core/BrightPay.TakeHome.Core.csproj src/BrightPay.TakeHome.Core/
COPY src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj src/BrightPay.TakeHome.Web/
RUN dotnet restore src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj

# Publish WITHOUT --no-restore. The Blazor framework static web assets
# (Microsoft.AspNetCore.App.Internal.Assets, which carries _framework/blazor.web.js)
# are only pulled into restore once the .razor source is present, which it is not
# during the manifest-only restore above. A manifest-only restore + publish
# --no-restore silently drops blazor.web.js, leaving the interactive circuit dead.
# Letting publish restore again here is near-free (the warm layer cached the rest).
COPY src/ src/
RUN dotnet publish src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj \
    --configuration Release \
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
