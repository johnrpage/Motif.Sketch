// Autosave in IndexedDB, with the file handle beside the work, and one owning tab at a time.
// Tabs coordinate with Web Locks (who owns autosave) and a BroadcastChannel ("use this tab instead").

const DB = "motif-sketch", STORE = "work", KEY = "current", LOCK = "motif-sketch-autosave";
const handles = (globalThis.motifFileHandles ??= new Map());
const channel = "BroadcastChannel" in window ? new BroadcastChannel("motif-sketch") : null;

let dotnet = null;
let release = null;      // resolves the held lock's promise, letting the lock go
let pending = null;      // the latest work not yet written
let writing = null;      // the write in flight

export function init(reference) {
  dotnet = reference;
  if (channel) channel.onmessage = async e => {
    if (e.data !== "take-over" || !release) return;
    await flush();                                  // the last autosave lands before handing over
    const letGo = release;
    release = null;
    await dotnet.invokeMethodAsync("OnReleased");
    letGo();
  };
}

function hold(lock, resolve) {
  resolve(true);
  return new Promise(r => (release = r));
}

export function claim() {
  if (!navigator.locks) return true;
  return new Promise(resolve =>
    navigator.locks.request(LOCK, { ifAvailable: true }, lock => (lock ? hold(lock, resolve) : resolve(false))));
}

export function takeOver() {
  if (!navigator.locks) return;
  return new Promise(resolve => {
    navigator.locks.request(LOCK, lock => hold(lock, resolve));
    channel?.postMessage("take-over");
  });
}

function open() {
  return new Promise((resolve, reject) => {
    const request = indexedDB.open(DB, 1);
    request.onupgradeneeded = () => request.result.createObjectStore(STORE);
    request.onsuccess = () => resolve(request.result);
    request.onerror = () => reject(request.error);
  });
}

async function run(mode, action) {
  const db = await open();
  try {
    return await new Promise((resolve, reject) => {
      const tx = db.transaction(STORE, mode);
      const request = action(tx.objectStore(STORE));
      tx.oncomplete = () => resolve(request.result);
      tx.onerror = () => reject(tx.error);
    });
  } finally {
    db.close();
  }
}

// Returns [json, fileName, handleId, unsaved] or null.
export async function restore() {
  const work = await run("readonly", store => store.get(KEY));
  if (!work) return null;
  let handleId = null;
  if (work.handle) {
    handleId = `restored-${Date.now().toString(36)}`;
    handles.set(handleId, work.handle);
  }
  return [work.json, work.fileName ?? null, handleId, work.unsaved ? "1" : ""];
}

export function store(json, fileName, handleId, unsaved) {
  pending = { json, fileName, handle: handleId ? handles.get(handleId) ?? null : null, unsaved };
  return flush();
}

// Writes are coalesced: while one is in flight, only the newest pending work is written next.
async function flush() {
  while (pending || writing) {
    if (writing) { await writing; continue; }
    const work = pending;
    pending = null;
    writing = run("readwrite", store => store.put(work, KEY)).finally(() => (writing = null));
  }
}
