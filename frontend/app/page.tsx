import Link from "next/link";
import { cookies } from "next/headers";
import { API_BASE_URL } from "@/lib/api";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { GitHubIcon, FlameIcon } from "@/components/icons";
import { LanguageSwitcher } from "@/components/language-switcher";
import { LOCALE_COOKIE, getDictionary, resolveInitialLocale } from "@/lib/i18n";

// Next.js 16'da searchParams ve cookies asenkrondur.
export default async function HomePage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>;
}) {
  const [{ error }, cookieStore] = await Promise.all([searchParams, cookies()]);

  const locale = resolveInitialLocale(cookieStore.get(LOCALE_COOKIE)?.value);
  const t = getDictionary(locale);

  const errorMessage = error
    ? (t.landing.errors[error as keyof typeof t.landing.errors] ?? null)
    : null;

  return (
    <main className="flex flex-1 flex-col items-center justify-center px-6 py-16">
      <div className="w-full max-w-3xl space-y-12">
        <div className="flex justify-end">
          <LanguageSwitcher />
        </div>

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
            {t.landing.title}
          </h1>

          <p className="mx-auto max-w-xl text-lg text-muted-foreground">
            {t.landing.subtitle}
          </p>

          <div className="flex flex-col items-center gap-3 pt-2">
            {/* OAuth akisi tam sayfa gecisi gerektirir; bu yuzden next/link degil <a>. */}
            <a
              href={`${API_BASE_URL}/api/v1/auth/github/login`}
              className={cn(buttonVariants({ size: "lg" }), "h-11 gap-2 px-6 text-base")}
            >
              <GitHubIcon className="h-5 w-5" />
              {t.landing.loginButton}
            </a>

            <p className="text-xs text-muted-foreground">{t.landing.loginNote}</p>
          </div>
        </div>

        <div className="grid gap-4 sm:grid-cols-3">
          <FeatureCard
            title={t.landing.features.notification.title}
            description={t.landing.features.notification.description}
          />
          <FeatureCard
            title={t.landing.features.badge.title}
            description={t.landing.features.badge.description}
          />
          <FeatureCard
            title={t.landing.features.control.title}
            description={t.landing.features.control.description}
          />
        </div>

        <footer className="border-t pt-6 text-center text-xs text-muted-foreground">
          <Link href="/gizlilik" className="underline underline-offset-4">
            {t.landing.privacyLink}
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
