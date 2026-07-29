import Link from "next/link";

export const metadata = {
  title: "Izinler ve Gizlilik — StreakTracker",
};

export default function PrivacyPage() {
  return (
    <main className="mx-auto w-full max-w-2xl flex-1 px-6 py-12">
      <Link
        href="/"
        className="text-sm text-muted-foreground underline underline-offset-4"
      >
        ← Ana sayfa
      </Link>

      <h1 className="mt-6 text-3xl font-bold">Izinler ve Gizlilik</h1>

      <div className="mt-8 space-y-8 text-sm leading-relaxed text-muted-foreground">
        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            Hangi izinleri istiyoruz?
          </h2>
          <ul className="list-inside list-disc space-y-1">
            <li>
              <strong className="text-foreground">repo</strong> — Gizli bildirim
              reposunu olusturmak, Issue&apos;ya yorum atmak ve private
              repo&apos;lardaki commit&apos;lerin seriye sayilmasi icin. GitHub
              bunun icin daha dar bir izin sunmuyor.
            </li>
            <li>
              <strong className="text-foreground">read:user</strong> — Profil
              bilgin ve katki takvimin icin.
            </li>
            <li>
              <strong className="text-foreground">user:email</strong> — Yedek
              bildirim kanali icin e-posta adresin.
            </li>
          </ul>
        </section>

        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            Neleri saklıyoruz?
          </h2>
          <p>
            GitHub kullanici adin, sayisal kimligin, e-postan, avatar adresin;
            streak verilerin (guncel seri, rekor, son commit gunu) ve gonderilen
            bildirimlerin kaydi. GitHub access token&apos;in veritabaninda{" "}
            <strong className="text-foreground">sifrelenmis</strong> olarak tutulur.
          </p>
        </section>

        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            Neleri saklamiyoruz?
          </h2>
          <p>
            Kodlarini okumuyor, indirmiyor veya saklamiyoruz. Repo iceriklerine
            erisimimiz yalnizca bildirim reposunu olusturmak ve o repodaki
            Issue&apos;ya yorum atmakla sinirli kullanilir.
          </p>
        </section>

        <section className="space-y-2">
          <h2 className="text-base font-semibold text-foreground">
            Verilerini silmek istersen
          </h2>
          <p>
            Panelindeki <strong className="text-foreground">Hesabi sil</strong>{" "}
            butonu ile tum verilerin kalici olarak silinir. Ayrica GitHub
            ayarlarindan uygulamanin erisimini istedigin an geri alabilirsin.
          </p>
        </section>
      </div>
    </main>
  );
}
