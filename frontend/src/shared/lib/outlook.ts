interface OutlookDraft {
  to: string;
  cc: string | null;
  subject: string;
  body: string;
}

/**
 * Opens the system's default mail client (Outlook on corporate machines) with
 * the draft pre-filled. Uses explicit encodeURIComponent for subject/body so
 * Outlook reads spaces as spaces rather than '+' (URLSearchParams encodes
 * spaces as '+', which Outlook does not decode correctly in all versions).
 */
export function openInOutlook(draft: OutlookDraft): void {
  const subject = encodeURIComponent(draft.subject);
  // Normalise line endings: Outlook expects CRLF (%0D%0A) in the body.
  const body = encodeURIComponent(
    draft.body.replace(/\r?\n/g, '\r\n'),
  );

  let href = `mailto:${encodeURIComponent(draft.to)}?subject=${subject}&body=${body}`;
  if (draft.cc) {
    href += `&cc=${encodeURIComponent(draft.cc)}`;
  }

  window.location.href = href;
}

/**
 * Triggers a browser download for a Blob received from the server.
 * Creates a temporary object URL, clicks a hidden anchor, then revokes the URL.
 */
export function saveEml(blob: Blob, filename: string): void {
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
