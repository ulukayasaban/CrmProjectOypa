/**
 * Triggers a browser file download for the given Blob.
 * Creates a temporary object URL, clicks a hidden anchor, then revokes the URL.
 * Mirrors the saveEml pattern in outlook.ts.
 */
export function saveBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.style.display = 'none';
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
