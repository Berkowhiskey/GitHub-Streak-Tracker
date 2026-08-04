/**
 * Backend API istemcisi.
 *
 * Kimlik dogrulama HttpOnly cerez uzerinden yurutulur; bu yuzden her istekte
 * `credentials: "include"` gonderilir ve token'a JavaScript'ten hic dokunulmaz.
 */

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5157";

/** Backend'in ProblemDetails yanitlarini tasiyan hata tipi. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }

  /** Oturum yoksa veya suresi dolduysa true. */
  get isUnauthorized() {
    return this.status === 401;
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      ...init,
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        ...init?.headers,
      },
    });
  } catch {
    // Ag hatasi: backend kapali veya erisilemiyor.
    throw new ApiError(
      "Sunucuya ulasilamadi. Backend'in calistigindan emin misin?",
      0,
    );
  }

  if (!response.ok) {
    let message = `Istek basarisiz oldu (${response.status}).`;

    try {
      const problem = await response.json();
      message = problem?.detail ?? problem?.title ?? message;
    } catch {
      // Yanit JSON degilse varsayilan mesaj kullanilir.
    }

    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// ---------------------------------------------------------------------------
// Tipler - backend DTO'lariyla birebir eslesir
// ---------------------------------------------------------------------------

export interface CurrentUser {
  id: string;
  gitHubUsername: string;
  email: string | null;
  avatarUrl: string | null;
  hasAcceptedTerms: boolean;
  isActive: boolean;
  /** Bildirim saati, kullanicinin kendi saat diliminde (0-23). */
  preferredNotificationHour: number;
  /** IANA saat dilimi kimligi (orn. "Europe/Istanbul"). */
  timeZoneId: string;
  /** Dil kodu: "tr" veya "en". */
  language: string;
  notificationRepoName: string | null;
  notificationIssueNumber: number | null;
  /** GitHub App kurulu mu? Kurulu degilse bildirimler gonderilemez. */
  gitHubAppInstalled: boolean;
}

export interface AppInstallationStatus {
  installed: boolean;
  installationUrl: string;
  /** Sunucuda GitHub App yapilandirilmis mi (AppId/PrivateKey tanimli mi)? */
  appConfigured: boolean;
}

export interface StreakStatus {
  currentStreak: number;
  longestStreak: number;
  hasCommittedToday: boolean;
  lastCommitDate: string | null;
  lastCheckedAt: string;
}

export interface CalendarDay {
  date: string;
  contributionCount: number;
}

export interface BadgeSnippets {
  badgeUrl: string;
  badgeUrlLight: string;
  markdown: string;
  html: string;
}

/** Backend'deki BadgeTheme ile birebir eslesir. */
export type BadgeTheme =
  | "dark"
  | "light"
  | "dracula"
  | "tokyo-night"
  | "nord"
  | "catppuccin";

export type BadgeVariant = "full" | "compact" | "max";

/** Backend'deki BadgeSettings ile birebir eslesir. */
export interface BadgeSettings {
  theme: BadgeTheme;
  variant: BadgeVariant;
  animated: boolean;
  flameFrom: string | null;
  flameTo: string | null;
  background: string | null;
  border: string | null;
}

export type RankKey = "spark" | "flame" | "fire" | "blaze" | "legend";

/**
 * Rutbeler ve esikleri. Esikler backend'deki StreakRankExtensions.RankFor ile
 * ayni; milestone bildirimleriyle de ortak (1 / 7 / 30 / 100 / 365).
 */
export const RANKS: { key: RankKey; threshold: number; tr: string; en: string }[] = [
  { key: "spark", threshold: 1, tr: "KIVILCIM", en: "SPARK" },
  { key: "flame", threshold: 7, tr: "ALEV", en: "FLAME" },
  { key: "fire", threshold: 30, tr: "ATES", en: "FIRE" },
  { key: "blaze", threshold: 100, tr: "YANGIN", en: "BLAZE" },
  { key: "legend", threshold: 365, tr: "EFSANE", en: "LEGEND" },
];

/** Secicide gosterilecek temalar; etiketler ceviri gerektirmeyen ozel adlardir. */
export const BADGE_THEMES: { value: BadgeTheme; label: string }[] = [
  { value: "dark", label: "Dark" },
  { value: "light", label: "Light" },
  { value: "dracula", label: "Dracula" },
  { value: "tokyo-night", label: "Tokyo Night" },
  { value: "nord", label: "Nord" },
  { value: "catppuccin", label: "Catppuccin" },
];

export interface OnboardingResult {
  repositoryName: string;
  issueNumber: number;
  wasAlreadySetUp: boolean;
  currentStreak: number;
  longestStreak: number;
  hasCommittedToday: boolean;
  lastCommitDate: string | null;
}

export interface NotificationResult {
  sent: boolean;
  reason: string;
}

// ---------------------------------------------------------------------------
// API cagrilari
// ---------------------------------------------------------------------------

export const api = {
  /** GitHub girisini baslatan adres; tarayici bu adrese yonlendirilir. */
  loginUrl: () => `${API_BASE_URL}/api/v1/auth/github/login`,

  getCurrentUser: () => request<CurrentUser>("/api/v1/users/me"),

  logout: () =>
    request<{ loggedOut: boolean }>("/api/v1/auth/logout", { method: "POST" }),

  completeOnboarding: (
    acceptTerms: boolean,
    preferredNotificationHour?: number,
    timeZoneId?: string,
  ) =>
    request<OnboardingResult>("/api/v1/onboarding/complete", {
      method: "POST",
      body: JSON.stringify({ acceptTerms, preferredNotificationHour, timeZoneId }),
    }),

  getStreak: () => request<StreakStatus>("/api/v1/streaks/me"),

  refreshStreak: () =>
    request<StreakStatus>("/api/v1/streaks/me/refresh", { method: "POST" }),

  /**
   * Gun sayisi bilerek burada sabitlenmiyor: ust sinir GitHub'in 1 yillik
   * kisitina bagli ve backend'de tanimli. Iki yerde tutulsaydi biri degistiginde
   * digeri sinirin disinda kalip 400 dondururdu.
   */
  getCalendar: (days?: number) =>
    request<CalendarDay[]>(
      days
        ? `/api/v1/streaks/me/calendar?days=${days}`
        : "/api/v1/streaks/me/calendar",
    ),

  /**
   * Onizleme adresi. Gorunumu belirleyen her sey adrese yazilir; aksi halde
   * adres degismedigi icin tarayici rozeti onbellekten gosterir ve secim
   * ekrana yansimaz.
   */
  badgeUrl: (
    username: string,
    options: {
      lang: string;
      theme: BadgeTheme;
      variant: BadgeVariant;
      animated?: boolean;
      flameFrom?: string | null;
      flameTo?: string | null;
      background?: string | null;
      border?: string | null;
    },
  ) => {
    const params = new URLSearchParams({ lang: options.lang });

    params.set("theme", options.theme);
    params.set("variant", options.variant);

    if (options.animated === false) params.set("animated", "false");
    if (options.flameFrom) params.set("flameFrom", options.flameFrom);
    if (options.flameTo) params.set("flameTo", options.flameTo);
    if (options.background) params.set("bg", options.background);
    if (options.border) params.set("border", options.border);

    return `${API_BASE_URL}/api/v1/badges/${username}.svg?${params}`;
  },

  /**
   * Tek bir rutbenin alev sekli. Kullaniciya ozel veri icermez;
   * kazanilmamis rutbeler `locked` ile sonuk cizilir.
   */
  flamePreviewUrl: (
    rank: RankKey,
    options: {
      theme: BadgeTheme;
      locked: boolean;
      flameFrom?: string | null;
      flameTo?: string | null;
    },
  ) => {
    const params = new URLSearchParams({ theme: options.theme });

    if (options.locked) params.set("locked", "true");
    if (options.flameFrom) params.set("flameFrom", options.flameFrom);
    if (options.flameTo) params.set("flameTo", options.flameTo);

    return `${API_BASE_URL}/api/v1/badges/flames/${rank}.svg?${params}`;
  },

  getBadgeSettings: () => request<BadgeSettings>("/api/v1/users/me/badge-settings"),

  updateBadgeSettings: (settings: {
    theme: BadgeTheme;
    variant: BadgeVariant;
    animated: boolean;
    flameFrom: string | null;
    flameTo: string | null;
    background: string | null;
    border: string | null;
  }) =>
    request<BadgeSettings>("/api/v1/users/me/badge-settings", {
      method: "PUT",
      body: JSON.stringify(settings),
    }),

  /**
   * README'ye yapistirilacak kod parcaciklari.
   * Dil/tema/varyant aciktan gonderilir: tercih kaydedilmeyi beklemeden
   * dogru kod uretilsin ve rozet onbellegi dogru anahtarla calissin.
   */
  getBadgeSnippets: (options?: {
    lang?: string;
    theme?: BadgeTheme;
    variant?: BadgeVariant;
  }) => {
    const params = new URLSearchParams();

    if (options?.lang) params.set("lang", options.lang);
    if (options?.theme) params.set("theme", options.theme);
    if (options?.variant) params.set("variant", options.variant);

    const query = params.toString();

    return request<BadgeSnippets>(
      query ? `/api/v1/users/me/badge?${query}` : "/api/v1/users/me/badge",
    );
  },

  /** GitHub App kurulumunu GitHub'a sorarak dogrular. */
  getAppStatus: () =>
    request<AppInstallationStatus>("/api/v1/users/me/app-status"),

  updatePreferences: (preferences: {
    preferredNotificationHour?: number;
    timeZoneId?: string;
    language?: string;
    isActive?: boolean;
  }) =>
    request<CurrentUser>("/api/v1/users/me/preferences", {
      method: "PATCH",
      body: JSON.stringify(preferences),
    }),

  sendTestNotification: () =>
    request<NotificationResult>("/api/v1/notifications/test", {
      method: "POST",
    }),

  deleteAccount: () =>
    request<{ deleted: boolean; message: string }>("/api/v1/users/me", {
      method: "DELETE",
    }),
};
