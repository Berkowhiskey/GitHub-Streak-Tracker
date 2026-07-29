"use client";

import { useState } from "react";
import { api, ApiError, type AppInstallationStatus } from "@/lib/api";
import { Button } from "@/components/ui/button";

/**
 * GitHub App kurulu degilse gosterilen uyari.
 *
 * Neden gerekli: GitHub, kullanicinin kendi eylemleri icin ona bildirim gondermez.
 * Bu yuzden bildirim yorumunu ayri bir kimlik (App / bot) atmak zorunda; App kurulu
 * degilse bildirimler Issue'ya dusse bile telefona push gitmez.
 */
export function AppInstallNotice({
  status,
  onInstalled,
}: {
  status: AppInstallationStatus;
  onInstalled: () => void;
}) {
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (status.installed) return null;

  if (!status.appConfigured) {
    return (
      <div className="mb-6 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-3 text-sm">
        <p className="font-medium">Bildirimler henuz etkin degil</p>
        <p className="mt-1 text-muted-foreground">
          Sunucuda GitHub App yapilandirilmamis. Bildirimlerin telefona
          dusebilmesi icin yoneticinin App kurulumunu tamamlamasi gerekiyor.
        </p>
      </div>
    );
  }

  async function handleCheck() {
    setChecking(true);
    setError(null);

    try {
      const result = await api.getAppStatus();

      if (result.installed) {
        onInstalled();
      } else {
        setError(
          "Kurulum henuz gorunmuyor. GitHub'da kurulumu tamamladigindan ve bildirim reposuna erisim verdiginden emin ol.",
        );
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Kontrol edilemedi.");
    } finally {
      setChecking(false);
    }
  }

  return (
    <div className="mb-6 space-y-3 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-4">
      <div>
        <p className="font-medium">Son adim: bildirim uygulamasini kur</p>
        <p className="mt-1 text-sm text-muted-foreground">
          GitHub, kendi yaptigin islemler icin sana bildirim gondermez. Bu yuzden
          uyarilari senin adina degil, ayri bir <strong>bot kimligiyle</strong>{" "}
          gonderiyoruz. Bot&apos;un yorum atabilmesi icin GitHub App&apos;i
          kurmalisin — kurulum sirasinda{" "}
          <strong>yalnizca bildirim reposunu</strong> secmen yeterli.
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        <a
          href={status.installationUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex h-9 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/80"
        >
          GitHub App&apos;i kur
        </a>

        <Button variant="outline" size="sm" onClick={handleCheck} disabled={checking}>
          {checking ? "Kontrol ediliyor…" : "Kurdum, kontrol et"}
        </Button>
      </div>

      {error && <p className="text-sm text-amber-300">{error}</p>}
    </div>
  );
}
