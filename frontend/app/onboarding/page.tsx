"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { api, ApiError, type CurrentUser } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CheckIcon, FlameIcon } from "@/components/icons";
import { HOUR_OPTIONS } from "@/lib/hours";

export default function OnboardingPage() {
  const router = useRouter();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [loading, setLoading] = useState(true);
  const [accepted, setAccepted] = useState(false);
  const [hour, setHour] = useState(20);
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
        setHour(current.preferredNotificationHourUtc);
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
      await api.completeOnboarding(true, hour);
      router.push("/dashboard");
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Kurulum tamamlanamadi.");
      setSubmitting(false);
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
    <main className="flex flex-1 items-center justify-center px-6 py-12">
      <Card className="w-full max-w-2xl">
        <CardHeader className="space-y-3">
          <div className="flex items-center gap-3">
            <FlameIcon className="h-8 w-8" />
            <CardTitle className="text-2xl">Son bir adim kaldi</CardTitle>
          </div>
          <p className="text-sm text-muted-foreground">
            Merhaba <strong>{user?.gitHubUsername}</strong>! Bildirimleri
            calistirabilmemiz icin hesabinda kucuk bir kurulum yapmamiz gerekiyor.
            Ne yapacagimizi asagida acikca anlatiyoruz.
          </p>
        </CardHeader>

        <CardContent className="space-y-6">
          <section className="space-y-3 rounded-lg border bg-muted/30 p-4">
            <h2 className="text-sm font-semibold">Onayinla neler yapacagiz?</h2>
            <ul className="space-y-3 text-sm text-muted-foreground">
              <ExplainItem title="Gizli bir repo olusturacagiz">
                Hesabinda <code className="text-foreground">.streak-tracker-notifications</code>{" "}
                adinda <strong>gizli (private)</strong> bir repo acilacak. Icerigini
                senden baskasi goremez.
              </ExplainItem>
              <ExplainItem title="Icine tek bir Issue acacagiz">
                Bildirimleri bu Issue&apos;ya yorum olarak dusurecegiz. GitHub Mobile
                bu yorumu telefonuna push bildirimi olarak iletir. Bildirim
                mekanizmamiz budur.
              </ExplainItem>
              <ExplainItem title="Katki gecmisini okuyacagiz">
                Streak&apos;ini hesaplayabilmek icin gunluk katki takvimini
                okuyoruz. <strong>Kodlarini okumuyoruz</strong>; yalnizca hangi gun
                kac katki yaptigin bilgisini kullaniyoruz.
              </ExplainItem>
            </ul>
          </section>

          <section className="space-y-2 rounded-lg border border-amber-500/30 bg-amber-500/5 p-4">
            <h2 className="text-sm font-semibold">
              Neden &quot;repo&quot; izni istiyoruz?
            </h2>
            <p className="text-sm text-muted-foreground">
              GitHub, gizli repo olusturabilmek ve private repo&apos;lardaki
              commit&apos;lerinin seriye sayilabilmesi icin bu izni zorunlu tutuyor.
              Daha dar bir izin secenegi sunmuyor. Izni istedigin an GitHub
              ayarlarindan geri alabilirsin.
            </p>
          </section>

          <section className="space-y-2">
            <label htmlFor="hour" className="text-sm font-medium">
              Bildirim saati (UTC)
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
            <p className="text-xs text-muted-foreground">
              O saatte hala commit atmamissan uyari gonderiyoruz. Bu ayari sonradan
              degistirebilirsin.
            </p>
          </section>

          <label className="flex cursor-pointer items-start gap-3 rounded-lg border p-4 hover:bg-muted/40">
            <input
              type="checkbox"
              checked={accepted}
              onChange={(e) => setAccepted(e.target.checked)}
              className="mt-0.5 h-4 w-4"
            />
            <span className="text-sm">
              Yukaridaki islemleri okudum ve onayliyorum. GitHub hesabimda gizli
              repo ve Issue olusturulmasina, katki takvimimin streak hesabi icin
              okunmasina izin veriyorum.
            </span>
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
              {submitting ? "Kurulum yapiliyor…" : "Onayla ve kurulumu tamamla"}
            </Button>

            <Button
              variant="ghost"
              size="lg"
              onClick={async () => {
                await api.logout().catch(() => {});
                router.push("/");
              }}
            >
              Vazgec
            </Button>
          </div>

          <p className="text-center text-xs text-muted-foreground">
            Onay vermezsen hesabinda hicbir sey olusturulmaz.
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
