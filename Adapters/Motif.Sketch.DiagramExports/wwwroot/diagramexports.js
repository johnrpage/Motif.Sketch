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

// Returns false if the user cancelled.
export async function save(text, suggestedName) {
  if (!("showSaveFilePicker" in window)) {
    download(text, suggestedName, "image/svg+xml");
    return true;
  }
  return (await pickAndWrite(text, suggestedName, [{ description: "SVG picture", accept: { "image/svg+xml": [".svg"] } }])) !== null;
}
