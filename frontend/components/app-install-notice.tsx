"use client";

import { useState } from "react";
import { api, ApiError, type AppInstallationStatus } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { useLanguage } from "@/components/language-provider";

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
  const { t } = useLanguage();
  const [checking, setChecking] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (status.installed) return null;

  if (!status.appConfigured) {
    return (
      <div className="mb-6 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-3 text-sm">
        <p className="font-medium">{t.appInstall.notConfiguredTitle}</p>
        <p className="mt-1 text-muted-foreground">
          {t.appInstall.notConfiguredBody}
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
          t.appInstall.notFound,
        );
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : t.appInstall.checkError);
    } finally {
      setChecking(false);
    }
  }

  return (
    <div className="mb-6 space-y-3 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-4">
      <div>
        <p className="font-medium">{t.appInstall.title}</p>
        <p className="mt-1 text-sm text-muted-foreground">
          {t.appInstall.body}
        </p>
      </div>

      <div className="flex flex-wrap gap-2">
        <a
          href={status.installationUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex h-9 items-center rounded-lg bg-primary px-4 text-sm font-medium text-primary-foreground hover:bg-primary/80"
        >
          {t.appInstall.installButton}
        </a>

        <Button variant="outline" size="sm" onClick={handleCheck} disabled={checking}>
          {checking ? t.appInstall.checking : t.appInstall.checkButton}
        </Button>
      </div>

      {error && <p className="text-sm text-amber-300">{error}</p>}
    </div>
  );
}
