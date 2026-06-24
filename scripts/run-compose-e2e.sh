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

url="${E2E_BASE_URL:-http://checkout-web:8080}"

for _ in {1..60}; do
    if curl --fail --silent --show-error "$url/" >/dev/null; then
        echo "Running E2E smoke tests against $url"
        results_dir="/tmp/brightpay-e2e-results"
        rm -rf "$results_dir"
        mkdir -p "$results_dir"

        E2E_BASE_URL="$url" dotnet test \
            tests/BrightPay.TakeHome.Tests.E2E/BrightPay.TakeHome.Tests.E2E.csproj \
            --configuration Release \
            --filter "Category=E2E" \
            --logger "console;verbosity=normal" \
            --logger "trx;LogFileName=e2e.trx" \
            --results-directory "$results_dir"

        passed_count="$(grep -o 'outcome="Passed"' "$results_dir/e2e.trx" | wc -l)"
        echo "E2E smoke tests completed: $passed_count passed"

        echo "Running Lighthouse CI against $url"
        corepack prepare pnpm@10.34.3 --activate
        corepack pnpm install --frozen-lockfile
        LHCI_BASE_URL="$url" corepack pnpm exec lhci autorun --config=./lighthouserc.cjs
        exit 0
    fi

    sleep 1
done

echo "Timed out waiting for $url" >&2
exit 1
