import type { Metadata } from "next";
import { cookies } from "next/headers";
import { Inter, JetBrains_Mono } from "next/font/google";
import "./globals.css";
import { LanguageProvider } from "@/components/language-provider";
import { LOCALE_COOKIE, getDictionary, resolveInitialLocale } from "@/lib/i18n";

// Inter: arayüz fontlarının fiili standardı. Rakamları belirgin oldugu icin
// streak sayilari gibi sayisal degerlerde okunakliligi yuksek.
const inter = Inter({
  variable: "--font-sans",
  subsets: ["latin", "latin-ext"], // latin-ext: Turkce karakterler (ç, ğ, ı, ö, ş, ü)
  display: "swap",
});

// Rozet kodu gibi kopyalanabilir metin alanlari icin.
const jetbrainsMono = JetBrains_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin", "latin-ext"],
  display: "swap",
});

// Sabit degil, fonksiyon: sekme basligi da kullanicinin diline gore uretilsin.
// Sabit `metadata` nesnesi cerezi okuyamaz ve her zaman Turkce kalirdi.
export async function generateMetadata(): Promise<Metadata> {
  const cookieStore = await cookies();
  const t = getDictionary(resolveInitialLocale(cookieStore.get(LOCALE_COOKIE)?.value));

  return {
    title: t.meta.siteTitle,
    description: t.meta.siteDescription,
  };
}

// Next.js 16'da cookies() asenkrondur.
export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  // Dil tercihi cerezde tutuldugu icin sunucu tarafinda da okunabiliyor;
  // boylece sayfa dogru dille render edilir ve yanip sonme (flash) olmaz.
  const cookieStore = await cookies();
  const locale = resolveInitialLocale(cookieStore.get(LOCALE_COOKIE)?.value);

  return (
    // Dark mode varsayilan tema; proje GitHub'in koyu arayuzuyle uyumlu tasarlandi.
    <html
      lang={locale}
      className={`dark ${inter.variable} ${jetbrainsMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-background text-foreground">
        <LanguageProvider initialLocale={locale}>{children}</LanguageProvider>
      </body>
    </html>
  );
}
