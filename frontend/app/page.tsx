import Link from "next/link";
import { API_BASE_URL } from "@/lib/api";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { GitHubIcon, FlameIcon } from "@/components/icons";

/** Callback hatalarinin kullaniciya gosterilecek karsiliklari. */
const ERROR_MESSAGES: Record<string, string> = {
  access_denied:
    "GitHub izni verilmedi. Devam etmek icin yetkilendirmeyi onaylaman gerekiyor.",
  missing_code: "GitHub'dan beklenen yanit alinamadi. Lutfen tekrar dene.",
  invalid_state:
    "Guvenlik dogrulamasi basarisiz oldu. Lutfen girisi bastan baslat.",
};

// Next.js 16'da searchParams bir Promise'tir ve await edilmelidir.
export default async function HomePage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const { error } = await searchParams;
  const errorMessage = error ? (ERROR_MESSAGES[error] ?? null) : null;

  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16">
      <div className="w-full max-w-3xl space-y-12">
        {errorMessage && (
          <div
            role="alert"
            className="rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm"
          >
            {errorMessage}
          </div>
        )}

        <div className="space-y-6 text-center">
          <div className="flex justify-center">
            <FlameIcon className="h-16 w-16" />
          </div>

          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl">
            Serini kaybetme.
          </h1>

          <p className="mx-auto max-w-xl text-lg text-muted-foreground">
            GitHub commit serini takip ediyoruz. Gun bitmeden commit atmadiysan
            telefonuna bildirim gonderiyoruz — hem de GitHub&apos;in kendi mobil
            uygulamasi uzerinden.
          </p>

          <div className="flex flex-col items-center gap-3 pt-2">
            {/* OAuth akisi tam sayfa gecisi gerektirir; bu yuzden next/link degil <a>. */}
            <a
              href={`${API_BASE_URL}/api/v1/auth/github/login`}
              className={cn(buttonVariants({ size: "lg" }), "h-11 gap-2 px-6 text-base")}
            >
              <GitHubIcon className="h-5 w-5" />
              GitHub ile Giris Yap
            </a>

            <p className="text-xs text-muted-foreground">
              Giris yaptiginda hesabinda hicbir sey olusturulmaz. Kurulum,
              bilgilendirme metnini okuyup onayladiktan sonra baslar.
            </p>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <FeatureCard
            title="Telefonuna bildirim"
            description="Serin tehlikedeyse GitHub Mobile uzerinden aninda push bildirimi alirsin. Ekstra uygulama kurmana gerek yok."
          />
          <FeatureCard
            title="Dinamik rozet"
            description="Profil README'ne ekleyebilecegin, her zaman guncel ve hizli calisan bir streak rozeti uretiriz."
          />
          <FeatureCard
            title="Sen kontrol edersin"
            description="Bildirim saatini secersin, istedigin an durdurursun, hesabini tek tikla silersin."
          />
        </div>

        <footer className="border-t pt-6 text-center text-xs text-muted-foreground">
          <Link href="/gizlilik" className="underline underline-offset-4">
            Hangi izinleri neden istiyoruz?
          </Link>
        </footer>
      </div>
    </main>
  );
}

function FeatureCard({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <div className="rounded-xl border bg-card p-5">
      <h2 className="font-semibold">{title}</h2>
      <p className="mt-2 text-sm text-muted-foreground">{description}</p>
    </div>
  );
}
