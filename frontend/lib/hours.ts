/**
 * Bildirim saati ve saat dilimi yardimcilari.
 *
 * Saat artik kullanicinin KENDI saat diliminde saklaniyor; bu yuzden arayuzde
 * ayrica "UTC karsiligi" gostermeye gerek yok - kullanici ne secerse o saatte
 * bildirim aliyor (yaz/kis saati degisse bile).
 */

export const HOUR_OPTIONS = Array.from({ length: 24 }, (_, hour) => ({
  value: hour,
  label: `${String(hour).padStart(2, "0")}:00`,
}));

/** Tarayicidan algilanan IANA saat dilimi (orn. "Europe/Istanbul"). */
export function detectTimeZone(): string {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || "UTC";
  } catch {
    return "UTC";
  }
}

/**
 * Secilebilir saat dilimi listesi.
 * Modern tarayicilar tam IANA listesini verir; desteklenmeyen ortamlarda
 * yaygin bolgelerden olusan bir yedek liste kullanilir.
 */
export function listTimeZones(): string[] {
  const supported = (
    Intl as unknown as { supportedValuesOf?: (key: string) => string[] }
  ).supportedValuesOf;

  if (typeof supported === "function") {
    try {
      return supported("timeZone");
    } catch {
      // yedek listeye dus
    }
  }

  return [
    "UTC",
    "Europe/Istanbul",
    "Europe/London",
    "Europe/Berlin",
    "Europe/Paris",
    "Europe/Moscow",
    "America/New_York",
    "America/Chicago",
    "America/Los_Angeles",
    "America/Sao_Paulo",
    "Asia/Dubai",
    "Asia/Kolkata",
    "Asia/Shanghai",
    "Asia/Tokyo",
    "Australia/Sydney",
  ];
}

/**
 * Bir saat diliminin UTC farkini okunur bicimde dondurur (orn. "UTC+3").
 * Kullanicinin listeden dogru dilimi secmesini kolaylastirir.
 */
export function formatUtcOffset(timeZone: string): string {
  try {
    const now = new Date();
    const parts = new Intl.DateTimeFormat("en-US", {
      timeZone,
      timeZoneName: "shortOffset",
    }).formatToParts(now);

    return parts.find((p) => p.type === "timeZoneName")?.value ?? "";
  } catch {
    return "";
  }
}

/** Saat diliminde su anki zamani "HH:mm" olarak dondurur. */
export function currentTimeIn(timeZone: string): string {
  try {
    return new Intl.DateTimeFormat("tr-TR", {
      timeZone,
      hour: "2-digit",
      minute: "2-digit",
    }).format(new Date());
  } catch {
    return "";
  }
}
