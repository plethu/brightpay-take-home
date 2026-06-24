/*
 * Interactive behaviour for mockup E.
 *
 * Unlike the shared mockups.js (which simulates fixed states for A-D), E is a
 * working cart: pressing a SKU key adds it, the per-line steppers change or
 * remove quantities, Clear and Charge complete the sale -- all client-side with
 * no full-page reload, so the mockup represents the real interactive feel we'll
 * build in Blazor (Interactive Server augmenting the no-JS form posts).
 *
 * Classic script so it loads over file:// without a module/CORS dance.
 */
(function () {
    "use strict";

    const CATALOG = {
        A: { name: "Apple", unit: 50, offer: { label: "3 for £1.30", n: 3, price: 130 } },
        B: { name: "Banana", unit: 30, offer: { label: "2 for £0.45", n: 2, price: 45 } },
        C: { name: "Chocolate", unit: 20, offer: null },
        D: { name: "Doughnut", unit: 15, offer: null },
    };

    const COUPON_ICON = '<svg class="coupon-ic" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20.59 13.41 13.42 20.58a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82Z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>';
    const MINUS_ICON = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" aria-hidden="true"><line x1="5" y1="12" x2="19" y2="12"/></svg>';
    const PLUS_ICON = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" aria-hidden="true"><line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/></svg>';
    const TRASH_ICON = '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 6h18M8 6V4a1 1 0 0 1 1-1h6a1 1 0 0 1 1 1v2m2 0v14a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V6"/></svg>';

    const gbp = (pence) => "£" + (pence / 100).toFixed(2);
    const EASE = "cubic-bezier(0.2, 0, 0, 1)";
    const reduce = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Ordered cart so lines keep a stable position as quantities change.
    let cart = [
        { sku: "A", qty: 3 },
        { sku: "B", qty: 2 },
        { sku: "C", qty: 1 },
        { sku: "D", qty: 1 },
    ];
    const lineEls = new Map(); // sku -> <li>

    // Apply the multi-buy offer as many whole times as it fits.
    function calc(sku, qty) {
        const c = CATALOG[sku];
        if (c.offer && qty >= c.offer.n) {
            const times = Math.floor(qty / c.offer.n);
            const rem = qty % c.offer.n;
            const base = qty * c.unit;
            const linePence = times * c.offer.price + rem * c.unit;
            return { linePence, base, saving: base - linePence, offer: c.offer };
        }
        return { linePence: qty * c.unit, base: qty * c.unit, saving: 0, offer: null };
    }

    // ---- DOM refs ----------------------------------------------------------
    const form = document.querySelector(".add-form");
    const qtyInput = document.getElementById("qty-e");
    const scanInput = document.getElementById("scan-e");
    const alertEl = document.querySelector('[data-region="alert"]');
    const basketEl = document.querySelector('[data-region="basket"]');
    const totalsEl = document.querySelector('[data-region="totals"]');
    const countEl = document.querySelector("[data-count]");
    const totalEl = document.querySelector("[data-total]");
    const clearBtn = document.querySelector('[data-action="clear"]');
    const checkoutBtn = document.querySelector('[data-action="checkout"]');
    const toastEl = document.querySelector(".toast");
    const toastMsg = document.querySelector("[data-toast-msg]");
    const toastDismiss = document.querySelector(".toast-dismiss");
    const confirmEl = document.getElementById("clear-confirm");
    const confirmCount = document.querySelector("[data-confirm-count]");

    let listEl = null; // <ul> created on demand

    // ---- Animations (all reduced-motion aware) -----------------------------
    function animateIn(el) {
        if (reduce()) return;
        const h = el.getBoundingClientRect().height;
        el.style.overflow = "hidden";
        const a = el.animate(
            [
                { blockSize: "0px", opacity: 0, transform: "translateX(-8px)" },
                { blockSize: h + "px", opacity: 1, transform: "none" },
            ],
            { duration: 240, easing: EASE },
        );
        a.onfinish = () => { el.style.overflow = ""; el.style.blockSize = ""; };
    }

    function animateOut(el, done) {
        if (reduce()) { el.remove(); done && done(); return; }
        const h = el.getBoundingClientRect().height;
        const style = getComputedStyle(el);
        el.style.overflow = "hidden";
        el.style.boxSizing = "border-box";
        el.style.blockSize = h + "px";
        const a = el.animate(
            [
                {
                    blockSize: h + "px",
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
        );
        a.onfinish = () => { el.remove(); done && done(); };
    }

    function pulse(el) {
        if (!el || reduce()) return;
        el.animate(
            [{ transform: "scale(1)" }, { transform: "scale(1.12)" }, { transform: "scale(1)" }],
            { duration: 240, easing: EASE },
        );
    }

    // True when the quantity is enough for the multi-buy offer to apply.
    const offerActive = (sku, qty) => calc(sku, qty).offer !== null;

    // When a line crosses into offer eligibility, let the coupon tag and the
    // struck pre-offer price arrive with a small tactile pop rather than just
    // snapping in -- same subtle philosophy as the line add/remove.
    function animateOfferIn(li) {
        if (reduce()) return;
        li.querySelectorAll(".line-meta, .line-was").forEach((el) => {
            el.animate(
                [
                    { opacity: 0, transform: "translateY(-4px) scale(0.92)" },
                    { opacity: 1, transform: "none" },
                ],
                { duration: 260, easing: EASE },
            );
        });
    }

    // Mirror of animateOfferIn: when a line drops below the offer threshold, let
    // the coupon tag and struck price collapse and fade out, then re-render the
    // line plain. done() runs once every leaving element has finished.
    function animateOfferOut(li, done) {
        const els = [...li.querySelectorAll(".line-meta, .line-was")];
        if (reduce() || els.length === 0) { done(); return; }
        let pending = els.length;
        const fin = () => { if (--pending === 0) done(); };
        els.forEach((el) => {
            const h = el.getBoundingClientRect().height;
            el.style.overflow = "hidden";
            el.animate(
                [
                    { opacity: 1, transform: "none", blockSize: h + "px" },
                    { opacity: 0, transform: "translateY(-4px) scale(0.92)", blockSize: "0px" },
                ],
                { duration: 200, easing: EASE },
            ).onfinish = fin;
        });
    }

    // ---- Rendering ---------------------------------------------------------
    function lineInner(sku, qty) {
        const c = CATALOG[sku];
        const r = calc(sku, qty);
        const meta = r.offer
            ? `<p class="line-meta"><span class="offer-badge">${COUPON_ICON}${r.offer.label}</span></p>`
            : "";
        const was = r.offer
            ? `<span class="line-was" aria-label="before offer">${gbp(r.base)}</span>`
            : "";
        const isOne = qty === 1;
        const decLabel = isOne ? `Remove ${c.name}` : `Decrease ${c.name}`;
        return `
            <div class="line-info">
                <p class="line-name">${c.name} <span class="line-sku">${sku}</span></p>
                ${meta}
            </div>
            <div class="line-qty-control" role="group" aria-label="${c.name} quantity">
                <button type="button" class="line-step" data-act="dec" data-sku="${sku}" aria-label="${decLabel}" title="${decLabel}">${isOne ? TRASH_ICON : MINUS_ICON}</button>
                <span class="line-qty" aria-live="polite">${qty}</span>
                <button type="button" class="line-step" data-act="inc" data-sku="${sku}" aria-label="Add ${c.name}" title="Add ${c.name}">${PLUS_ICON}</button>
            </div>
            <div class="line-amounts">
                <span class="line-price">${gbp(r.linePence)}</span>
                ${was}
            </div>`;
    }

    function makeLine(entry) {
        const li = document.createElement("li");
        li.className = "sale-line";
        li.dataset.sku = entry.sku;
        li.innerHTML = lineInner(entry.sku, entry.qty);
        return li;
    }

    function ensureList() {
        if (listEl && listEl.isConnected) return;
        basketEl.innerHTML = "";
        listEl = document.createElement("ul");
        listEl.className = "sale-list";
        basketEl.appendChild(listEl);
    }

    function showEmpty() {
        listEl = null;
        lineEls.clear();
        basketEl.innerHTML =
            `<div class="empty-state"><strong>No items yet</strong>Press an item above, or scan one with the gun.</div>`;
    }

    function refreshTotals() {
        const items = cart.reduce((s, e) => s + e.qty, 0);
        const lines = cart.length;
        let sub = 0, sav = 0, total = 0;
        cart.forEach((e) => {
            const r = calc(e.sku, e.qty);
            sub += r.base; sav += r.saving; total += r.linePence;
        });

        countEl.textContent = lines ? `· ${items} item${items === 1 ? "" : "s"}` : "";
        totalsEl.innerHTML = sav > 0
            ? `<div class="totals-row"><span>Subtotal</span><span class="amount">${gbp(sub)}</span></div>
               <div class="totals-row totals-saving"><span>Offer savings</span><span class="amount">−${gbp(sav)}</span></div>`
            : `<div class="totals-row"><span>Subtotal</span><span class="amount">${gbp(sub)}</span></div>`;
        totalEl.textContent = gbp(total);
        const empty = lines === 0;
        clearBtn.disabled = empty;
        checkoutBtn.disabled = empty;
    }

    function renderAll() {
        if (cart.length === 0) { showEmpty(); refreshTotals(); return; }
        ensureList();
        lineEls.clear();
        listEl.innerHTML = "";
        cart.forEach((e) => {
            const li = makeLine(e);
            listEl.appendChild(li);
            lineEls.set(e.sku, li);
        });
        refreshTotals();
    }

    // ---- Mutations ---------------------------------------------------------
    function addItem(sku, qty) {
        const entry = cart.find((e) => e.sku === sku);
        if (entry) {
            const before = offerActive(sku, entry.qty);
            entry.qty += qty;
            const li = lineEls.get(sku);
            li.innerHTML = lineInner(sku, entry.qty);
            pulse(li.querySelector(".line-qty"));
            if (!before && offerActive(sku, entry.qty)) animateOfferIn(li);
        } else {
            const e = { sku, qty };
            cart.push(e);
            ensureList();
            const li = makeLine(e);
            listEl.appendChild(li);
            lineEls.set(sku, li);
            animateIn(li);
        }
        refreshTotals();
    }

    function changeItem(sku, delta) {
        const entry = cart.find((e) => e.sku === sku);
        if (!entry) return;
        const before = offerActive(sku, entry.qty);
        entry.qty += delta;
        if (entry.qty <= 0) { removeItem(sku); return; }
        const li = lineEls.get(sku);
        const after = offerActive(sku, entry.qty);

        if (before && !after) {
            // Offer just stopped applying. Update the qty/price live so the press
            // feels instant, animate the tag + struck price out, then re-render
            // the line plain (fixes the dec icon/labels for the new quantity).
            const r = calc(sku, entry.qty);
            li.querySelector(".line-price").textContent = gbp(r.linePence);
            const qtyEl = li.querySelector(".line-qty");
            qtyEl.textContent = entry.qty;
            pulse(qtyEl);
            animateOfferOut(li, () => { li.innerHTML = lineInner(sku, entry.qty); });
            refreshTotals();
            return;
        }

        li.innerHTML = lineInner(sku, entry.qty);
        pulse(li.querySelector(".line-qty"));
        if (!before && after) animateOfferIn(li);
        refreshTotals();
    }

    function removeItem(sku) {
        const li = lineEls.get(sku);
        cart = cart.filter((e) => e.sku !== sku);
        lineEls.delete(sku);
        const after = () => { if (cart.length === 0) showEmpty(); refreshTotals(); };
        if (li) animateOut(li, after); else after();
    }

    function clearSale() {
        if (cart.length === 0) return;
        cart = [];
        const els = [...lineEls.values()];
        lineEls.clear();
        let pending = els.length;
        els.forEach((li) => animateOut(li, () => { if (--pending === 0) showEmpty(); }));
        if (els.length === 0) showEmpty();
        refreshTotals();
    }

    // ---- Errors + toast ----------------------------------------------------
    function showError(raw) {
        alertEl.innerHTML =
            `<p class="banner-error" role="alert"><span><strong>“${raw}” isn’t a known SKU.</strong> Check the keys and try again.</span></p>`;
        scanInput.setAttribute("aria-invalid", "true");
        const banner = alertEl.firstElementChild;
        if (!banner || reduce()) return;
        const h = banner.getBoundingClientRect().height;
        banner.style.overflow = "hidden";
        banner.animate(
            [
                { blockSize: "0px", opacity: 0, transform: "translateY(-4px)" },
                { blockSize: h + "px", opacity: 1, transform: "none" },
            ],
            { duration: 220, easing: EASE },
        ).onfinish = () => { banner.style.overflow = ""; };
    }
    function clearError() {
        scanInput.removeAttribute("aria-invalid");
        const banner = alertEl.firstElementChild;
        if (!banner) return;
        if (reduce()) { alertEl.innerHTML = ""; return; }
        const h = banner.getBoundingClientRect().height;
        banner.style.overflow = "hidden";
        banner.animate(
            [
                { blockSize: h + "px", opacity: 1, transform: "none" },
                { blockSize: "0px", opacity: 0, transform: "translateY(-4px)" },
            ],
            { duration: 180, easing: EASE },
        ).onfinish = () => { if (alertEl.firstElementChild === banner) alertEl.innerHTML = ""; };
    }

    // Toasts coalesce: rapid adds wait briefly before the toast appears, so
    // seven quick taps become one "Added 7..." message. Once an add toast is
    // visible, later adds update that same toast in place; spaced updates get a
    // small pulse, while bursty ones are folded into a trailing update.
    const TOAST_DEBOUNCE_MS = 120;
    const TOAST_DISMISS_MS = 1800;
    const addBatch = new Map(); // sku -> qty added while the toast is up
    let toastTimer = null;
    let addDebounceTimer = null;
    let lastAddToastAt = 0;

    function pulseToast() {
        if (reduce()) return;
        toastEl.animate(
            [{ transform: "scale(1)" }, { transform: "scale(1.04)" }, { transform: "scale(1)" }],
            { duration: 200, easing: EASE },
        );
    }

    function hideToast() {
        clearTimeout(toastTimer);
        clearTimeout(addDebounceTimer);
        addBatch.clear();
        if (toastEl.hidden) return;
        if (reduce()) { toastEl.hidden = true; return; }
        toastEl.animate(
            [{ opacity: 1, transform: "none" }, { opacity: 0, transform: "translateY(8px)" }],
            { duration: 160, easing: EASE },
        ).onfinish = () => { toastEl.hidden = true; };
    }

    // Render text now. A hidden toast slides in; a visible one updates in place
    // (pulsing when asked) instead of replaying the entrance.
    function renderToast(msg, pulse) {
        clearTimeout(toastTimer);
        toastEl.getAnimations().forEach((animation) => animation.cancel());
        const wasVisible = !toastEl.hidden;
        toastMsg.textContent = msg;
        toastEl.hidden = false;
        if (!wasVisible) {
            if (!reduce()) {
                toastEl.animate(
                    [{ opacity: 0, transform: "translateY(8px)" }, { opacity: 1, transform: "none" }],
                    { duration: 200, easing: EASE },
                );
            }
        } else if (pulse) {
            pulseToast();
        }
        toastTimer = setTimeout(hideToast, TOAST_DISMISS_MS);
    }

    // One-off message (e.g. charge complete): not an add, so drop any add tally.
    function showToast(msg) {
        clearTimeout(addDebounceTimer);
        addBatch.clear();
        renderToast(msg, false);
    }

    function addToastMessage() {
        if (addBatch.size === 1) {
            const [s, q] = [...addBatch][0];
            return `Added ${q} × ${CATALOG[s].name} to the sale.`;
        }
        const total = [...addBatch.values()].reduce((a, b) => a + b, 0);
        return `Added ${total} items to the sale.`;
    }

    function flushAddToast(pulseVisible) {
        clearTimeout(addDebounceTimer);
        if (addBatch.size === 0) return;
        renderToast(addToastMessage(), pulseVisible);
        lastAddToastAt = performance.now();
    }

    // Add feedback, coalesced into the visible toast.
    function notifyAdd(sku, qty) {
        addBatch.set(sku, (addBatch.get(sku) || 0) + qty);
        const visible = !toastEl.hidden;
        const elapsed = performance.now() - lastAddToastAt;

        if (!visible) {
            clearTimeout(addDebounceTimer);
            addDebounceTimer = setTimeout(() => flushAddToast(false), TOAST_DEBOUNCE_MS);
            return;
        }

        if (elapsed >= TOAST_DEBOUNCE_MS) {
            flushAddToast(true);
            return;
        }

        clearTimeout(addDebounceTimer);
        addDebounceTimer = setTimeout(
            () => flushAddToast(false),
            TOAST_DEBOUNCE_MS - elapsed,
        );
    }

    // ---- Wiring ------------------------------------------------------------
    const clampQty = (v) => Math.max(1, parseInt(v, 10) || 1);

    // Manual / scan / typed entry. This is the only path that can produce an
    // unknown SKU (the tiles can't), so it owns the validation error state.
    function manualAdd() {
        const raw = (scanInput.value || "").trim();
        if (!raw) return;
        const sku = raw.toUpperCase();
        if (!CATALOG[sku]) { showError(raw); return; }
        clearError();
        const qty = clampQty(qtyInput.value);
        addItem(sku, qty);
        notifyAdd(sku, qty);
        scanInput.value = "";
        qtyInput.value = 1;
    }

    form.addEventListener("submit", (e) => {
        e.preventDefault(); // JS path: no reload. No-JS path posts the form.
        const submitter = e.submitter;
        // A SKU tile names itself: a direct, always-valid add at the chosen qty.
        if (submitter && submitter.name === "sku") {
            const qty = clampQty(qtyInput.value);
            addItem(submitter.value, qty);
            notifyAdd(submitter.value, qty);
            qtyInput.value = 1;
            return;
        }
        // Everything else is the manual "Add" button.
        manualAdd();
    });

    // Enter inside a text field would otherwise fire the form's default button --
    // the first SKU tile -- and silently add an Apple regardless of what was
    // typed. Route the scan field's Enter to the manual add and swallow Enter in
    // the qty field.
    scanInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") { e.preventDefault(); manualAdd(); }
    });
    qtyInput.addEventListener("keydown", (e) => {
        if (e.key === "Enter") e.preventDefault();
    });
    // Clear a stale error as soon as the operator starts correcting the SKU.
    scanInput.addEventListener("input", () => {
        if (scanInput.getAttribute("aria-invalid")) clearError();
    });

    // Global quantity stepper (applies to the next add).
    document.querySelectorAll(".qty-step").forEach((btn) => {
        btn.addEventListener("click", () => {
            const dir = btn.getAttribute("aria-label").startsWith("Increase") ? 1 : -1;
            qtyInput.value = clampQty(parseInt(qtyInput.value, 10) + dir);
        });
    });

    // Per-line steppers (delegated).
    basketEl.addEventListener("click", (e) => {
        const btn = e.target.closest(".line-step");
        if (!btn) return;
        changeItem(btn.dataset.sku, btn.dataset.act === "inc" ? 1 : -1);
    });

    // Clear is destructive: confirm via the native dialog first. Esc / backdrop /
    // "Keep sale" all resolve to a non-"clear" returnValue and leave the sale.
    clearBtn.addEventListener("click", () => {
        if (cart.length === 0) return;
        const lines = cart.length;
        confirmCount.textContent = lines === 1 ? "the only line" : `all ${lines} lines`;
        if (typeof confirmEl.showModal === "function") {
            confirmEl.showModal();
        } else {
            clearSale(); // very old engines without <dialog>: fall back to immediate clear
        }
    });
    confirmEl.addEventListener("close", () => {
        if (confirmEl.returnValue === "clear") clearSale();
        confirmEl.returnValue = "";
    });
    checkoutBtn.addEventListener("click", () => {
        const total = cart.reduce((s, e) => s + calc(e.sku, e.qty).linePence, 0);
        showToast(`Charged ${gbp(total)} · sale complete.`);
        clearSale();
    });

    toastDismiss.addEventListener("click", hideToast);

    renderAll();

    // Screenshot hook (matches the ?state=/?theme= convention): ?open=clear pops
    // the confirm dialog so a reviewer can capture it without clicking.
    if (new URLSearchParams(location.search).get("open") === "clear" && cart.length) {
        confirmCount.textContent = `all ${cart.length} lines`;
        confirmEl.showModal();
    }
})();
