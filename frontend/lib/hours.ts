/**
 * Bildirim saati secenekleri.
 * Backend UTC ile calisir; kullaniciya ayrica yerel saat karsiligi gosterilir.
 */
export const HOUR_OPTIONS = Array.from({ length: 24 }, (_, hour) => ({
  value: hour,
  label: `${String(hour).padStart(2, "0")}:00 UTC (yerel ${formatLocalHour(hour)})`,
}));

/** UTC saatinin, tarayicinin bulundugu saat diliminde karsiligi. */
export function formatLocalHour(utcHour: number): string {
  const date = new Date();
  date.setUTCHours(utcHour, 0, 0, 0);

  return date.toLocaleTimeString("tr-TR", {
    hour: "2-digit",
    minute: "2-digit",
  });
}
