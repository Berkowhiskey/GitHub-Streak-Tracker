"use client";

import { useCallback, useEffect, useState } from "react";
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
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { FlameIcon } from "@/components/icons";
import { ContributionHeatmap } from "@/components/contribution-heatmap";
import { CopyField } from "@/components/copy-field";
import { HOUR_OPTIONS } from "@/lib/hours";

export default function DashboardPage() {
  const router = useRouter();

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
        api.getBadgeSnippets(),
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

      setError(err instanceof ApiError ? err.message : "Veriler yuklenemedi.");
      setLoading(false);
    }
  }, [router]);

  useEffect(() => {
    loadAll();
  }, [loadAll]);

  async function run(key: string, action: () => Promise<string | null>) {
    setBusy(key);
    setMessage(null);
    setError(null);

    try {
      const result = await action();
      if (result) setMessage(result);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Islem tamamlanamadi.");
    } finally {
      setBusy(null);
    }
  }

  if (loading) {
    return (
      <main className="flex flex-1 items-center justify-center">
        <p className="text-sm text-muted-foreground">Yukleniyor…</p>
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
            <p className="text-xs text-muted-foreground">StreakTracker paneli</p>
          </div>
        </div>

        <Button
          variant="ghost"
          onClick={async () => {
            await api.logout().catch(() => {});
            router.push("/");
          }}
        >
          Cikis yap
        </Button>
      </header>

      {appStatus && (
        <AppInstallNotice
          status={appStatus}
          onInstalled={() => {
            setAppStatus({ ...appStatus, installed: true });
            setMessage("GitHub App kurulumu dogrulandi. Bildirimler artik calisiyor.");
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
              Guncel seri
            </CardTitle>
          </CardHeader>
          <CardContent className="flex items-center gap-3">
            <FlameIcon
              className="h-9 w-9"
              muted={(streak?.currentStreak ?? 0) === 0}
            />
            <div>
              <p className="text-3xl font-bold">{streak?.currentStreak ?? 0}</p>
              <p className="text-xs text-muted-foreground">gun</p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Rekor
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-3xl font-bold">{streak?.longestStreak ?? 0}</p>
            <p className="text-xs text-muted-foreground">gun</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm font-medium text-muted-foreground">
              Bugun
            </CardTitle>
          </CardHeader>
          <CardContent>
            <p
              className={`text-lg font-semibold ${
                streak?.hasCommittedToday ? "text-emerald-500" : "text-amber-500"
              }`}
            >
              {streak?.hasCommittedToday ? "Commit atildi" : "Henuz commit yok"}
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              {streak?.hasCommittedToday
                ? "Serin bugunluk guvende."
                : "Gun bitmeden bir commit at, serini koru."}
            </p>
          </CardContent>
        </Card>
      </section>

      {/* --- Heatmap --- */}
      <Card className="mb-8">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle>Katki takvimi</CardTitle>
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
                return "Veriler GitHub'dan tazelendi.";
              })
            }
          >
            {busy === "refresh" ? "Yenileniyor…" : "Yenile"}
          </Button>
        </CardHeader>
        <CardContent>
          <ContributionHeatmap days={calendar} />
        </CardContent>
      </Card>

      {/* --- Rozet --- */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>Profil rozetin</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="overflow-x-auto rounded-lg border bg-muted/30 p-4">
            {/* Rozet backend tarafindan SVG olarak uretilir. */}
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={`${API_BASE_URL}/api/v1/badges/${user?.gitHubUsername}.svg`}
              alt="Streak rozetin"
              width={400}
              height={120}
              className="max-w-full"
            />
          </div>

          {badges && (
            <div className="space-y-3">
              <CopyField label="Markdown (README icin)" value={badges.markdown} />
              <CopyField label="HTML" value={badges.html} />
            </div>
          )}

          <p className="text-xs text-muted-foreground">
            Not: Rozet adresi su an <code>localhost</code> uzerinde. Profil
            README&apos;nde gorunebilmesi icin servisin canli bir adrese
            yayinlanmasi gerekiyor.
          </p>
        </CardContent>
      </Card>

      {/* --- Bildirim ayarlari --- */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>Bildirim ayarlari</CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="space-y-2">
            <label htmlFor="hour" className="text-sm font-medium">
              Bildirim saati
            </label>
            <select
              id="hour"
              value={user?.preferredNotificationHourUtc ?? 20}
              disabled={busy === "hour"}
              onChange={(e) => {
                const nextHour = Number(e.target.value);
                run("hour", async () => {
                  const updated = await api.updatePreferences({
                    preferredNotificationHourUtc: nextHour,
                  });
                  setUser(updated);
                  return "Bildirim saati guncellendi.";
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

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div>
              <p className="text-sm font-medium">Bildirimler</p>
              <p className="text-xs text-muted-foreground">
                Kapatirsan sana hic bildirim gonderilmez.
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
                    ? "Bildirimler acildi."
                    : "Bildirimler kapatildi.";
                })
              }
            >
              {user?.isActive ? "Acik" : "Kapali"}
            </Button>
          </div>

          <div className="flex items-center justify-between rounded-lg border p-4">
            <div>
              <p className="text-sm font-medium">Test bildirimi</p>
              <p className="text-xs text-muted-foreground">
                Telefonuna push bildirimi dusuyor mu, hemen dene.
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
              {busy === "test" ? "Gonderiliyor…" : "Test gonder"}
            </Button>
          </div>

          {user?.notificationRepoName && (
            <p className="text-xs text-muted-foreground">
              Bildirimler{" "}
              <a
                href={`https://github.com/${user.gitHubUsername}/${user.notificationRepoName}/issues/${user.notificationIssueNumber}`}
                target="_blank"
                rel="noopener noreferrer"
                className="underline underline-offset-4"
              >
                {user.notificationRepoName}#{user.notificationIssueNumber}
              </a>{" "}
              adresine yorum olarak dusuruluyor.
            </p>
          )}
        </CardContent>
      </Card>

      {/* --- Hesap silme --- */}
      <Card className="border-red-500/30">
        <CardHeader>
          <CardTitle className="text-red-400">Hesabi sil</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            StreakTracker&apos;daki tum verilerin (profil, streak gecmisi, bildirim
            kayitlari) kalici olarak silinir. GitHub hesabindaki gizli repo
            silinmez; ona sen karar verirsin.
          </p>
          <Button
            variant="destructive"
            disabled={busy === "delete"}
            onClick={() => {
              if (!confirm("Hesabin ve tum verilerin silinecek. Emin misin?")) return;

              run("delete", async () => {
                const result = await api.deleteAccount();
                await api.logout().catch(() => {});
                alert(result.message);
                router.push("/");
                return null;
              });
            }}
          >
            Hesabimi ve verilerimi sil
          </Button>
        </CardContent>
      </Card>
    </main>
  );
}
