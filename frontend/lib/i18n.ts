/**
 * Dil sozlukleri.
 *
 * Kutuphane kullanilmiyor: 4 sayfalik bir uygulama icin route yapisini
 * degistirmek (next-intl'in [locale] segmenti) gereksiz karmasiklik olurdu.
 * Tercih cerezde saklandigi icin hem sunucu hem istemci bilesenleri okuyabiliyor.
 */

export type Locale = "tr" | "en";

export const LOCALE_COOKIE = "streaktracker_lang";
export const DEFAULT_LOCALE: Locale = "tr";

export const LOCALES: { value: Locale; label: string }[] = [
  { value: "tr", label: "Türkçe" },
  { value: "en", label: "English" },
];

export function isLocale(value: string | undefined | null): value is Locale {
  return value === "tr" || value === "en";
}

/**
 * Cerezden okunan ham degeri gecerli bir dile cevirir.
 *
 * Bilerek burada duruyor: sunucu bilesenleri (layout, landing, gizlilik) bunu
 * cagiriyor. "use client" isaretli bir dosyada tanimlansaydi Next.js istemci
 * bundle'inin parcasi sayar ve sunucudan cagrilmasina izin vermezdi.
 */
export function resolveInitialLocale(cookieValue: string | undefined): Locale {
  return isLocale(cookieValue) ? cookieValue : DEFAULT_LOCALE;
}

/** Tarayici dilinden desteklenen bir dile eslestirir. */
export function localeFromNavigator(): Locale {
  if (typeof navigator === "undefined") return DEFAULT_LOCALE;
  return navigator.language.toLowerCase().startsWith("en") ? "en" : "tr";
}

