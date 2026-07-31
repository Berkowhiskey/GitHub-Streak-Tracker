"use client";

import { createContext, useCallback, useContext, useEffect, useState } from "react";
import {
  DEFAULT_LOCALE,
  LOCALE_COOKIE,
  getDictionary,
  localeFromNavigator,
  type Dictionary,
  type Locale,
} from "@/lib/i18n";
import { api } from "@/lib/api";

interface LanguageContextValue {
  locale: Locale;
  t: Dictionary;
  setLocale: (locale: Locale) => void;
}

const LanguageContext = createContext<LanguageContextValue | null>(null);

/** Dil tercihini bir yil boyunca saklar; sunucu bilesenleri de bu cerezi okur. */
function writeLocaleCookie(locale: Locale) {
  document.cookie = `${LOCALE_COOKIE}=${locale}; path=/; max-age=31536000; samesite=lax`;
}

export function LanguageProvider({
  initialLocale,
  children,
}: {
  initialLocale: Locale;
  children: React.ReactNode;
}) {
  const [locale, setLocaleState] = useState<Locale>(initialLocale);

  useEffect(() => {
    // Cerez yoksa (ilk ziyaret) tarayici diline gore sec ve kaydet.
    const hasCookie = document.cookie.includes(`${LOCALE_COOKIE}=`);

    if (!hasCookie) {
      const detected = localeFromNavigator();
      setLocaleState(detected);
      writeLocaleCookie(detected);
    }
  }, []);

  const setLocale = useCallback((next: Locale) => {
    setLocaleState(next);
    writeLocaleCookie(next);

    // Giris yapilmissa sunucudaki tercihi de guncelle: bildirimler ve rozet
    // bu degere gore uretiliyor. Oturum yoksa sessizce gecilir.
    api.updatePreferences({ language: next }).catch(() => {});

    document.documentElement.lang = next;
  }, []);

  return (
    <LanguageContext.Provider value={{ locale, t: getDictionary(locale), setLocale }}>
      {children}
    </LanguageContext.Provider>
  );
}

/**
 * Ceviri sozlugune ve dil degistiriciye erisim.
 * Saglayici disinda cagrilirsa varsayilan dile duser (bilesen izole test edilebilsin).
 */
export function useLanguage(): LanguageContextValue {
  const context = useContext(LanguageContext);

  if (context) return context;

  return {
    locale: DEFAULT_LOCALE,
    t: getDictionary(DEFAULT_LOCALE),
    setLocale: () => {},
  };
}
