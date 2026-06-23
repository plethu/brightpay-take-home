.DEFAULT_GOAL := check

JUST ?= just

.PHONY: check restore build test test-unit test-components test-e2e test-e2e-host fmt fmt-fix infra-init infra-fmt infra-fmt-fix infra-validate infra-check run up up-detached down restart logs shell ps clean image-build outdated

check restore build test test-unit test-components test-e2e test-e2e-host fmt fmt-fix infra-init infra-fmt infra-fmt-fix infra-validate infra-check run up up-detached down restart logs shell ps clean image-build outdated:
	@command -v $(JUST) >/dev/null || { echo "just is required. Run: mise install"; exit 127; }
	@$(JUST) $@