const tr = {
  // Tarayici sekmesi ve link onizlemelerinde gorunen metinler.
  meta: {
    siteTitle: "StreakTracker — GitHub Serini Kaybetme",
    siteDescription:
      "GitHub commit serini takip et, seri bozulmadan once telefonuna bildirim al ve profiline dinamik rozet ekle.",
    privacyTitle: "Izinler ve Gizlilik — StreakTracker",
  },

  common: {
    loading: "Yukleniyor…",
    save: "Kaydet",
    cancel: "Vazgec",
    close: "Kapat",
    back: "Ana sayfa",
    logout: "Cikis yap",
    language: "Dil",
  },

  landing: {
    title: "Serini kaybetme.",
    subtitle:
      "GitHub commit serini takip ediyoruz. Gun bitmeden commit atmadiysan telefonuna bildirim gonderiyoruz — hem de GitHub'in kendi mobil uygulamasi uzerinden.",
    loginButton: "GitHub ile Giris Yap",
    loginNote:
      "Giris yaptiginda hesabinda hicbir sey olusturulmaz. Kurulum, bilgilendirme metnini okuyup onayladiktan sonra baslar.",
    features: {
      notification: {
        title: "Telefonuna bildirim",
        description:
          "Serin tehlikedeyse GitHub Mobile uzerinden aninda push bildirimi alirsin. Ekstra uygulama kurmana gerek yok.",
      },
      badge: {
        title: "Dinamik rozet",
        description:
          "Profil README'ne ekleyebilecegin, her zaman guncel ve hizli calisan bir streak rozeti uretiriz.",
      },
      control: {
        title: "Sen kontrol edersin",
        description:
          "Bildirim saatini secersin, istedigin an durdurursun, hesabini tek tikla silersin.",
      },
    },
    privacyLink: "Hangi izinleri neden istiyoruz?",
    errors: {
      access_denied:
        "GitHub izni verilmedi. Devam etmek icin yetkilendirmeyi onaylaman gerekiyor.",
      missing_code: "GitHub'dan beklenen yanit alinamadi. Lutfen tekrar dene.",
      invalid_state:
        "Guvenlik dogrulamasi basarisiz oldu. Lutfen girisi bastan baslat.",
    },
  },

  onboarding: {
    title: "Son bir adim kaldi",
    greeting: "Merhaba",
    intro:
      "Bildirimleri calistirabilmemiz icin hesabinda kucuk bir kurulum yapmamiz gerekiyor. Ne yapacagimizi asagida acikca anlatiyoruz.",
    whatWeDo: "Onayinla neler yapacagiz?",
    steps: {
      repo: {
        title: "Gizli bir repo olusturacagiz",
        body: "adinda gizli (private) bir repo acilacak. Icerigini senden baskasi goremez.",
      },
      issue: {
        title: "Icine tek bir Issue acacagiz",
        body: "Bildirimleri bu Issue'ya yorum olarak dusurecegiz. GitHub Mobile bu yorumu telefonuna push bildirimi olarak iletir.",
      },
      contributions: {
        title: "Katki gecmisini okuyacagiz",
        body: "Streak'ini hesaplayabilmek icin gunluk katki takvimini okuyoruz. Kodlarini okumuyoruz; yalnizca hangi gun kac katki yaptigin bilgisini kullaniyoruz.",
      },
    },
    repoScope: {
      title: 'Neden "repo" izni istiyoruz?',
      body: "GitHub, gizli repo olusturabilmek ve private repo'lardaki commit'lerinin seriye sayilabilmesi icin bu izni zorunlu tutuyor. Daha dar bir izin secenegi sunmuyor. Izni istedigin an GitHub ayarlarindan geri alabilirsin.",
    },
    notificationHour: "Bildirim saati",
    hourNote:
      "O saatte hala commit atmamissan uyari gonderiyoruz. Bu ayari sonradan degistirebilirsin.",
    timeZoneDetected: "Saat dilimin",
    timeZoneNote:
      "olarak algilandi. Seri hesabi ve bildirimler bu dilime gore yapilir; panelden degistirebilirsin.",
    consent:
      "Yukaridaki islemleri okudum ve onayliyorum. GitHub hesabimda gizli repo ve Issue olusturulmasina, katki takvimimin streak hesabi icin okunmasina izin veriyorum.",
    submit: "Onayla ve kurulumu tamamla",
    submitting: "Kurulum yapiliyor…",
    noConsentNote: "Onay vermezsen hesabinda hicbir sey olusturulmaz.",
    error: "Kurulum tamamlanamadi.",
  },

  dashboard: {
    panelSubtitle: "StreakTracker paneli",
    currentStreak: "Guncel seri",
    record: "Rekor",
    today: "Bugun",
    days: "gun",
    committed: "Commit atildi",
    notCommitted: "Henuz commit yok",
    committedNote: "Serin bugunluk guvende.",
    notCommittedNote: "Gun bitmeden bir commit at, serini koru.",
    calendar: "Katki takvimi",
    refresh: "Yenile",
    refreshing: "Yenileniyor…",
    refreshed: "Veriler GitHub'dan tazelendi.",
    badge: "Profil rozetin",
    badgeMarkdown: "Markdown (README icin)",
    badgeHtml: "HTML",
    badgeTheme: "Tema",
    badgeVariant: "Boyut",
    badgeVariantFull: "Tam",
    badgeVariantCompact: "Kompakt",
    badgeRankNote:
      "Rutbe serine gore degisir: 1 gun Kivilcim, 7 Alev, 30 Ates, 100 Yangin, 365 Efsane.",
    badgeCustomize: "Rozeti ozellestir",
    settings: "Bildirim ayarlari",
    hourLabel: "Bildirim saati",
    timeZone: "Saat dilimi",
    timeZoneNote: "Seri hesabin ve bildirimler",
    timeZoneNoteSuffix: "dilimine gore yapiliyor (su an orada saat",
    browserShows: "Tarayicin",
    browserShowsSuffix: "gosteriyor — farkliysa yukaridan duzeltebilirsin.",
    notifications: "Bildirimler",
    notificationsNote: "Kapatirsan sana hic bildirim gonderilmez.",
    on: "Acik",
    off: "Kapali",
    testNotification: "Test bildirimi",
    testNote: "Telefonuna push bildirimi dusuyor mu, hemen dene.",
    sendTest: "Test gonder",
    sending: "Gonderiliyor…",
    notificationsTarget: "Bildirimler",
    notificationsTargetSuffix: "adresine yorum olarak dusuruluyor.",
    hourUpdated: "Bildirim saati guncellendi.",
    timeZoneUpdated: "Saat dilimi guncellendi.",
    languageUpdated: "Dil guncellendi.",
    notificationsOn: "Bildirimler acildi.",
    notificationsOff: "Bildirimler kapatildi.",
    deleteTitle: "Hesabi sil",
    deleteNote:
      "StreakTracker'daki tum verilerin (profil, streak gecmisi, bildirim kayitlari) kalici olarak silinir. GitHub hesabindaki gizli repo silinmez; ona sen karar verirsin.",
    deleteButton: "Hesabimi ve verilerimi sil",
    deleteConfirm: "Hesabin ve tum verilerin silinecek. Emin misin?",
    badgeLocalNote:
      "Rozet adresini profil README'ne ekleyebilirsin. Farkli dilde gostermek istersen adresin sonuna ?lang=en ekleyebilirsin.",
    loadError: "Veriler yuklenemedi.",
    actionError: "Islem tamamlanamadi.",
  },

  heatmap: {
    totalPrefix: "Son bir yilda",
    totalSuffix: "katki",
    less: "Az",
    more: "Cok",
    contributions: "katki",
    empty: "Henuz gosterilecek katki verisi yok.",
    dayLabels: ["Pzt", "Çar", "Cum"],
  },

  appInstall: {
    notConfiguredTitle: "Bildirimler henuz etkin degil",
    notConfiguredBody:
      "Sunucuda GitHub App yapilandirilmamis. Bildirimlerin telefona dusebilmesi icin yoneticinin App kurulumunu tamamlamasi gerekiyor.",
    title: "Son adim: bildirim uygulamasini kur",
    body: "GitHub, kendi yaptigin islemler icin sana bildirim gondermez. Bu yuzden uyarilari senin adina degil, ayri bir bot kimligiyle gonderiyoruz. Bot'un yorum atabilmesi icin GitHub App'i kurmalisin — kurulum sirasinda yalnizca bildirim reposunu secmen yeterli.",
    installButton: "GitHub App'i kur",
    checkButton: "Kurdum, kontrol et",
    checking: "Kontrol ediliyor…",
    notFound:
      "Kurulum henuz gorunmuyor. GitHub'da kurulumu tamamladigindan ve bildirim reposuna erisim verdiginden emin ol.",
    checkError: "Kontrol edilemedi.",
    verified: "GitHub App kurulumu dogrulandi. Bildirimler artik calisiyor.",
  },

  customize: {
    title: "Rozeti ozellestir",
    subtitle: "Degisiklikler aninda onizlemede gorunur; kaydedene kadar rozetin degismez.",
    back: "Panele don",
    preview: "Onizleme",
    sizeSection: "Boyut",
    sizeFull: "Normal",
    sizeFullNote: "Dengeli, her yere uyar",
    sizeCompact: "Kompakt",
    sizeCompactNote: "Yan yana dizmek icin",
    sizeMax: "Genis",
    sizeMaxNote: "README'de bir basligi kaplar",
    themeSection: "Tema",
    colorSection: "Renkler",
    colorNote: "Bos birakirsan temanin kendi renkleri kullanilir.",
    flameFrom: "Alev - ust",
    flameTo: "Alev - alt",
    background: "Arka plan",
    border: "Kenarlik",
    reset: "Temaya don",
    animation: "Alev animasyonu",
    animationNote: "Alev yanip soner. Isletim sisteminde 'hareketi azalt' seciliyse kendiliginden durur.",
    save: "Kaydet",
    saving: "Kaydediliyor…",
    saved: "Rozetin guncellendi. Profilindeki gorunum birkac dakika icinde yenilenir.",
    saveError: "Kaydedilemedi.",
    snippetTitle: "README kodun",
    snippetNote:
      "Gorunumunu her degistirdiginde bu kod da degisir - profilindeki rozetin guncellenmesi icin yeniden kopyalaman gerekir.",
    ranksTitle: "Rutbeler",
    ranksNote:
      "Alev sekli secilmez, kazanilir: serin buyudukce rozetindeki alev de degisir.",
    rankCurrent: "Su anki rutben",
    rankUnlocked: "Kazanildi",
    rankDaysLeft: "gun kaldi",
    rankRequirement: "gun",
  },

  privacy: {
    title: "Izinler ve Gizlilik",
    permissions: "Hangi izinleri istiyoruz?",
    repoScope:
      "Gizli bildirim reposunu olusturmak, Issue'ya yorum atmak ve private repo'lardaki commit'lerin seriye sayilmasi icin. GitHub bunun icin daha dar bir izin sunmuyor.",
    readUser: "Profil bilgin ve katki takvimin icin.",
    userEmail: "Yedek bildirim kanali icin e-posta adresin.",
    whatWeStore: "Neleri sakliyoruz?",
    whatWeStoreBody:
      "GitHub kullanici adin, sayisal kimligin, e-postan, avatar adresin; streak verilerin (guncel seri, rekor, son commit gunu) ve gonderilen bildirimlerin kaydi. GitHub access token'in veritabaninda sifrelenmis olarak tutulur.",
    whatWeDont: "Neleri saklamiyoruz?",
    whatWeDontBody:
      "Kodlarini okumuyor, indirmiyor veya saklamiyoruz. Repo iceriklerine erisimimiz yalnizca bildirim reposunu olusturmak ve o repodaki Issue'ya yorum atmakla sinirli kullanilir.",
    deletion: "Verilerini silmek istersen",
    deletionBody:
      "Panelindeki Hesabi sil butonu ile tum verilerin kalici olarak silinir. Ayrica GitHub ayarlarindan uygulamanin erisimini istedigin an geri alabilirsin.",
  },
};
// Not: burada "as const" KULLANILMIYOR. Kullanilsaydi her metin kendi literal
// tipine donusur ve Ingilizce sozluk "ayni metin degil" diye reddedilirdi.
// Boylece tr sozlugu sema gorevi goruyor: en'de eksik/fazla anahtar derleme hatasi verir.

