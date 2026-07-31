"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import {
  api,
  ApiError,
  API_BASE_URL,
  type AppInstallationStatus,
  type BadgeSnippets,
  type CalendarDay,
  type CurrentUser,
  type StreakStatus,
} from "@/lib/api";
import { AppInstallNotice } from "@/components/app-install-notice";
import { useLanguage } from "@/components/language-provider";
import { LanguageSwitcher } from "@/components/language-switcher";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { FlameIcon } from "@/components/icons";
import { ContributionHeatmap } from "@/components/contribution-heatmap";
import { CopyField } from "@/components/copy-field";
import {
  HOUR_OPTIONS,
  currentTimeIn,
  detectTimeZone,
  formatUtcOffset,
  listTimeZones,
} from "@/lib/hours";

export default function DashboardPage() {
  const router = useRouter();
  const { t, locale } = useLanguage();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [streak, setStreak] = useState<StreakStatus | null>(null);
  const [calendar, setCalendar] = useState<CalendarDay[]>([]);
  const [badges, setBadges] = useState<BadgeSnippets | null>(null);
  const [appStatus, setAppStatus] = useState<AppInstallationStatus | null>(null);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const loadAll = useCallback(async () => {
    try {
      const current = await api.getCurrentUser();

      // Kurulumu tamamlamamis kullanici once onay ekranina gitmeli.
      if (!current.hasAcceptedTerms || current.notificationIssueNumber === null) {
        router.replace("/onboarding");
        return;
      }

      setUser(current);

      // Takvim GitHub'a gittigi icin digerlerinden yavas olabilir;
      // hepsini paralel isteyip sayfayi tek seferde gosteriyoruz.
      const [streakData, calendarData, badgeData, appStatusData] = await Promise.all([
        api.getStreak(),
        api.getCalendar().catch(() => [] as CalendarDay[]),
        api.getBadgeSnippets(locale),
        // App kurulumu sorgulanamazsa panel yine de acilmali.
        api.getAppStatus().catch(() => null),
      ]);

      setStreak(streakData);
      setCalendar(calendarData);
      setBadges(badgeData);
      setAppStatus(appStatusData);
      setLoading(false);
    } catch (err) {
      if (err instanceof ApiError && err.isUnauthorized) {
        router.replace("/");
        return;
      }

      setError(err instanceof ApiError ? err.message : t.dashboard.loadError);
      setLoading(false);
    }
  }, [router]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  // Dil degisince README kod parcaciklari yeni dile gore yeniden uretilmeli.
  // Yalnizca kod parcaciklari tazeleniyor; takvim GitHub'a gittigi icin
  // sayfanin tamamini yeniden yuklemek gereksiz yavaslik olurdu.
  const snippetLocale = useRef(locale);

  useEffect(() => {
    if (snippetLocale.current === locale) return;

    snippetLocale.current = locale;
    api.getBadgeSnippets(locale).then(setBadges).catch(() => {});
  }, [locale]);

  async function run(key: string, action: () => Promise<string | null>) {
    setBusy(key);
    setMessage(null);
    setError(null);

    try {
      const result = await action();
      if (result) setMessage(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t.dashboard.actionError);
    } finally {
      setBusy(null);
    }
  }

  if (loading) {
    return (
      <main className="flex flex-1 items-center justify-center">
        <p className="text-sm text-muted-foreground">{t.common.loading}</p>
      </main>
    );
  }

  return (
    <main className="mx-auto w-full max-w-4xl flex-1 px-6 py-10">
      <header className="mb-8 flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          {user?.avatarUrl && (
            // Harici avatar; next/image yapilandirmasi gerektirmemesi icin <img> kullanildi.
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={user.avatarUrl}
              alt=""
              className="h-11 w-11 rounded-full border"
            />
          )}
          <div>
            <p className="font-semibold">{user?.gitHubUsername}</p>
            <p className="text-xs text-muted-foreground">{t.dashboard.panelSubtitle}</p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <LanguageSwitcher />
          <Button
            variant="ghost"
            onClick={async () => {
              await api.logout().catch(() => {});
              router.push("/");
            }}
          >
            {t.common.logout}
          </Button>
        </div>
      </header>

      {appStatus && (
        <AppInstallNotice
          status={appStatus}
          onInstalled={() => {
            setAppStatus({ ...appStatus, installed: true });
            setMessage(t.appInstall.verified);
          }}
        />
      )}

      {(message || error) && (
        <div
          role="status"
          className={`mb-6 rounded-lg border px-4 py-3 text-sm ${
            error
              ? "border-red-500/40 bg-red-500/10"
              : "border-emerald-500/40 bg-emerald-500/10"
          }`}
        >
          {error ?? message}
        </div>
      )}

      {/* --- Streak kartlari --- */}
      <section className="mb-8 grid gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t.dashboard.currentStreak}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex items-center gap-3">
            <FlameIcon
              className="h-9 w-9"
              muted={(streak?.currentStreak ?? 0) === 0}
            />
            <div>
              <p className="text-3xl font-bold">{streak?.currentStreak ?? 0}</p>
              <p className="text-xs text-muted-foreground">{t.dashboard.days}</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t.dashboard.record}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{streak?.longestStreak ?? 0}</p>
            <p className="text-xs text-muted-foreground">{t.dashboard.days}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              {t.dashboard.today}
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p
              className={`text-lg font-semibold ${
                streak?.hasCommittedToday ? "text-emerald-500" : "text-amber-500"
              }`}
            >
              {streak?.hasCommittedToday ? t.dashboard.committed : t.dashboard.notCommitted}
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              {streak?.hasCommittedToday
                ? t.dashboard.committedNote
                : t.dashboard.notCommittedNote}
            </p>
          </CardContent>
        </Card>
      </section>

      {/* --- Heatmap --- */}
      <Card className="mb-8">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>{t.dashboard.calendar}</CardTitle>
          <Button
            variant="outline"
            size="sm"
            disabled={busy === "refresh"}
            onClick={() =>
              run("refresh", async () => {
                const [updated, updatedCalendar] = await Promise.all([
                  api.refreshStreak(),
                  api.getCalendar(),
                ]);
                setStreak(updated);
                setCalendar(updatedCalendar);
                return t.dashboard.refreshed;
              })
            }
          >
            {busy === "refresh" ? t.dashboard.refreshing : t.dashboard.refresh}
          </Button>
        </CardHeader>
        <CardContent>
          <ContributionHeatmap days={calendar} />
        </CardContent>
      </Card>

      {/* --- Rozet --- */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>{t.dashboard.badge}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="overflow-x-auto rounded-lg border bg-muted/30 p-4">
            {/* Rozet backend tarafindan SVG olarak uretilir. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              // Dil URL'de: aksi halde adres degismedigi icin tarayici rozeti
              // onbellekten (max-age=300) gosterir ve dil degisimi ekrana yansimaz.
              src={`${API_BASE_URL}/api/v1/badges/${user?.gitHubUsername}.svg?lang=${locale}`}
              alt="Streak rozetin"
              width={400}
              height={120}
              className="max-w-full"
            />
          </div>

          {badges && (
            <div className="space-y-3">
              <CopyField label={t.dashboard.badgeMarkdown} value={badges.markdown} />
              <CopyField label={t.dashboard.badgeHtml} value={badges.html} />
            </div>
          )}

          <p className="text-xs text-muted-foreground">
            {t.dashboard.badgeLocalNote}
          </p>
        </CardContent>
      </Card>

      {/* --- Bildirim ayarlari --- */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>{t.dashboard.settings}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <label htmlFor="hour" className="text-sm font-medium">
                {t.dashboard.hourLabel}
              </label>
              <select
                id="hour"
                value={user?.preferredNotificationHour ?? 20}
                disabled={busy === "hour"}
                onChange={(e) => {
                  const nextHour = Number(e.target.value);
                  run("hour", async () => {
                    const updated = await api.updatePreferences({
                      preferredNotificationHour: nextHour,
                    });
                    setUser(updated);
                    return t.dashboard.hourUpdated;
                  });
                }}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              >
                {HOUR_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              <label htmlFor="timezone" className="text-sm font-medium">
                {t.dashboard.timeZone}
              </label>
              <select
                id="timezone"
                value={user?.timeZoneId ?? "UTC"}
                disabled={busy === "timezone"}
                onChange={(e) => {
                  const nextZone = e.target.value;
                  run("timezone", async () => {
                    const updated = await api.updatePreferences({ timeZoneId: nextZone });
                    setUser(updated);
                    // Seri "bugun"u saat dilimine gore hesaplandigi icin tazeliyoruz.
                    setStreak(await api.getStreak());
                    return t.dashboard.timeZoneUpdated;
                  });
                }}
                className="w-full rounded-md border bg-background px-3 py-2 text-sm"
              >
                {listTimeZones().map((zone) => (
                  <option key={zone} value={zone}>
                    {zone}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <p className="text-xs text-muted-foreground">
            {t.dashboard.timeZoneNote}{" "}
            <strong className="text-foreground">
              {user?.timeZoneId} {formatUtcOffset(user?.timeZoneId ?? "UTC")}
            </strong>{" "}
            {t.dashboard.timeZoneNoteSuffix} {currentTimeIn(user?.timeZoneId ?? "UTC")}).
            {user && detectTimeZone() !== user.timeZoneId && (
              <>
                {" "}
                {t.dashboard.browserShows}{" "}
                <strong className="text-foreground">{detectTimeZone()}</strong>{" "}
                {t.dashboard.browserShowsSuffix}
              </>
            )}
          </p>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div>
              <p className="text-sm font-medium">{t.dashboard.notifications}</p>
              <p className="text-xs text-muted-foreground">
                {t.dashboard.notificationsNote}
              </p>
            </div>
            <Button
              variant={user?.isActive ? "outline" : "default"}
              size="sm"
              disabled={busy === "active"}
              onClick={() =>
                run("active", async () => {
                  const updated = await api.updatePreferences({
                    isActive: !user?.isActive,
                  });
                  setUser(updated);
                  return updated.isActive
                    ? t.dashboard.notificationsOn
                    : t.dashboard.notificationsOff;
                })
              }
            >
              {user?.isActive ? t.dashboard.on : t.dashboard.off}
            </Button>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div>
              <p className="text-sm font-medium">{t.dashboard.testNotification}</p>
              <p className="text-xs text-muted-foreground">
                {t.dashboard.testNote}
              </p>
            </div>
            <Button
              variant="outline"
              size="sm"
              disabled={busy === "test"}
              onClick={() =>
                run("test", async () => {
                  const result = await api.sendTestNotification();
                  return result.reason;
                })
              }
            >
              {busy === "test" ? t.dashboard.sending : t.dashboard.sendTest}
            </Button>
          </div>

          {user?.notificationRepoName && (
            <p className="text-xs text-muted-foreground">
              {t.dashboard.notificationsTarget}{" "}
              <a
                href={`https://github.com/${user.gitHubUsername}/${user.notificationRepoName}/issues/${user.notificationIssueNumber}`}
                target="_blank"
                rel="noopener noreferrer"
                className="underline underline-offset-4"
              >
                {user.notificationRepoName}#{user.notificationIssueNumber}
              </a>{" "}
              {t.dashboard.notificationsTargetSuffix}
            </p>
          )}
        </CardContent>
      </Card>

      {/* --- Hesap silme --- */}
      <Card className="border-red-500/30">
        <CardHeader>
          <CardTitle className="text-red-400">{t.dashboard.deleteTitle}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            {t.dashboard.deleteNote}
          </p>
          <Button
            variant="destructive"
            disabled={busy === "delete"}
            onClick={() => {
              if (!confirm(t.dashboard.deleteConfirm)) return;

              run("delete", async () => {
                const result = await api.deleteAccount();
                await api.logout().catch(() => {});
                alert(result.message);
                router.push("/");
                return null;
              });
            }}
          >
            {t.dashboard.deleteButton}
          </Button>
        </CardContent>
      </Card>
    </main>
  );
}
