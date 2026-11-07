export async function copy(text) {
  await navigator.clipboard.writeText(text);
}
