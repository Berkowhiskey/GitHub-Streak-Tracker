import Link from "next/link";
import { cookies } from "next/headers";
import { LOCALE_COOKIE, getDictionary, resolveInitialLocale } from "@/lib/i18n";

export async function generateMetadata() {
  const cookieStore = await cookies();
  const t = getDictionary(resolveInitialLocale(cookieStore.get(LOCALE_COOKIE)?.value));

  return { title: t.meta.privacyTitle };
}

export default async function PrivacyPage() {
  const cookieStore = await cookies();
  const locale = resolveInitialLocale(cookieStore.get(LOCALE_COOKIE)?.value);
  const t = getDictionary(locale);

  return (
    <main className="mx-auto w-full max-w-2xl flex-1 px-6 py-12">
      <Link
        href="/"
        className="text-sm text-muted-foreground underline underline-offset-4"
      >
        ← {t.common.back}
      </Link>

      <h1 className="mt-6 text-3xl font-bold">{t.privacy.title}</h1>

      <div className="mt-8 space-y-8 text-sm leading-relaxed text-muted-foreground">
        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            {t.privacy.permissions}
          </h2>
          <ul className="list-inside list-disc space-y-1">
            <li>
              <strong className="text-foreground">repo</strong> — {t.privacy.repoScope}
            </li>
            <li>
              <strong className="text-foreground">read:user</strong> — {t.privacy.readUser}
            </li>
            <li>
              <strong className="text-foreground">user:email</strong> — {t.privacy.userEmail}
            </li>
          </ul>
        </section>

        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            {t.privacy.whatWeStore}
          </h2>
          <p>{t.privacy.whatWeStoreBody}</p>
        </section>

        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            {t.privacy.whatWeDont}
          </h2>
          <p>{t.privacy.whatWeDontBody}</p>
        </section>

        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            {t.privacy.deletion}
          </h2>
          <p>{t.privacy.deletionBody}</p>
        </section>
      </div>
    </main>
  );
}