const en: typeof tr = {
  meta: {
    siteTitle: "StreakTracker — Don't Break Your GitHub Streak",
    siteDescription:
      "Track your GitHub commit streak, get a push notification on your phone before it breaks, and add a dynamic badge to your profile.",
    privacyTitle: "Permissions and Privacy — StreakTracker",
  },

  common: {
    loading: "Loading…",
    save: "Save",
    cancel: "Cancel",
    close: "Close",
    back: "Home",
    logout: "Sign out",
    language: "Language",
  },

  landing: {
    title: "Don't break the chain.",
    subtitle:
      "We track your GitHub commit streak. If you haven't committed before the day ends, we send a push notification to your phone — through GitHub's own mobile app.",
    loginButton: "Sign in with GitHub",
    loginNote:
      "Signing in creates nothing in your account. Setup starts only after you read and approve the details.",
    features: {
      notification: {
        title: "Push to your phone",
        description:
          "When your streak is at risk you get an instant push via GitHub Mobile. No extra app to install.",
      },
      badge: {
        title: "Dynamic badge",
        description:
          "An always up-to-date, fast streak badge you can drop into your profile README.",
      },
      control: {
        title: "You stay in control",
        description:
          "Pick your notification hour, pause anytime, delete your account with one click.",
      },
    },
    privacyLink: "Which permissions do we ask for, and why?",
    errors: {
      access_denied:
        "GitHub access was denied. You need to approve the authorization to continue.",
      missing_code: "We didn't get the expected response from GitHub. Please try again.",
      invalid_state:
        "Security verification failed. Please start the sign-in again.",
    },
  },

  onboarding: {
    title: "One last step",
    greeting: "Hi",
    intro:
      "To make notifications work we need a small setup in your account. Here's exactly what we'll do.",
    whatWeDo: "What happens when you approve?",
    steps: {
      repo: {
        title: "We create a private repository",
        body: "will be created as a private repository. Nobody but you can see its contents.",
      },
      issue: {
        title: "We open a single Issue inside it",
        body: "Notifications are posted as comments on that Issue. GitHub Mobile delivers them to your phone as push notifications.",
      },
      contributions: {
        title: "We read your contribution history",
        body: "To calculate your streak we read your daily contribution calendar. We don't read your code; only how many contributions you made on which day.",
      },
    },
    repoScope: {
      title: 'Why do we need the "repo" permission?',
      body: "GitHub requires it to create a private repository and to count commits in private repositories toward your streak. There is no narrower option. You can revoke access anytime from your GitHub settings.",
    },
    notificationHour: "Notification hour",
    hourNote:
      "If you still haven't committed at that hour, we send a warning. You can change this later.",
    timeZoneDetected: "Your time zone was detected as",
    timeZoneNote:
      ". Streak calculation and notifications follow this zone; you can change it from the dashboard.",
    consent:
      "I have read and approve the actions above. I allow a private repository and Issue to be created in my GitHub account, and my contribution calendar to be read for streak calculation.",
    submit: "Approve and finish setup",
    submitting: "Setting up…",
    noConsentNote: "If you don't approve, nothing is created in your account.",
    error: "Setup could not be completed.",
  },

  dashboard: {
    panelSubtitle: "StreakTracker dashboard",
    currentStreak: "Current streak",
    record: "Record",
    today: "Today",
    days: "days",
    committed: "Committed",
    notCommitted: "No commit yet",
    committedNote: "Your streak is safe for today.",
    notCommittedNote: "Commit before the day ends to keep your streak.",
    calendar: "Contribution calendar",
    refresh: "Refresh",
    refreshing: "Refreshing…",
    refreshed: "Data refreshed from GitHub.",
    badge: "Your profile badge",
    badgeMarkdown: "Markdown (for README)",
    badgeHtml: "HTML",
    badgeTheme: "Theme",
    badgeVariant: "Size",
    badgeVariantFull: "Full",
    badgeVariantCompact: "Compact",
    badgeRankNote:
      "Your rank grows with your streak: 1 day Spark, 7 Flame, 30 Fire, 100 Blaze, 365 Legend.",
    badgeCustomize: "Customize badge",
    settings: "Notification settings",
    hourLabel: "Notification hour",
    timeZone: "Time zone",
    timeZoneNote: "Streak calculation and notifications follow",
    timeZoneNoteSuffix: "(local time there is now",
    browserShows: "Your browser reports",
    browserShowsSuffix: "— if that's correct, update it above.",
    notifications: "Notifications",
    notificationsNote: "If you turn this off, we won't send you anything.",
    on: "On",
    off: "Off",
    testNotification: "Test notification",
    testNote: "Check right away whether push reaches your phone.",
    sendTest: "Send test",
    sending: "Sending…",
    notificationsTarget: "Notifications are posted to",
    notificationsTargetSuffix: "as comments.",
    hourUpdated: "Notification hour updated.",
    timeZoneUpdated: "Time zone updated.",
    languageUpdated: "Language updated.",
    notificationsOn: "Notifications enabled.",
    notificationsOff: "Notifications disabled.",
    deleteTitle: "Delete account",
    deleteNote:
      "All your StreakTracker data (profile, streak history, notification logs) is permanently deleted. The private repository in your GitHub account is not deleted; that's your call.",
    deleteButton: "Delete my account and data",
    deleteConfirm: "Your account and all data will be deleted. Are you sure?",
    badgeLocalNote:
      "Add the badge URL to your profile README. To show it in another language, append ?lang=en to the URL.",
    loadError: "Could not load data.",
    actionError: "Action could not be completed.",
  },

  heatmap: {
    totalPrefix: "In the last year:",
    totalSuffix: "contributions",
    less: "Less",
    more: "More",
    contributions: "contributions",
    empty: "No contribution data to show yet.",
    dayLabels: ["Mon", "Wed", "Fri"],
  },

  appInstall: {
    notConfiguredTitle: "Notifications aren't active yet",
    notConfiguredBody:
      "The GitHub App isn't configured on the server. An administrator needs to complete the App setup before notifications can reach your phone.",
    title: "Last step: install the notification app",
    body: "GitHub doesn't notify you about your own actions. That's why warnings are sent by a separate bot identity rather than by you. For the bot to post comments you need to install the GitHub App — during setup, selecting only the notification repository is enough.",
    installButton: "Install the GitHub App",
    checkButton: "I installed it, check now",
    checking: "Checking…",
    notFound:
      "The installation isn't visible yet. Make sure you completed it on GitHub and granted access to the notification repository.",
    checkError: "Could not check.",
    verified: "GitHub App installation verified. Notifications work now.",
  },

  customize: {
    title: "Customize badge",
    subtitle: "Changes appear in the preview instantly; your badge stays the same until you save.",
    back: "Back to dashboard",
    preview: "Preview",
    sizeSection: "Size",
    sizeFull: "Normal",
    sizeFullNote: "Balanced, fits anywhere",
    sizeCompact: "Compact",
    sizeCompactNote: "For placing side by side",
    sizeMax: "Wide",
    sizeMaxNote: "Fills a section in your README",
    themeSection: "Theme",
    colorSection: "Colors",
    colorNote: "Leave empty to use the theme's own colors.",
    flameFrom: "Flame - top",
    flameTo: "Flame - bottom",
    background: "Background",
    border: "Border",
    reset: "Back to theme",
    animation: "Flame animation",
    animationNote: "The flame flickers. It stops automatically if your system prefers reduced motion.",
    save: "Save",
    saving: "Saving…",
    saved: "Badge updated. The version on your profile refreshes within a few minutes.",
    saveError: "Could not save.",
    snippetTitle: "Your README code",
    snippetNote:
      "This code changes every time you change the look - copy it again so the badge on your profile updates.",
    ranksTitle: "Ranks",
    ranksNote:
      "The flame isn't chosen, it's earned: as your streak grows, so does the flame on your badge.",
    rankCurrent: "Your current rank",
    rankUnlocked: "Earned",
    rankDaysLeft: "days to go",
    rankRequirement: "days",
  },

  privacy: {
    title: "Permissions and Privacy",
    permissions: "Which permissions do we ask for?",
    repoScope:
      "To create the private notification repository, post comments on the Issue, and count commits in private repositories toward your streak. GitHub offers no narrower permission for this.",
    readUser: "For your profile information and contribution calendar.",
    userEmail: "Your email address, for the fallback notification channel.",
    whatWeStore: "What do we store?",
    whatWeStoreBody:
      "Your GitHub username, numeric id, email, avatar URL; your streak data (current streak, record, last commit day) and a log of notifications sent. Your GitHub access token is stored encrypted.",
    whatWeDont: "What don't we store?",
    whatWeDontBody:
      "We don't read, download or store your code. Our access to repository contents is used only to create the notification repository and post comments on its Issue.",
    deletion: "If you want your data deleted",
    deletionBody:
      "The Delete account button in your dashboard permanently removes all your data. You can also revoke the app's access anytime from your GitHub settings.",
  },
};

export const dictionaries: Record<Locale, typeof tr> = { tr, en };

export type Dictionary = typeof tr;

export function getDictionary(locale: Locale): Dictionary {
  return dictionaries[locale];
}
