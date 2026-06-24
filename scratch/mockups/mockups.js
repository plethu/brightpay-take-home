/*
 * Shared behaviour for the checkout-till mockups.
 *
 * PROCESS ARTIFACT, not production code. Each mockup HTML renders ONE till and
 * declares region placeholders (data-region="..."); this script fills them for
 * the selected simulated state and wires the floating dev-tools panel that lets
 * a reviewer flip between states. The three layouts share this so the basket,
 * totals, savings, and error rendering stay identical across A/B/C.
 *
 * Classic script (not a module) so it loads over file:// without CORS fuss.
 */
(function () {
    "use strict";

    // SPEC pricing, in pence. Offers are the only type the kata defines: buy n
    // for a fixed price.
    const CATALOG = {
        A: { name: "Apple", unit: 50, offer: { label: "3 for £1.30", n: 3, price: 130 } },
        B: { name: "Banana", unit: 30, offer: { label: "2 for £0.45", n: 2, price: 45 } },
        C: { name: "Chocolate", unit: 20, offer: null },
        D: { name: "Doughnut", unit: 15, offer: null },
    };

    const gbp = (pence) => "£" + (pence / 100).toFixed(2);

    // Coupon/tag glyph for offers. A drawn tag reads as "deal" so the offer's
    // colour is never the only signal; it pairs with --color-offer-* in CSS.
    const COUPON_ICON = '<svg class="coupon-ic" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20.59 13.41 13.42 20.58a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82Z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>';

    // Build a basket line, applying the multi-buy offer as many times as it fits.
    function line(sku, qty) {
        const c = CATALOG[sku];
        if (c.offer && qty >= c.offer.n) {
            const times = Math.floor(qty / c.offer.n);
            const rem = qty % c.offer.n;
            const base = qty * c.unit;
            const linePence = times * c.offer.price + rem * c.unit;
            return {
                sku, qty, unit: c.unit, linePence,
                offer: { label: c.offer.label, times, basePence: base, savingPence: base - linePence },
            };
        }
        return { sku, qty, unit: c.unit, linePence: qty * c.unit, offer: null };
    }

    // The four reachable states from scratch/05-design.md. "added" doubles as the
    // running-total-with-offers state.
    const STATES = {
        empty: { label: "Empty basket", lines: [], scan: "", error: null, status: "" },
        added: {
            label: "Item added",
            lines: [line("A", 3), line("B", 2), line("C", 1), line("D", 1)],
            scan: "", error: null, status: "Added 3 × Apple to the sale.",
        },
        error: {
            label: "Validation error",
            lines: [line("C", 1)],
            scan: "Z",
            error: "<strong>“Z” isn’t a known SKU.</strong> Check the price list and try again.",
            status: "",
        },
    };

    const STATE_ORDER = ["empty", "added", "error"];

    function renderBasket(state, emptyHint) {
        if (state.lines.length === 0) {
            return `<div class="empty-state"><strong>No items yet</strong>${emptyHint}</div>`;
        }
        const items = state.lines.map((l) => {
            const name = CATALOG[l.sku].name;
            // Meta line: SKU chip, then either the applied offer (coupon tag) or
            // the unit price when a plain line has more than one of an item.
            const offerOrEach = l.offer
                ? `<span class="offer-badge">${COUPON_ICON}${l.offer.label}</span>`
                : (l.qty > 1 ? `<span class="line-each">${gbp(l.unit)} each</span>` : "");
            const was = l.offer ? `<span class="line-was">was ${gbp(l.offer.basePence)}</span>` : "";
            const saving = l.offer ? `<span class="line-saving">−${gbp(l.offer.savingPence)}</span>` : "";
            // The meta line only appears when it carries something (an offer, or a
            // unit price on a multi-item plain line); otherwise it's omitted so a
            // lone SKU never floats on its own row.
            const metaInner = `${offerOrEach}${was}`;
            const meta = metaInner ? `<p class="line-meta">${metaInner}</p>` : "";
            return `<li class="basket-line">
                <div class="line-info">
                    <p class="line-name">${name} <span class="line-sku">${l.sku}</span> <span class="line-qty">× ${l.qty}</span></p>
                    ${meta}
                    <form><button type="submit" class="line-remove" aria-label="Remove ${name}">Remove</button></form>
                </div>
                <div class="line-amounts">
                    <span class="line-price">${gbp(l.linePence)}</span>
                    ${saving}
                </div>
            </li>`;
        }).join("");
        return `<ul class="basket">${items}</ul>`;
    }

    function renderTotals(state) {
        const sub = state.lines.reduce((s, l) => s + (l.offer ? l.offer.basePence : l.linePence), 0);
        const sav = state.lines.reduce((s, l) => s + (l.offer ? l.offer.savingPence : 0), 0);
        const total = state.lines.reduce((s, l) => s + l.linePence, 0);
        const rows = sav > 0
            ? `<div class="totals-row"><span>Subtotal</span><span class="amount">${gbp(sub)}</span></div>
               <div class="totals-row totals-saving"><span>Offer savings</span><span class="amount">−${gbp(sav)}</span></div>`
            : "";
        return `${rows}<div class="totals-grand"><span class="label">Total</span><span class="amount">${gbp(total)}</span></div>`;
    }

    function countLabel(state) {
        if (state.lines.length === 0) return "";
        const items = state.lines.reduce((s, l) => s + l.qty, 0);
        const lines = state.lines.length;
        return `${lines} line${lines === 1 ? "" : "s"} · ${items} item${items === 1 ? "" : "s"}`;
    }

    function apply(name) {
        const state = STATES[name];
        const emptyHint = document.body.dataset.emptyHint || "Scan a SKU to start the transaction.";

        document.querySelectorAll('[data-region="basket"]').forEach((el) => {
            el.innerHTML = renderBasket(state, emptyHint);
        });
        document.querySelectorAll('[data-region="totals"]').forEach((el) => {
            el.innerHTML = renderTotals(state);
        });
        document.querySelectorAll('[data-region="alert"]').forEach((el) => {
            el.innerHTML = state.error ? `<p class="banner-error" role="alert"><span>${state.error}</span></p>` : "";
        });
        document.querySelectorAll('[data-region="status"]').forEach((el) => {
            el.textContent = state.status;
        });
        document.querySelectorAll('[data-region="count"]').forEach((el) => {
            el.textContent = countLabel(state);
        });
        document.querySelectorAll("[data-scan]").forEach((el) => {
            el.value = state.scan;
            if (state.scan && state.error) {
                el.setAttribute("aria-invalid", "true");
            } else {
                el.removeAttribute("aria-invalid");
            }
        });
        // Clear / checkout are meaningless on an empty basket.
        document.querySelectorAll("[data-action]").forEach((el) => {
            el.disabled = name === "empty";
        });
        document.querySelectorAll("[data-state-option]").forEach((r) => {
            r.checked = r.value === name;
        });
    }

    function toggleTheme(button) {
        const root = document.documentElement;
        const dark = root.getAttribute("data-theme") === "dark";
        root.setAttribute("data-theme", dark ? "light" : "dark");
        button.setAttribute("aria-pressed", String(!dark));
    }

    function mountDevtools(active) {
        const options = STATE_ORDER.map((k) =>
            `<label class="devtools-radio">
                <input type="radio" name="mock-state" value="${k}" data-state-option ${k === active ? "checked" : ""}>
                ${STATES[k].label}
            </label>`).join("");

        const panel = document.createElement("aside");
        panel.className = "devtools";
        panel.setAttribute("aria-label", "Mockup dev tools");
        panel.innerHTML = `
            <p class="devtools-title">Mockup dev tools <span class="devtools-tag">not product UI</span></p>
            <button type="button" class="btn btn-quiet devtools-collapse" data-collapse aria-expanded="true">Hide</button>
            <fieldset class="devtools-group">
                <legend>Simulate state</legend>
                ${options}
            </fieldset>
            <button type="button" class="btn btn-secondary devtools-theme" data-theme-toggle aria-pressed="false">Toggle theme</button>`;
        document.body.appendChild(panel);

        panel.querySelectorAll("[data-state-option]").forEach((r) => {
            r.addEventListener("change", () => apply(r.value));
        });
        panel.querySelector("[data-theme-toggle]").addEventListener("click", (e) => toggleTheme(e.currentTarget));
        panel.querySelector("[data-collapse]").addEventListener("click", (e) => {
            const collapsed = panel.getAttribute("data-collapsed") === "true";
            panel.setAttribute("data-collapsed", String(!collapsed));
            e.currentTarget.setAttribute("aria-expanded", String(collapsed));
            e.currentTarget.textContent = collapsed ? "Hide" : "Show";
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        // ?state=empty|added|error overrides the per-mockup default, so a state is
        // shareable/bookmarkable (and screenshot-able) without touching markup.
        const params = new URLSearchParams(location.search);
        const requested = params.get("state");
        const active = (requested && STATES[requested])
            ? requested
            : (document.body.dataset.defaultState || "added");
        // ?theme=dark|light is handy for screenshotting both schemes headlessly.
        const theme = params.get("theme");
        if (theme === "dark" || theme === "light") {
            document.documentElement.setAttribute("data-theme", theme);
        }
        mountDevtools(active);
        apply(active);
    });
})();
