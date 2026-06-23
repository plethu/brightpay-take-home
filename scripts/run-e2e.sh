#!/usr/bin/env bash
set -euo pipefail

url="${E2E_BASE_URL:-http://127.0.0.1:5087}"
export DisableHttpsRedirection="${DisableHttpsRedirection:-true}"

if [[ -n "${E2E_APP_DLL:-}" ]]; then
    dotnet "$E2E_APP_DLL" --urls "$url" &
else
    dotnet run \
        --project src/BrightPay.TakeHome.Web/BrightPay.TakeHome.Web.csproj \
        --configuration Release \
        --no-build \
        --no-launch-profile \
        --urls "$url" &
fi
server_pid="$!"

cleanup() {
    kill "$server_pid" 2>/dev/null || true
}
trap cleanup EXIT

for _ in {1..60}; do
    if curl --fail --silent --show-error "$url/cart" | grep -q "Product A"; then
        echo "Running E2E smoke tests against $url"
        results_dir="/tmp/brightpay-e2e-results"
        rm -rf "$results_dir"
        mkdir -p "$results_dir"

        E2E_BASE_URL="$url" dotnet test \
            tests/BrightPay.TakeHome.Tests.E2E/BrightPay.TakeHome.Tests.E2E.csproj \
            --configuration Release \
            --no-build \
            --filter "Category=E2E" \
            --logger "console;verbosity=normal" \
            --logger "trx;LogFileName=e2e.trx" \
            --results-directory "$results_dir"

        passed_count="$(grep -o 'outcome="Passed"' "$results_dir/e2e.trx" | wc -l)"
        echo "E2E smoke tests completed: $passed_count passed"
        exit 0
    fi

    sleep 1
done

echo "Timed out waiting for $url" >&2
exit 1
