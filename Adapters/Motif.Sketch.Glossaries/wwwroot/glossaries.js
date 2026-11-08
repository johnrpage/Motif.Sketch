// The project's glossary (motif/glossary.md): opened, then written back to the same file where the
// browser has the File System Access API, else downloaded. Handles stay in this module: unlike a project
// file's, a glossary's handle isn't autosaved, so it lasts only until the page closes.
const handles = new Map();
let counter = 0;
const types = [{ description: "Markdown glossary", accept: { "text/markdown": [".md"] } }];

function keep(handle) {
  const id = `glossary-${counter++}`;
  handles.set(id, handle);
  return id;
}

function download(text, name) {
  const url = URL.createObjectURL(new Blob([text], { type: "text/markdown" }));
  const a = document.createElement("a");
  a.href = url;
  a.download = name;
  document.body.appendChild(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 10000);
}

async function write(handle, text) {
  const writable = await handle.createWritable();
  await writable.write(text);
  await writable.close();
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
    input.accept = ".md,.markdown,text/markdown";
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
    download(text, suggestedName);
    return [suggestedName, null];
  }
  const existing = handleId ? handles.get(handleId) : null;
  if (existing) {
    const options = { mode: "readwrite" };
    if ((await existing.queryPermission(options)) !== "granted" &&
        (await existing.requestPermission(options)) !== "granted")
      throw new Error("the browser wasn't allowed to write to that file.");
    await write(existing, text);
    return [existing.name, handleId];
  }
  let handle;
  try {
    handle = await window.showSaveFilePicker({ suggestedName, types });
  } catch (e) {
    if (e.name === "AbortError") return null;
    throw e;
  }
  await write(handle, text);
  return [handle.name, keep(handle)];
}
