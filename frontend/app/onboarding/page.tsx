"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, ApiError, type CurrentUser } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CheckIcon, FlameIcon } from "@/components/icons";
import { HOUR_OPTIONS, detectTimeZone, formatUtcOffset } from "@/lib/hours";
import { useLanguage } from "@/components/language-provider";
import { LanguageSwitcher } from "@/components/language-switcher";

export default function OnboardingPage() {
  const router = useRouter();
  const { t } = useLanguage();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [accepted, setAccepted] = useState(false);
  const [hour, setHour] = useState(20);
  // Saat dilimi tarayicidan otomatik alginir; kullanici panelden degistirebilir.
  const [timeZone, setTimeZone] = useState("UTC");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .getCurrentUser()
      .then((current) => {
        // Kurulumu zaten tamamlamis kullaniciyi panele gonder.
        if (current.hasAcceptedTerms && current.notificationIssueNumber !== null) {
          router.replace("/dashboard");
          return;
        }

        setUser(current);
        setHour(current.preferredNotificationHour);
        setTimeZone(detectTimeZone());
        setLoading(false);
      })
      .catch((err: ApiError) => {
        if (err.isUnauthorized) {
          router.replace("/");
          return;
        }

        setError(err.message);
        setLoading(false);
      });
  }, [router]);

  async function handleSubmit() {
    setSubmitting(true);
    setError(null);

    try {
      await api.completeOnboarding(true, hour, timeZone);
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t.onboarding.error);
      setSubmitting(false);
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
    <main className="flex flex-1 items-center justify-center px-6 py-12">
      <Card className="w-full max-w-2xl">
        <CardHeader className="space-y-3">
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <FlameIcon className="h-8 w-8" />
              <CardTitle className="text-2xl">{t.onboarding.title}</CardTitle>
            </div>
            <LanguageSwitcher />
          </div>
          <p className="text-sm text-muted-foreground">
            {t.onboarding.greeting} <strong>{user?.gitHubUsername}</strong>!{" "}
            {t.onboarding.intro}
          </p>
        </CardHeader>

        <CardContent className="space-y-6">
          <section className="space-y-3 rounded-lg border bg-muted/30 p-4">
            <h2 className="text-sm font-semibold">{t.onboarding.whatWeDo}</h2>
            <ul className="space-y-3 text-sm text-muted-foreground">
              <ExplainItem title={t.onboarding.steps.repo.title}>
                <code className="text-foreground">.streak-tracker-notifications</code>{" "}
                {t.onboarding.steps.repo.body}
              </ExplainItem>
              <ExplainItem title={t.onboarding.steps.issue.title}>
                {t.onboarding.steps.issue.body}
              </ExplainItem>
              <ExplainItem title={t.onboarding.steps.contributions.title}>
                {t.onboarding.steps.contributions.body}
              </ExplainItem>
            </ul>
          </section>

          <section className="space-y-2 rounded-lg border border-amber-500/30 bg-amber-500/5 p-4">
            <h2 className="text-sm font-semibold">{t.onboarding.repoScope.title}</h2>
            <p className="text-sm text-muted-foreground">{t.onboarding.repoScope.body}</p>
          </section>

          <section className="space-y-2">
            <label htmlFor="hour" className="text-sm font-medium">
              {t.onboarding.notificationHour}
            </label>
            <select
              id="hour"
              value={hour}
              onChange={(e) => setHour(Number(e.target.value))}
              className="w-full rounded-md border bg-background px-3 py-2 text-sm"
            >
              {HOUR_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <p className="text-xs text-muted-foreground">{t.onboarding.hourNote}</p>

            <p className="rounded-md bg-muted/40 px-3 py-2 text-xs text-muted-foreground">
              {t.onboarding.timeZoneDetected}{" "}
              <strong className="text-foreground">
                {timeZone} {formatUtcOffset(timeZone)}
              </strong>
              {t.onboarding.timeZoneNote}
            </p>
          </section>

          <label className="flex cursor-pointer items-start gap-3 rounded-lg border p-4 hover:bg-muted/40">
            <input
              type="checkbox"
              checked={accepted}
              onChange={(e) => setAccepted(e.target.checked)}
              className="mt-0.5 h-4 w-4"
            />
            <span className="text-sm">{t.onboarding.consent}</span>
          </label>

          {error && (
            <p role="alert" className="text-sm text-red-400">
              {error}
            </p>
          )}

          <div className="flex flex-col gap-3 sm:flex-row-reverse">
            <Button
              onClick={handleSubmit}
              disabled={!accepted || submitting}
              size="lg"
              className="flex-1"
            >
              {submitting ? t.onboarding.submitting : t.onboarding.submit}
            </Button>

            <Button
              variant="ghost"
              size="lg"
              onClick={async () => {
                await api.logout().catch(() => {});
                router.push("/");
              }}
            >
              {t.common.cancel}
            </Button>
          </div>

          <p className="text-center text-xs text-muted-foreground">
            {t.onboarding.noConsentNote}
          </p>
        </CardContent>
      </Card>
    </main>
  );
}

function ExplainItem({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) {
  return (
    <li className="flex gap-3">
      <CheckIcon className="mt-0.5 h-4 w-4 shrink-0 text-emerald-500" />
      <span>
        <strong className="text-foreground">{title}.</strong> {children}
      </span>
    </li>
  );
}
