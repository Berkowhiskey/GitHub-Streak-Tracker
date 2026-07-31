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

  getCalendar: (days = 364) =>
    request<CalendarDay[]>(`/api/v1/streaks/me/calendar?days=${days}`),

  /** Dil aciktan gonderilir: tercih kaydedilmeyi beklemeden dogru kod uretilsin. */
  getBadgeSnippets: (lang?: string) =>
    request<BadgeSnippets>(
      lang ? `/api/v1/users/me/badge?lang=${lang}` : "/api/v1/users/me/badge",
    ),

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
