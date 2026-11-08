// Saves text to a file: the File System Access picker where there is one, else a download.
function download(text, name, mime) {
  const url = URL.createObjectURL(new Blob([text], { type: mime }));
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10000);
}

async function pickAndWrite(text, suggestedName, types) {
  let handle;
  try {
    handle = await window.showSaveFilePicker({ suggestedName, types });
  } catch (e) {
    if (e.name === "AbortError") return null;
    throw e;
  }
  await write(handle, text);
  return handle;
}

async function write(handle, text) {
  const writable = await handle.createWritable();
  await writable.write(text);
  await writable.close();
}

// File handles live here, keyed by opaque ids, so the Autosave adapter can store them by id too.
// This map is the one browser contract shared between adapters.
const handles = (globalThis.motifFileHandles ??= new Map());
let counter = 0;
// Chrome refuses picker extensions over 16 characters, and ".motifsketch.json" is 17. ".json" covers it:
// the suggested name still ends in ".motifsketch.json", and the open picker still lists those files.
const types = [{ description: "Motif Sketch project", accept: { "application/json": [".json"] } }];

function keep(handle) {
  const id = `file-${Date.now().toString(36)}-${counter++}`;
  handles.set(id, handle);
  return id;
}

export function canSaveInPlace() {
  return "showSaveFilePicker" in window;
}

// Returns [name, text, handleId] or null if the user cancelled.
export async function open() {
  if ("showOpenFilePicker" in window) {
    let handle;
    try {
      [handle] = await window.showOpenFilePicker({ types });
    } catch (e) {
      if (e.name === "AbortError") return null;
      throw e;
    }
    const file = await handle.getFile();
    return [file.name, await file.text(), keep(handle)];
  }
  return await new Promise((resolve, reject) => {
    const input = document.createElement("input");
    input.type = "file";
    input.accept = ".json,.motifsketch.json,application/json";
    input.addEventListener("change", async () => {
      const file = input.files?.[0];
      if (!file) return resolve(null);
      try { resolve([file.name, await file.text(), null]); } catch (e) { reject(e); }
    });
    input.addEventListener("cancel", () => resolve(null));
    input.click();
  });
}

// Returns [name, handleId] or null if the user cancelled.
export async function save(text, suggestedName, handleId) {
  if (!canSaveInPlace()) {
    download(text, suggestedName, "application/json");
    return [suggestedName, null];
  }
  const existing = handleId ? handles.get(handleId) : null;
  if (existing) {
    // After a refresh the browser asks once more for permission to write to the same file.
    const options = { mode: "readwrite" };
    if ((await existing.queryPermission(options)) !== "granted" &&
        (await existing.requestPermission(options)) !== "granted")
      throw new Error("the browser wasn't allowed to write to that file. Use Save as to pick it again.");
    await write(existing, text);
    return [existing.name, handleId];
  }
  const handle = await pickAndWrite(text, suggestedName, types);
  return handle ? [handle.name, keep(handle)] : null;
}
