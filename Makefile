.DEFAULT_GOAL := list

JUST ?= just

.PHONY: list check restore build test test-unit test-components test-e2e test-e2e-host fmt fmt-fix infra-init infra-fmt infra-fmt-fix infra-validate infra-check run up up-detached down restart logs shell shell-web shell-db ps clean outdated

list:
	@command -v $(JUST) >/dev/null || { echo "just is required. Run: mise install"; exit 127; }
	@$(JUST) --list

check restore build test test-unit test-components test-e2e test-e2e-host fmt fmt-fix infra-init infra-fmt infra-fmt-fix infra-validate infra-check run up up-detached down restart logs shell shell-web shell-db ps clean outdated:
	@command -v $(JUST) >/dev/null || { echo "just is required. Run: mise install"; exit 127; }
	@$(JUST) $@
