// @ts-check

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

    /**
     * @typedef {"A" | "B" | "C" | "D"} Sku
     * @typedef {{ label: string, n: number, price: number }} Offer
     * @typedef {{ name: string, unit: number, offer: Offer | null }} CatalogItem
     * @typedef {{ sku: Sku, qty: number }} CartEntry
     * @typedef {{ linePence: number, base: number, saving: number, offer: Offer | null }} LineCalc
     */

    /** @type {Record<Sku, CatalogItem>} */
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

    /** @param {number} pence */
    const gbp = (pence) => "£" + (pence / 100).toFixed(2);
    const EASE = "cubic-bezier(0.2, 0, 0, 1)";
    const reduce = () => window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    // Ordered cart so lines keep a stable position as quantities change.
    /** @type {CartEntry[]} */
    let cart = [
        { sku: "A", qty: 3 },
        { sku: "B", qty: 2 },
        { sku: "C", qty: 1 },
        { sku: "D", qty: 1 },
    ];
    /** @type {Map<Sku, HTMLLIElement>} */
    const lineEls = new Map(); // sku -> <li>

    /**
     * @template {Element} T
     * @param {ParentNode} root
     * @param {string} selector
     * @returns {T}
     */
    function must(root, selector) {
        const el = root.querySelector(selector);
        if (!el) throw new Error(`Missing required mockup element: ${selector}`);
        return /** @type {T} */ (el);
    }

    /**
     * @param {string} value
     * @returns {value is Sku}
     */
    function isSku(value) {
        return Object.hasOwn(CATALOG, value);
    }

    /**
     * @param {string} value
     * @returns {string}
     */
    function escapeHtml(value) {
        const span = document.createElement("span");
        span.textContent = value;
        return span.innerHTML;
    }

    // Apply the multi-buy offer as many whole times as it fits.
    /**
     * @param {Sku} sku
     * @param {number} qty
     * @returns {LineCalc}
     */
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
    /** @type {HTMLFormElement} */
    const form = must(document, ".add-form");
    /** @type {HTMLInputElement} */
    const qtyInput = must(document, "#qty-e");
    /** @type {HTMLInputElement} */
    const scanInput = must(document, "#scan-e");
    /** @type {HTMLElement} */
    const alertEl = must(document, '[data-region="alert"]');
    /** @type {HTMLElement} */
    const basketEl = must(document, '[data-region="basket"]');
    /** @type {HTMLElement} */
    const totalsEl = must(document, '[data-region="totals"]');
    /** @type {HTMLElement} */
    const countEl = must(document, "[data-count]");
    /** @type {HTMLElement} */
    const totalEl = must(document, "[data-total]");
    /** @type {HTMLButtonElement} */
    const clearBtn = must(document, '[data-action="clear"]');
    /** @type {HTMLButtonElement} */
    const checkoutBtn = must(document, '[data-action="checkout"]');
    /** @type {HTMLElement} */
    const toastEl = must(document, ".toast");
    /** @type {HTMLElement} */
    const toastMsg = must(document, "[data-toast-msg]");
    /** @type {HTMLButtonElement} */
    const toastDismiss = must(document, ".toast-dismiss");
    /** @type {HTMLElement} */
    const liveRegion = must(document, "[data-live-region]");
    /** @type {HTMLDialogElement} */
    const confirmEl = must(document, "#clear-confirm");
    /** @type {HTMLElement} */
    const confirmCount = must(document, "[data-confirm-count]");
    const clearForm = clearBtn.form;
    const checkoutForm = checkoutBtn.form;
    if (!clearForm || !checkoutForm) {
        throw new Error("Terminal action buttons must belong to forms.");
    }

    /** @type {HTMLUListElement | null} */
    let listEl = null; // <ul> created on demand

    // ---- Animations (all reduced-motion aware) -----------------------------
    /**
     * @param {HTMLElement} el
     */
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

    /**
     * @param {HTMLElement} el
     * @param {() => void} [done]
     */
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

    /**
     * @param {Element | null} el
     */
    function pulse(el) {
        if (!el || reduce()) return;
        el.animate(
            [{ transform: "scale(1)" }, { transform: "scale(1.12)" }, { transform: "scale(1)" }],
            { duration: 240, easing: EASE },
        );
    }

    // True when the quantity is enough for the multi-buy offer to apply.
    /**
     * @param {Sku} sku
     * @param {number} qty
     * @returns {boolean}
     */
    const offerActive = (sku, qty) => calc(sku, qty).offer !== null;

    // When a line crosses into offer eligibility, let the coupon tag and the
    // struck pre-offer price arrive with a small tactile pop rather than just
    // snapping in -- same subtle philosophy as the line add/remove.
    /**
     * @param {HTMLLIElement} li
     */
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
    /**
     * @param {HTMLLIElement} li
     * @param {() => void} done
     */
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
    /**
     * @returns {HTMLButtonElement | null}
     */
    function firstSkuButton() {
        const first = document.querySelector(".sku-tile");
        return first instanceof HTMLButtonElement ? first : null;
    }

    /**
     * @param {HTMLElement | null} target
     */
    function focusSoon(target) {
        if (!target) return;
        requestAnimationFrame(() => target.focus({ preventScroll: true }));
    }

    function focusDefaultAddTarget() {
        focusSoon(firstSkuButton() || scanInput);
    }

    /**
     * @param {HTMLLIElement} removedLine
     * @returns {HTMLElement | null}
     */
    function focusTargetAfterLineRemoval(removedLine) {
        if (!listEl) return firstSkuButton();
        const lines = [...listEl.querySelectorAll(".sale-line")];
        const removedIndex = lines.indexOf(removedLine);
        const nextLine = lines[removedIndex + 1] || lines[removedIndex - 1];
        const nextControl = nextLine?.querySelector(".line-step");
        if (nextControl instanceof HTMLButtonElement) return nextControl;
        return firstSkuButton();
    }

    /**
     * @param {Sku} sku
     * @param {number} qty
     * @returns {string}
     */
    function lineInner(sku, qty) {
        const c = CATALOG[sku];
        const r = calc(sku, qty);
        const name = escapeHtml(c.name);
        const skuText = escapeHtml(sku);
        const meta = r.offer
            ? `<p class="line-meta"><span class="offer-badge">${COUPON_ICON}${escapeHtml(r.offer.label)}</span></p>`
            : "";
        const was = r.offer
            ? `<span class="line-was" aria-label="before offer">${gbp(r.base)}</span>`
            : "";
        const isOne = qty === 1;
        const decLabel = escapeHtml(isOne ? `Remove ${c.name}` : `Decrease ${c.name}`);
        const addLabel = escapeHtml(`Add ${c.name}`);
        return `
            <div class="line-info">
                <p class="line-name">${name} <span class="line-sku">${skuText}</span></p>
                ${meta}
            </div>
            <form class="line-qty-control" method="post" action="/checkout/items/${sku}" aria-label="${name} quantity">
                <input type="hidden" name="sku" value="${sku}">
                <button type="submit" name="delta" value="-1" formaction="/checkout/items/${sku}/decrement" class="line-step" data-act="dec" data-sku="${sku}" aria-label="${decLabel}" title="${decLabel}">${isOne ? TRASH_ICON : MINUS_ICON}</button>
                <span class="line-qty" aria-live="polite">${qty}</span>
                <button type="submit" name="delta" value="1" formaction="/checkout/items/${sku}/increment" class="line-step" data-act="inc" data-sku="${sku}" aria-label="${addLabel}" title="${addLabel}">${PLUS_ICON}</button>
            </form>
            <div class="line-amounts">
                <span class="line-price">${gbp(r.linePence)}</span>
                ${was}
            </div>`;
    }

    /**
     * @param {CartEntry} entry
     * @returns {HTMLLIElement}
     */
    function makeLine(entry) {
        const li = document.createElement("li");
        li.className = "sale-line";
        li.dataset.sku = entry.sku;
        li.innerHTML = lineInner(entry.sku, entry.qty);
        return li;
    }

    /**
     * @returns {HTMLUListElement}
     */
    function ensureList() {
        if (listEl && listEl.isConnected) return listEl;
        basketEl.innerHTML = "";
        listEl = document.createElement("ul");
        listEl.className = "sale-list";
        basketEl.appendChild(listEl);
        return listEl;
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
        const list = ensureList();
        lineEls.clear();
        list.innerHTML = "";
        cart.forEach((e) => {
            const li = makeLine(e);
            list.appendChild(li);
            lineEls.set(e.sku, li);
        });
        refreshTotals();
    }

    // ---- Mutations ---------------------------------------------------------
    /**
     * @param {Sku} sku
     * @param {number} qty
     */
    function addItem(sku, qty) {
        const entry = cart.find((e) => e.sku === sku);
        if (entry) {
            const before = offerActive(sku, entry.qty);
            entry.qty += qty;
            const li = lineEls.get(sku);
            if (!li) { renderAll(); return; }
            li.innerHTML = lineInner(sku, entry.qty);
            pulse(li.querySelector(".line-qty"));
            if (!before && offerActive(sku, entry.qty)) animateOfferIn(li);
        } else {
            const e = { sku, qty };
            cart.push(e);
            const list = ensureList();
            const li = makeLine(e);
            list.appendChild(li);
            lineEls.set(sku, li);
            animateIn(li);
        }
        refreshTotals();
    }

    /**
     * @param {Sku} sku
     * @param {number} delta
     */
    function changeItem(sku, delta) {
        const entry = cart.find((e) => e.sku === sku);
        if (!entry) return;
        const before = offerActive(sku, entry.qty);
        entry.qty += delta;
        if (entry.qty <= 0) { removeItem(sku); return; }
        const li = lineEls.get(sku);
        if (!li) { renderAll(); return; }
        const after = offerActive(sku, entry.qty);

        if (before && !after) {
            // Offer just stopped applying. Update the qty/price live so the press
            // feels instant, animate the tag + struck price out, then re-render
            // the line plain (fixes the dec icon/labels for the new quantity).
            const r = calc(sku, entry.qty);
            const priceEl = li.querySelector(".line-price");
            if (priceEl) priceEl.textContent = gbp(r.linePence);
            const qtyEl = li.querySelector(".line-qty");
            if (!qtyEl) { li.innerHTML = lineInner(sku, entry.qty); refreshTotals(); return; }
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

    /**
     * @param {Sku} sku
     */
    function removeItem(sku) {
        const li = lineEls.get(sku);
        const focusTarget = li && li.contains(document.activeElement)
            ? focusTargetAfterLineRemoval(li)
            : null;
        cart = cart.filter((e) => e.sku !== sku);
        lineEls.delete(sku);
        const after = () => { if (cart.length === 0) showEmpty(); refreshTotals(); };
        focusSoon(focusTarget);
        if (li) animateOut(li, after); else after();
    }

    function clearSale() {
        if (cart.length === 0) return;
        cart = [];
        const els = [...lineEls.values()];
        lineEls.clear();
        let pending = els.length;
        const finish = () => { showEmpty(); focusDefaultAddTarget(); };
        els.forEach((li) => animateOut(li, () => { if (--pending === 0) finish(); }));
        if (els.length === 0) finish();
        refreshTotals();
    }

    // ---- Errors + toast ----------------------------------------------------
    /**
     * @param {string} raw
     */
    function showError(raw) {
        alertEl.innerHTML =
            `<p class="banner-error" role="alert"><span><strong>“${escapeHtml(raw)}” isn’t a known SKU.</strong> Check the keys and try again.</span></p>`;
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
    /** @type {Map<Sku, number>} */
    const addBatch = new Map(); // sku -> qty added while the toast is up
    let toastTimer = null;
    let addDebounceTimer = null;
    let lastAddToastAt = 0;
    let announceFrame = 0;

    /**
     * @param {string} msg
     */
    function announce(msg) {
        cancelAnimationFrame(announceFrame);
        liveRegion.textContent = "";
        announceFrame = requestAnimationFrame(() => {
            liveRegion.textContent = msg;
        });
    }

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
        cancelAnimationFrame(announceFrame);
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
    /**
     * @param {string} msg
     * @param {boolean} pulse
     */
    function renderToast(msg, pulse) {
        clearTimeout(toastTimer);
        toastEl.getAnimations().forEach((animation) => animation.cancel());
        const wasVisible = !toastEl.hidden;
        toastMsg.textContent = msg;
        announce(msg);
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
    /**
     * @param {string} msg
     */
    function showToast(msg) {
        clearTimeout(addDebounceTimer);
        addBatch.clear();
        renderToast(msg, false);
    }

    /**
     * @returns {string}
     */
    function addToastMessage() {
        if (addBatch.size === 1) {
            const [s, q] = [...addBatch][0];
            return `Added ${q} × ${CATALOG[s].name} to the sale.`;
        }
        const total = [...addBatch.values()].reduce((a, b) => a + b, 0);
        return `Added ${total} items to the sale.`;
    }

    /**
     * @param {boolean} pulseVisible
     */
    function flushAddToast(pulseVisible) {
        clearTimeout(addDebounceTimer);
        if (addBatch.size === 0) return;
        renderToast(addToastMessage(), pulseVisible);
        lastAddToastAt = performance.now();
    }

    // Add feedback, coalesced into the visible toast.
    /**
     * @param {Sku} sku
     * @param {number} qty
     */
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
    /**
     * @param {string | number} v
     * @returns {number}
     */
    const clampQty = (v) => Math.max(1, parseInt(String(v), 10) || 1);

    // Manual / scan / typed entry. This is the only path that can produce an
    // unknown SKU (the tiles can't), so it owns the validation error state.
    function manualAdd() {
        const raw = (scanInput.value || "").trim();
        if (!raw) return;
        const sku = raw.toUpperCase();
        if (!isSku(sku)) { showError(raw); return; }
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
        if (submitter instanceof HTMLButtonElement && submitter.name === "sku" && isSku(submitter.value)) {
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

    // Per-line steppers (delegated). The buttons are real submit controls for
    // the no-JS contract; the mockup intercepts the form submit for the live demo.
    basketEl.addEventListener("submit", (e) => {
        e.preventDefault();
        const submitter = e.submitter;
        if (!(submitter instanceof HTMLButtonElement)) return;
        const sku = submitter.dataset.sku;
        if (!sku || !isSku(sku)) return;
        changeItem(sku, submitter.dataset.act === "inc" ? 1 : -1);
    });

    // Clear is destructive: confirm via the native dialog first. Esc / backdrop /
    // "Keep sale" all resolve to a non-"clear" returnValue and leave the sale.
    clearForm.addEventListener("submit", (e) => {
        e.preventDefault();
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
    checkoutForm.addEventListener("submit", (e) => {
        e.preventDefault();
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
