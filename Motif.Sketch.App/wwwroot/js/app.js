// The Web App's own browser plumbing. Everything here is thin: the logic stays in C#.

let unsaved = false;
window.addEventListener("beforeunload", e => {
  if (!unsaved) return;
  e.preventDefault();
  e.returnValue = "";
});

export function setUnsaved(value) { unsaved = value; }

export function fragment() { return location.hash; }
export function pageUrl() { return location.href; }
export function clearFragment() { history.replaceState(null, "", location.pathname + location.search); }

// Keys the app handles itself. Text fields keep their own editing keys (and their own undo).
export function listenForKeys(dotnet) {
  document.addEventListener("keydown", e => {
    const t = e.target;
    const typing = t instanceof HTMLElement && (t.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(t.tagName));
    const ctrl = e.ctrlKey || e.metaKey;
    const key = e.key.length === 1 ? e.key.toLowerCase() : e.key;
    let name = null;
    if (ctrl && key === "s") name = e.shiftKey ? "SaveAs" : "Save";
    else if (e.key === "Escape") name = "Escape";
    else if (e.altKey && !ctrl && ["1", "2", "3"].includes(e.code.replace("Digit", ""))) name = "Layout" + e.code.replace("Digit", "");
    else if (!typing && ctrl && key === "z") name = e.shiftKey ? "Redo" : "Undo";
    else if (!typing && ctrl && key === "y") name = "Redo";
    else if (!typing && e.key === "Delete") name = "Delete";
    else if (!typing && e.key === "F2") name = "Rename";
    if (!name) return;
    e.preventDefault();
    // Commit a field being edited before saving or leaving it.
    if (typing && (name.startsWith("Save") || name === "Escape")) t.blur();
    dotnet.invokeMethodAsync("OnKey", name);
  });
}

// Wheel zoom needs a non-passive listener so the page itself doesn't zoom or scroll.
export function listenForWheel(element, dotnet) {
  element.addEventListener("wheel", e => {
    e.preventDefault();
    const r = element.getBoundingClientRect();
    dotnet.invokeMethodAsync("OnWheel", e.clientX - r.left, e.clientY - r.top, e.deltaY, e.deltaMode);
  }, { passive: false });
  const observer = new ResizeObserver(() => {
    const r = element.getBoundingClientRect();
    dotnet.invokeMethodAsync("OnResize", r.width, r.height);
  });
  observer.observe(element);
  observers.set(element, observer);
}

const observers = new WeakMap();

export function stopListening(element) {
  observers.get(element)?.disconnect();
}

export function rect(element) {
  if (!element) return [0, 0, 0, 0];
  const r = element.getBoundingClientRect();
  return [r.left, r.top, r.width, r.height];
}

export function capture(element, pointerId) {
  try { element.setPointerCapture(pointerId); } catch { /* the pointer may already be gone */ }
}

// What the pointer is over: the nearest data-hit, ignoring anything marked as a drag ghost.
export function hitAt(x, y) {
  for (const el of document.elementsFromPoint(x, y)) {
    if (el.closest(".ghost-drag")) continue;
    const hit = el.closest("[data-hit]");
    if (hit) return hit.getAttribute("data-hit");
    if (el.closest(".canvas")) return null;
  }
  return null;
}

export function focus(selector) {
  const el = document.querySelector(selector);
  if (el) { el.focus(); el.select?.(); }
}

// Theme: follows the system, with an override kept in this browser only.
export function theme() {
  try { return localStorage.getItem("motif-theme") || "system"; } catch { return "system"; }
}

export function setTheme(value) {
  try { localStorage.setItem("motif-theme", value); } catch { /* storage may be blocked */ }
  applyTheme(value);
}

export function applyTheme(value) {
  if (value === "light" || value === "dark") document.documentElement.setAttribute("data-theme", value);
  else document.documentElement.removeAttribute("data-theme");
}

export function preference(key) {
  try { return localStorage.getItem(key); } catch { return null; }
}

export function setPreference(key, value) {
  try { localStorage.setItem(key, value); } catch { /* storage may be blocked */ }
}

// Viewport classes: phones are view-only, narrow screens get no Split layout.
export function watchViewport(dotnet) {
  const phone = matchMedia("(max-width: 599px)");
  const narrow = matchMedia("(max-width: 899px)");
  const report = () => dotnet.invokeMethodAsync("OnViewport", phone.matches, narrow.matches);
  phone.addEventListener("change", report);
  narrow.addEventListener("change", report);
  return [phone.matches, narrow.matches];
}
