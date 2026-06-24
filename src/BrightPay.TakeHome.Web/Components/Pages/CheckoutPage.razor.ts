function closestButton(element: EventTarget | null, selector: string): HTMLButtonElement | null {
    return element instanceof HTMLElement ? element.closest<HTMLButtonElement>(selector) : null;
}

const EASE = "cubic-bezier(0.2, 0, 0, 1)";

function reduceMotion(): boolean {
    return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function findQuantityInput(stepButton: HTMLButtonElement): HTMLInputElement | null {
    const form = stepButton.closest("form");
    return form?.querySelector<HTMLInputElement>("input[name='quantity']") ?? null;
}

function updateQuantityDisabledState(input: HTMLInputElement): void {
    const control = input.closest(".qty-control");
    const decrement = control?.querySelector<HTMLButtonElement>("[data-qty-step='-1']");
    if (!decrement) {
        return;
    }

    const current = Number(input.value || "1");
    decrement.disabled = current <= 1;
}

function updateQuantity(stepButton: HTMLButtonElement): void {
    const input = findQuantityInput(stepButton);
    if (!input) {
        return;
    }

    const step = Number(stepButton.dataset.qtyStep ?? "0");
    const current = Number(input.value || "1");
    input.value = String(Math.max(1, current + step));
    updateQuantityDisabledState(input);
    input.dispatchEvent(new Event("change", { bubbles: true }));
}

function pulse(element: Element | null): void {
    if (!element || reduceMotion()) {
        return;
    }

    if (element instanceof HTMLElement) {
        element.dataset.pulse = "true";
    }
    element.animate(
        [{ transform: "scale(1)" }, { transform: "scale(1.12)" }, { transform: "scale(1)" }],
        { duration: 240, easing: EASE },
    ).onfinish = () => {
        if (element instanceof HTMLElement) {
            delete element.dataset.pulse;
        }
    };
}

function animateLineIn(line: Element | null): void {
    if (!line || reduceMotion()) {
        return;
    }

    const height = line.getBoundingClientRect().height;
    if (line instanceof HTMLElement) {
        line.style.overflow = "hidden";
    }
    line.animate(
        [
            { blockSize: "0px", opacity: 0, transform: "translateX(-8px)" },
            { blockSize: `${height}px`, opacity: 1, transform: "none" },
        ],
        { duration: 240, easing: EASE },
    ).onfinish = () => {
        if (line instanceof HTMLElement) {
            line.style.overflow = "";
            line.style.blockSize = "";
        }
    };
}

type LineState = {
    quantity: string;
    offerActive: boolean;
    metaHtml: string;
    wasHtml: string;
    outerHtml: string;
};

const lineStates = new Map<string, LineState>();
let controller: AbortController | null = null;
let observer: MutationObserver | null = null;
let totalText = "";

function lineState(line: Element): LineState {
    return {
        quantity: line.getAttribute("data-quantity") ?? "",
        offerActive: line.getAttribute("data-offer-active") === "true",
        metaHtml: line.querySelector(".line-meta")?.innerHTML ?? "",
        wasHtml: line.querySelector(".line-was")?.outerHTML ?? "",
        outerHtml: line instanceof HTMLElement ? line.outerHTML : "",
    };
}

function captureLineStates(root: ParentNode = document): void {
    root.querySelectorAll(".sale-line[data-sku]").forEach((line) => {
        const sku = line.getAttribute("data-sku");
        if (sku) {
            lineStates.set(sku, lineState(line));
        }
    });
}

function animateOfferIn(line: Element): void {
    if (reduceMotion()) {
        return;
    }

    line.querySelectorAll(".line-meta, .line-was").forEach((element) => {
        element.animate(
            [
                { opacity: 0, transform: "translateY(-4px) scale(0.92)" },
                { opacity: 1, transform: "none" },
            ],
            { duration: 260, easing: EASE },
        );
    });
}

function animateOfferOut(line: Element, previous: LineState): void {
    if (reduceMotion()) {
        return;
    }

    const info = line.querySelector(".line-info");
    if (info && previous.metaHtml) {
        const metaGhost = document.createElement("p");
        metaGhost.className = "line-meta offer-exit-ghost";
        metaGhost.innerHTML = previous.metaHtml;
        info.append(metaGhost);
        collapseAndRemove(metaGhost);
    }

    const amounts = line.querySelector(".line-amounts");
    if (amounts && previous.wasHtml) {
        const holder = document.createElement("template");
        holder.innerHTML = previous.wasHtml;
        const wasGhost = holder.content.firstElementChild;
        if (wasGhost instanceof HTMLElement) {
            wasGhost.classList.add("offer-exit-ghost");
            amounts.append(wasGhost);
            collapseAndRemove(wasGhost);
        }
    }
}

function collapseAndRemove(element: HTMLElement): void {
    const height = element.getBoundingClientRect().height;
    element.style.overflow = "hidden";
    element.animate(
        [
            { opacity: 1, transform: "none", blockSize: `${height}px` },
            { opacity: 0, transform: "translateY(-4px) scale(0.92)", blockSize: "0px" },
        ],
        { duration: 200, easing: EASE },
    ).onfinish = () => element.remove();
}

function animateLineOut(line: HTMLElement): void {
    if (reduceMotion()) {
        line.remove();
        return;
    }

    const height = line.getBoundingClientRect().height;
    const style = getComputedStyle(line);
    line.style.overflow = "hidden";
    line.style.boxSizing = "border-box";
    line.style.blockSize = `${height}px`;
    line.animate(
        [
            {
                blockSize: `${height}px`,
                paddingBlockStart: style.paddingBlockStart,
                paddingBlockEnd: style.paddingBlockEnd,
                borderBlockEndWidth: style.borderBlockEndWidth,
                opacity: 1,
                transform: "none",
            },
            {
                blockSize: "0px",
                paddingBlockStart: "0px",
                paddingBlockEnd: "0px",
                borderBlockEndWidth: "0px",
                opacity: 0,
                transform: "translateX(-8px)",
            },
        ],
        { duration: 220, easing: EASE },
    ).onfinish = () => line.remove();
}

function animateRemovedLines(records: MutationRecord[]): void {
    const list = document.querySelector(".sale-list");
    if (!list) {
        return;
    }

    records.forEach((record) => {
        record.removedNodes.forEach((node) => {
            if (!(node instanceof HTMLElement)
                || !node.matches(".sale-line[data-sku]")
                || node.dataset.animationGhost === "true") {
                return;
            }

            const ghost = node.cloneNode(true);
            if (!(ghost instanceof HTMLElement)) {
                return;
            }

            ghost.dataset.animationGhost = "true";
            const next = record.nextSibling instanceof Node && record.nextSibling.parentNode === list
                ? record.nextSibling
                : null;
            list.insertBefore(ghost, next);
            animateLineOut(ghost);
        });
    });
}

function processLineChanges(root: ParentNode = document): void {
    root.querySelectorAll(".sale-line[data-sku]").forEach((line) => {
        const sku = line.getAttribute("data-sku");
        if (!sku) {
            return;
        }

        const previous = lineStates.get(sku);
        const current = lineState(line);
        if (!previous) {
            animateLineIn(line);
        } else {
            if (previous.quantity !== current.quantity) {
                pulse(line.querySelector(".line-qty"));
            }

            if (!previous.offerActive && current.offerActive) {
                animateOfferIn(line);
            }

            if (previous.offerActive && !current.offerActive) {
                animateOfferOut(line, previous);
            }
        }

        lineStates.set(sku, current);
    });
    for (const sku of [...lineStates.keys()]) {
        if (!root.querySelector(`[data-sku='${CSS.escape(sku)}']`)) {
            lineStates.delete(sku);
        }
    }
}

function observeCheckoutChanges(root: Element): void {
    const total = root.querySelector("[data-action='checkout'] .checkout-amount");
    if (!root) {
        return;
    }

    totalText = total?.textContent ?? "";
    captureLineStates(root);
    observer = new MutationObserver((records) => {
        animateRemovedLines(records);
        window.requestAnimationFrame(() => {
            processLineChanges(root);
            const currentTotal = root.querySelector("[data-action='checkout'] .checkout-amount");
            const nextTotal = currentTotal?.textContent ?? "";
            if (totalText !== nextTotal) {
                pulse(currentTotal);
                totalText = nextTotal;
            }
        });
    });
    observer.observe(root, { childList: true, subtree: true, attributes: true, attributeFilter: ["data-quantity", "data-offer-active"] });
}

function submitAfterLineOut(button: HTMLButtonElement): boolean {
    const form = button.form;
    const line = button.closest<HTMLElement>(".sale-line");
    const quantity = line?.querySelector<HTMLElement>(".line-qty")?.textContent?.trim();
    if (!form || !line || quantity !== "1" || form.dataset.animatedSubmit === "true") {
        return false;
    }

    form.dataset.animatedSubmit = "true";
    if (reduceMotion()) {
        form.requestSubmit();
        return true;
    }

    const height = line.getBoundingClientRect().height;
    const style = getComputedStyle(line);
    line.style.overflow = "hidden";
    line.style.boxSizing = "border-box";
    line.style.blockSize = `${height}px`;
    line.animate(
        [
            {
                blockSize: `${height}px`,
                paddingBlockStart: style.paddingBlockStart,
                paddingBlockEnd: style.paddingBlockEnd,
                opacity: 1,
                transform: "none",
            },
            {
                blockSize: "0px",
                paddingBlockStart: "0px",
                paddingBlockEnd: "0px",
                opacity: 0,
                transform: "translateX(-8px)",
            },
        ],
        { duration: 180, easing: EASE },
    ).onfinish = () => form.requestSubmit();
    return true;
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

function wireQuantityInputs(root: ParentNode, signal: AbortSignal): void {
    root.querySelectorAll<HTMLInputElement>("input[name='quantity']").forEach((input) => {
        updateQuantityDisabledState(input);
        input.addEventListener("input", () => updateQuantityDisabledState(input), { signal });
        input.addEventListener("change", () => updateQuantityDisabledState(input), { signal });
    });
}

function replayQueryFeedback(root: ParentNode): void {
    const query = new URLSearchParams(window.location.search);
    if (query.get("feedback") !== "Added") {
        return;
    }

    const sku = query.get("sku");
    animateLineIn(sku ? root.querySelector(`[data-sku='${CSS.escape(sku)}']`) : null);
    pulse(root.querySelector("[data-action='checkout'] .checkout-amount"));
}

export function pulseToast(): void {
    const toast = document.querySelector<HTMLElement>(".toast");
    if (!toast || reduceMotion()) {
        return;
    }

    toast.animate(
        [{ transform: "scale(1)" }, { transform: "scale(1.04)" }, { transform: "scale(1)" }],
        { duration: 200, easing: EASE },
    );
}

export function initialize(): void {
    dispose();

    const root = document.querySelector("[data-checkout-page]");
    if (!root) {
        return;
    }

    controller = new AbortController();
    const { signal } = controller;

    document.addEventListener("click", (event) => {
        if (event.defaultPrevented) {
            return;
        }

        const stepButton = closestButton(event.target, "[data-qty-step]");
        if (stepButton) {
            updateQuantity(stepButton);
            return;
        }

        const removeButton = closestButton(event.target, ".line-step[data-act='dec']");
        if (removeButton && submitAfterLineOut(removeButton)) {
            event.preventDefault();
        }
    }, { signal });

    wireQuantityInputs(root, signal);
    root.querySelectorAll<HTMLFormElement>("[data-clear-form]").forEach(wireClearDialog);
    observeCheckoutChanges(root);
    replayQueryFeedback(root);
}

export function dispose(): void {
    observer?.disconnect();
    observer = null;
    controller?.abort();
    controller = null;
    lineStates.clear();
    totalText = "";
}
