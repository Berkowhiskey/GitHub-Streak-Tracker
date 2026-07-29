/**
 * Projede kullanilan SVG ikonlar.
 * Emoji yerine vektorel cizim tercih edildi; her platformda ayni gorunur.
 */

export function GitHubIcon({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden="true">
      <path d="M12 .5C5.73.5.5 5.73.5 12c0 5.08 3.29 9.39 7.86 10.91.58.11.79-.25.79-.56 0-.28-.01-1.02-.02-2-3.2.7-3.88-1.54-3.88-1.54-.52-1.33-1.28-1.68-1.28-1.68-1.05-.72.08-.7.08-.7 1.16.08 1.77 1.19 1.77 1.19 1.03 1.77 2.7 1.26 3.36.96.1-.75.4-1.26.73-1.55-2.55-.29-5.24-1.28-5.24-5.69 0-1.26.45-2.29 1.19-3.09-.12-.29-.52-1.46.11-3.05 0 0 .97-.31 3.18 1.18a11 11 0 0 1 5.79 0c2.2-1.49 3.17-1.18 3.17-1.18.63 1.59.23 2.76.12 3.05.74.8 1.18 1.83 1.18 3.09 0 4.42-2.69 5.39-5.25 5.68.41.36.78 1.06.78 2.14 0 1.55-.01 2.8-.01 3.18 0 .31.21.68.8.56A11.5 11.5 0 0 0 23.5 12C23.5 5.73 18.27.5 12 .5z" />
    </svg>
  );
}

export function FlameIcon({
  className,
  muted = false,
}: {
  className?: string;
  muted?: boolean;
}) {
  return (
    <svg viewBox="0 0 24 24" className={className} aria-hidden="true">
      {!muted && (
        <defs>
          <linearGradient id="flameGradientUi" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="#ffa028" />
            <stop offset="100%" stopColor="#f0483e" />
          </linearGradient>
        </defs>
      )}
      <path
        d="M12 2c0 4-3 5.5-5 8-1.5 1.9-2 3.6-2 5.5C5 19.5 8 22 12 22s7-2.5 7-6.5c0-1.9-.5-3.6-2-5.5-2-2.5-5-4-5-8z"
        fill={muted ? "currentColor" : "url(#flameGradientUi)"}
        opacity={muted ? 0.35 : 1}
      />
      {!muted && (
        <path
          d="M12 12.5c0 2-1.4 2.8-2.4 4-.7.9-1 1.7-1 2.6 0 1.9 1.5 2.9 3.4 2.9s3.4-1 3.4-2.9c0-.9-.3-1.7-1-2.6-1-1.2-2.4-2-2.4-4z"
          fill="#ffd75e"
          opacity={0.95}
        />
      )}
    </svg>
  );
}

export function CheckIcon({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.5"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M20 6 9 17l-5-5" />
    </svg>
  );
}

export function CopyIcon({ className }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <rect width="14" height="14" x="8" y="8" rx="2" ry="2" />
      <path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2" />
    </svg>
  );
}
