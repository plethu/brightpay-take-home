function closestButton(element: EventTarget | null, selector: string): HTMLButtonElement | null {
    return element instanceof HTMLElement ? element.closest<HTMLButtonElement>(selector) : null;
}

function updateQuantity(stepButton: HTMLButtonElement): void {
    const form = stepButton.closest("form");
    const input = form?.querySelector<HTMLInputElement>("input[name='quantity']");
    if (!input) {
        return;
    }

    const step = Number(stepButton.dataset.qtyStep ?? "0");
    const current = Number(input.value || "1");
    input.value = String(Math.max(1, current + step));
    input.dispatchEvent(new Event("change", { bubbles: true }));
}

function wireClearDialog(form: HTMLFormElement): void {
    const dialog = document.querySelector<HTMLDialogElement>("[data-clear-dialog]");
    if (!dialog) {
        return;
    }

    form.addEventListener("submit", (event) => {
        if (form.dataset.confirmed === "true") {
            return;
        }

        event.preventDefault();
        if (typeof dialog.showModal === "function") {
            dialog.showModal();
            return;
        }

        form.dataset.confirmed = "true";
        form.requestSubmit();
    });

    dialog.addEventListener("close", () => {
        if (dialog.returnValue !== "clear") {
            return;
        }

        form.dataset.confirmed = "true";
        form.requestSubmit();
    });
}

document.addEventListener("click", (event) => {
    const stepButton = closestButton(event.target, "[data-qty-step]");
    if (stepButton) {
        updateQuantity(stepButton);
    }
});

document.querySelectorAll<HTMLFormElement>("[data-clear-form]").forEach(wireClearDialog);

export {};
