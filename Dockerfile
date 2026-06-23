# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props BrightPay.TakeHome.slnx ./
COPY src/BrightPay.TakeHome.Core/BrightPay.TakeHome.Core.csproj src/BrightPay.TakeHome.Core/
COPY src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj src/BrightPay.TakeHome.Web/
COPY tests/BrightPay.TakeHome.Tests.Unit/BrightPay.TakeHome.Tests.Unit.csproj tests/BrightPay.TakeHome.Tests.Unit/
COPY tests/BrightPay.TakeHome.Tests.Components/BrightPay.TakeHome.Tests.Components.csproj tests/BrightPay.TakeHome.Tests.Components/
COPY tests/BrightPay.TakeHome.Tests.E2E/BrightPay.TakeHome.Tests.E2E.csproj tests/BrightPay.TakeHome.Tests.E2E/
COPY tools/BrightPay.TakeHome.Tooling/BrightPay.TakeHome.Tooling.csproj tools/BrightPay.TakeHome.Tooling/
RUN --mount=type=cache,target=/src/.nuget/packages dotnet restore BrightPay.TakeHome.slnx

COPY . .
RUN --mount=type=cache,target=/src/.nuget/packages dotnet publish src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BrightPay.TakeHome.Web.dll"]
