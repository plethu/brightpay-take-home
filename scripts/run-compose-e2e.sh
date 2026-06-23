#!/usr/bin/env bash
set -euo pipefail

export DOTNET_ROOT="${DOTNET_ROOT:-/opt/dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export DOTNET_NOLOGO="${DOTNET_NOLOGO:-1}"

install_script="/tmp/dotnet-install.sh"
curl --fail --silent --show-error --location https://dot.net/v1/dotnet-install.sh --output "$install_script"
bash "$install_script" --jsonfile global.json --install-dir "$DOTNET_ROOT"
rm "$install_script"

dotnet restore BrightPay.TakeHome.slnx
dotnet build BrightPay.TakeHome.slnx --configuration Release --no-restore
dotnet publish src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj \
    --configuration Release \
    --no-build \
    --output /tmp/brightpay-publish

E2E_APP_DLL=/tmp/brightpay-publish/BrightPay.TakeHome.Web.dll ./scripts/run-e2e.sh
