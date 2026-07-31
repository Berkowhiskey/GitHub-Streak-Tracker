"use client";

import { useLanguage } from "@/components/language-provider";
import { LOCALES, type Locale } from "@/lib/i18n";

/**
 * Dil secici. Secim cerezde saklanir ve giris yapilmissa sunucudaki
 * tercihe de yazilir (bildirimler ve rozet o dile gore uretilir).
 */
export function LanguageSwitcher({ className }: { className?: string }) {
  const { locale, setLocale } = useLanguage();

  return (
    <div
      className={`inline-flex overflow-hidden rounded-lg border text-xs ${className ?? ""}`}
      role="group"
      aria-label="Language"
    >
      {LOCALES.map((option) => {
        const active = option.value === locale;

        return (
          <button
            key={option.value}
            type="button"
            onClick={() => setLocale(option.value as Locale)}
            aria-pressed={active}
            className={`px-2.5 py-1.5 transition-colors ${
              active
                ? "bg-primary text-primary-foreground"
                : "text-muted-foreground hover:bg-muted hover:text-foreground"
            }`}
          >
            {option.value.toUpperCase()}
          </button>
        );
      })}
    </div>
  );
}
