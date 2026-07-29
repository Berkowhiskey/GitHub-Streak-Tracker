import type { Metadata } from "next";
import { Inter, JetBrains_Mono } from "next/font/google";
import "./globals.css";

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

export const metadata: Metadata = {
  title: "StreakTracker — GitHub Serini Kaybetme",
  description:
    "GitHub commit serini takip et, seri bozulmadan once telefonuna bildirim al ve profiline dinamik rozet ekle.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    // Dark mode varsayilan tema; proje GitHub'in koyu arayuzuyle uyumlu tasarlandi.
    <html
      lang="tr"
      className={`dark ${inter.variable} ${jetbrainsMono.variable} h-full antialiased`}
    >
      <body className="min-h-full flex flex-col bg-background text-foreground">
        {children}
      </body>
    </html>
  );
}
