"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  api,
  ApiError,
  BADGE_THEMES,
  RANKS,
  type BadgeSnippets,
  type BadgeTheme,
  type BadgeVariant,
  type CurrentUser,
} from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CopyField } from "@/components/copy-field";
import { FlameIcon } from "@/components/icons";
import { useLanguage } from "@/components/language-provider";

/** Onizlemenin cerceve boyutlari; secilen varyantla birlikte degisir. */
const PREVIEW_SIZE: Record<BadgeVariant, { width: number; height: number }> = {
  full: { width: 400, height: 120 },
  compact: { width: 190, height: 52 },
  max: { width: 850, height: 200 },
};

export default function CustomizeBadgePage() {
  const router = useRouter();
  const { t, locale } = useLanguage();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [currentStreak, setCurrentStreak] = useState(0);
  const [snippets, setSnippets] = useState<BadgeSnippets | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // --- Gorunum secimleri ---
  const [theme, setTheme] = useState<BadgeTheme>("dark");
  const [variant, setVariant] = useState<BadgeVariant>("full");
  const [animated, setAnimated] = useState(true);
  const [flameFrom, setFlameFrom] = useState<string | null>(null);
  const [flameTo, setFlameTo] = useState<string | null>(null);
  const [background, setBackground] = useState<string | null>(null);
  const [border, setBorder] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([
      api.getCurrentUser(),
      api.getBadgeSettings(),
      // Rutbe galerisinde hangi alevlerin kazanildigini gostermek icin gerekli.
      api.getStreak().catch(() => null),
    ])
      .then(([current, settings, streak]) => {
        setUser(current);
        setCurrentStreak(streak?.currentStreak ?? 0);
        setTheme(settings.theme);
        setVariant(settings.variant);
        setAnimated(settings.animated);
        setFlameFrom(settings.flameFrom);
        setFlameTo(settings.flameTo);
        setBackground(settings.background);
        setBorder(settings.border);
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

  const handleSave = useCallback(async () => {
    setSaving(true);
    setError(null);
    setMessage(null);

    try {
      await api.updateBadgeSettings({
        theme,
        variant,
        animated,
        flameFrom,
        flameTo,
        background,
        border,
      });

      // Kod parcaciklari kaydedilen ayarin imzasini icerir; kaydettikten
      // sonra tazelenmeli ki kullanici guncel adresi kopyalayabilsin.
      const fresh = await api.getBadgeSnippets({ lang: locale });

      setSnippets(fresh);
      setMessage(t.customize.saved);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t.customize.saveError);
    } finally {
      setSaving(false);
    }
  }, [theme, variant, animated, flameFrom, flameTo, background, border, locale, t]);

  if (loading) {
    return (
      <main className="flex flex-1 items-center justify-center">
        <p className="text-sm text-muted-foreground">{t.common.loading}</p>
      </main>
    );
  }

  // Beklenmedik bir varyant degeri sayfayi dusurmemeli; normal boyuta duseriz.
  const size = PREVIEW_SIZE[variant] ?? PREVIEW_SIZE.full;

  const previewUrl = api.badgeUrl(user?.gitHubUsername ?? "", {
    lang: locale,
    theme,
    variant,
    animated,
    flameFrom,
    flameTo,
    background,
    border,
  });

  return (
    <main className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
      <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <FlameIcon className="h-7 w-7" />
          <div>
            <h1 className="text-2xl font-bold">{t.customize.title}</h1>
            <p className="text-sm text-muted-foreground">{t.customize.subtitle}</p>
          </div>
        </div>

        <Link
          href="/dashboard"
          className="text-sm text-muted-foreground underline-offset-4 hover:underline"
        >
          ← {t.customize.back}
        </Link>
      </div>

      {/* --- Onizleme --- */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>{t.customize.preview}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto rounded-lg border bg-muted/30 p-6">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={previewUrl}
              alt={t.customize.preview}
              width={size.width}
              height={size.height}
              className="max-w-full"
            />
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 md:grid-cols-2">
        {/* --- Boyut --- */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t.customize.sizeSection}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <SizeOption
              active={variant === "full"}
              onClick={() => setVariant("full")}
              label={t.customize.sizeFull}
              note={t.customize.sizeFullNote}
            />
            <SizeOption
              active={variant === "compact"}
              onClick={() => setVariant("compact")}
              label={t.customize.sizeCompact}
              note={t.customize.sizeCompactNote}
            />
            <SizeOption
              active={variant === "max"}
              onClick={() => setVariant("max")}
              label={t.customize.sizeMax}
              note={t.customize.sizeMaxNote}
            />
          </CardContent>
        </Card>

        {/* --- Tema --- */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">{t.customize.themeSection}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-2 gap-2">
              {BADGE_THEMES.map((option) => (
                <button
                  key={option.value}
                  type="button"
                  onClick={() => setTheme(option.value)}
                  className={`rounded-lg border px-3 py-2 text-sm transition ${
                    theme === option.value
                      ? "border-primary bg-primary/10 font-medium"
                      : "hover:bg-muted/50"
                  }`}
                >
                  {option.label}
                </button>
              ))}
            </div>
          </CardContent>
        </Card>

        {/* --- Renkler --- */}
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">{t.customize.colorSection}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-xs text-muted-foreground">{t.customize.colorNote}</p>

            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <ColorPicker
                label={t.customize.flameFrom}
                value={flameFrom}
                fallback="#ffa028"
                onChange={setFlameFrom}
                resetLabel={t.customize.reset}
              />
              <ColorPicker
                label={t.customize.flameTo}
                value={flameTo}
                fallback="#f0483e"
                onChange={setFlameTo}
                resetLabel={t.customize.reset}
              />
              <ColorPicker
                label={t.customize.background}
                value={background}
                fallback="#0d1117"
                onChange={setBackground}
                resetLabel={t.customize.reset}
              />
              <ColorPicker
                label={t.customize.border}
                value={border}
                fallback="#30363d"
                onChange={setBorder}
                resetLabel={t.customize.reset}
              />
            </div>
          </CardContent>
        </Card>

          {/* --- Rutbeler --- */}
        <Card className="md:col-span-2">
          <CardHeader>
            <CardTitle className="text-base">{t.customize.ranksTitle}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-xs text-muted-foreground">{t.customize.ranksNote}</p>

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
              {RANKS.map((rank) => {
                const unlocked = currentStreak >= rank.threshold;
                const isCurrent =
                  unlocked &&
                  !RANKS.some(
                    (r) => r.threshold > rank.threshold && currentStreak >= r.threshold,
                  );

                return (
                  <div
                    key={rank.key}
                    className={`rounded-lg border p-3 text-center transition ${
                      isCurrent ? "border-primary bg-primary/10" : "bg-muted/20"
                    }`}
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={api.flamePreviewUrl(rank.key, {
                        theme,
                        locked: !unlocked,
                        flameFrom,
                        flameTo,
                      })}
                      alt={locale === "en" ? rank.en : rank.tr}
                      width={72}
                      height={72}
                      className="mx-auto"
                    />

                    <p className="mt-1 text-xs font-semibold tracking-wide">
                      {locale === "en" ? rank.en : rank.tr}
                    </p>

                    <p className="mt-0.5 text-[11px] text-muted-foreground">
                      {unlocked ? (
                        isCurrent ? (
                          <span className="text-primary">{t.customize.rankCurrent}</span>
                        ) : (
                          t.customize.rankUnlocked
                        )
                      ) : (
                        <>
                          🔒 {rank.threshold - currentStreak} {t.customize.rankDaysLeft}
                        </>
                      )}
                    </p>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>

      {/* --- Animasyon --- */}
        <Card className="md:col-span-2">
          <CardContent className="flex flex-wrap items-center justify-between gap-4 pt-6">
            <div>
              <p className="text-sm font-medium">{t.customize.animation}</p>
              <p className="mt-1 text-xs text-muted-foreground">
                {t.customize.animationNote}
              </p>
            </div>

            <label className="flex cursor-pointer items-center gap-2">
              <input
                type="checkbox"
                checked={animated}
                onChange={(e) => setAnimated(e.target.checked)}
                className="h-4 w-4"
              />
              <span className="text-sm">{animated ? "✓" : "—"}</span>
            </label>
          </CardContent>
        </Card>
      </div>

      {/* --- Kaydet --- */}
      <div className="mt-8 flex flex-wrap items-center gap-4">
        <Button onClick={handleSave} disabled={saving} size="lg">
          {saving ? t.customize.saving : t.customize.save}
        </Button>

        {message && <p className="text-sm text-emerald-400">{message}</p>}
        {error && (
          <p role="alert" className="text-sm text-red-400">
            {error}
          </p>
        )}
      </div>

      {/* --- Kaydettikten sonra guncel README kodu --- */}
      {snippets && (
        <Card className="mt-8">
          <CardHeader>
            <CardTitle className="text-base">{t.customize.snippetTitle}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <p className="text-xs text-muted-foreground">{t.customize.snippetNote}</p>
            <CopyField label={t.dashboard.badgeMarkdown} value={snippets.markdown} />
            <CopyField label={t.dashboard.badgeHtml} value={snippets.html} />
          </CardContent>
        </Card>
      )}
    </main>
  );
}

function SizeOption({
  active,
  onClick,
  label,
  note,
}: {
  active: boolean;
  onClick: () => void;
  label: string;
  note: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`w-full rounded-lg border px-4 py-3 text-left transition ${
        active ? "border-primary bg-primary/10" : "hover:bg-muted/50"
      }`}
    >
      <span className="block text-sm font-medium">{label}</span>
      <span className="mt-0.5 block text-xs text-muted-foreground">{note}</span>
    </button>
  );
}

/**
 * Renk secici. Deger null oldugunda temanin rengi kullanilir; bu yuzden
 * "temaya don" secenegi ayri bir dugme olarak sunuluyor - renk secicide
 * "renk yok" diye bir durum gosterilemiyor.
 */
function ColorPicker({
  label,
  value,
  fallback,
  onChange,
  resetLabel,
}: {
  label: string;
  value: string | null;
  fallback: string;
  onChange: (value: string | null) => void;
  resetLabel: string;
}) {
  return (
    <div className="space-y-1.5">
      <label className="block text-xs text-muted-foreground">{label}</label>

      <div className="flex items-center gap-2">
        <input
          type="color"
          value={value ?? fallback}
          onChange={(e) => onChange(e.target.value)}
          className="h-9 w-14 cursor-pointer rounded border bg-background"
        />

        <span className="font-mono text-xs text-muted-foreground">
          {value ?? "—"}
        </span>
      </div>

      {value && (
        <button
          type="button"
          onClick={() => onChange(null)}
          className="text-xs text-muted-foreground underline-offset-4 hover:underline"
        >
          {resetLabel}
        </button>
      )}
    </div>
  );
}
