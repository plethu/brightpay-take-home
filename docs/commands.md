# Commands

Use `just` as the primary task runner.

| Command | Purpose |
| --- | --- |
| `just check` | Full quality gate: unit tests, component tests, containerized E2E, formatting, and infrastructure validation. |
| `just check-host` | Faster host-only gate: unit tests, component tests, formatting. |
| `just test` | All tests, including containerized E2E. |
| `just test-unit` | Unit tests only. |
| `just test-components` | bUnit component tests only. |
| `just test-e2e` | Containerized Playwright E2E tests. |
| `just test-e2e-host` | E2E tests against an already running app at `E2E_BASE_URL`. |
| `just db-up` | Start the SQL Server Compose service used by the app and host database commands. |
| `just db-update` | Apply EF Core checkout catalogue migrations to `ConnectionStrings__CheckoutDatabase`. |
| `just db-script` | Generate an idempotent SQL script for the checkout catalogue migrations. |
| `just fmt` | Verify formatting. |
| `just fmt-fix` | Apply formatting. |
| `just infra-check` | Validate OpenTofu formatting and provider configuration. |
| `just infra-fmt` | Verify OpenTofu formatting. |
| `just infra-fmt-fix` | Apply OpenTofu formatting. |
| `just infra-validate` | Run `tofu init -backend=false` and `tofu validate`. |
| `just run` | Alias for the containerized dev app. |
| `just up` | Start the Compose dev app with `dotnet watch`. |
| `just up-detached` | Start the containerized dev app in the background. |
| `just down` | Stop the Compose app and database containers. |
| `just restart` | Restart the dev app container. |
| `just logs` | Follow the dev app container logs. |
| `just shell` | Open a shell in the running dev app container. |
| `just ps` | Show dev app container status. |
| `just clean` | Clean .NET outputs and ignored build/test artifacts. |
| `just outdated` | Check NuGet package freshness. |

`just clean` uses `git clean -fdX` with exclusions for `.env`, local .NET/NuGet
caches, and common editor folders. It is intended for generated artifacts, not
for source cleanup.

Database commands use `dotnet-ef` from the local tool manifest. The host
connection string comes from `.env` or the shell as
`ConnectionStrings__CheckoutDatabase`; override it when applying migrations
elsewhere.
