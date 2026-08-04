# 🔥 Rozet Görsel Kütüphanesi

Buraya eklenen SVG'ler, kullanıcıların rozetlerinde seçebileceği **alev şekilleri** ve **ekipmanlar** olur.

```
Assets/Badges/
├── flames/         alevin kendisi (kullanıcı birini seçer)
└── accessories/    alevle birlikte çizilen parçalar (meşale, taç, kanat...)
```

---

## Dosya nasıl eklenir?

1. Şekli indir veya çiz
2. İlgili klasöre `.svg` olarak at
3. Dosya adı **küçük harf ve tire** ile olsun: `sharp.svg`, `royal-crown.svg`
   *(Bu ad, rozet adresinde `?flame=sharp` şeklinde görünecek.)*

Sonrasını ben hallederim: kütüphaneye bağlama, konumlandırma, ölçekleme.

---

## ⚠️ Tek kritik kural: renk olmayacak

Şekil **tek renkli** olmalı ve dosyada **sabit renk bulunmamalı**.

```svg
<!-- ✅ DOĞRU: renk yok, rozet gradyanı uygulanır -->
<path d="M12 2c0 4-3 5.5-5 8..."/>

<!-- ❌ YANLIŞ: sabit renk, kullanıcının renk seçimi çalışmaz -->
<path d="M12 2c0 4-3 5.5-5 8..." fill="#ffffff"/>
```

Game Icons'tan indirdiğin dosyalarda genelde `fill="#fff"` bulunur — **onu silmen yeterli**, gerisini ben ayarlarım. (Silmezsen de ben temizlerim, sadece bilgin olsun.)

---

## Sık sorulanlar

**viewBox farklı olursa?**
Sorun değil. Game Icons `512x512`, Lucide `24x24` kullanır — ikisi de olur, ben normalleştiririm.

**Birden fazla `<path>` olabilir mi?**
Olur. `classic.svg`'de iki tane var (dış hat + iç çekirdek). İç çekirdek `class="core"` ile işaretlenirse ayrı animasyonla titrer.

**Ekipmanı nereye çizeceğimi nasıl bileyim?**
Bilmene gerek yok. Meşalenin altta, tacın üstte durması gerektiğini ben tanımlayacağım. Sen sadece şekli at.

**Lisans?**
Kullandığın setin lisansını söyle, README'ye kaynak notu ekleyeyim:

| Set | Lisans | Gereken |
|---|---|---|
| [Game Icons](https://game-icons.net) | CC BY 3.0 | Kaynak belirtmek |
| [Lucide](https://lucide.dev) | ISC | — |
| [Phosphor](https://phosphoricons.com) | MIT | — |
| [Tabler](https://tabler.io/icons) | MIT | — |

---

## Mevcut dosyalar

| Dosya | Açıklama |
|---|---|
| `flames/classic.svg` | Şu an kullanılan alev — format örneği |
| `accessories/torch.svg` | Basit meşale sapı — format örneği |

Bu ikisi örnek amaçlı; istersen üzerine yaz ya da yanlarına yenilerini ekle.
