# Kod Standartları

## C# (.NET 9)
- `Nullable` ve `ImplicitUsings` açık (tüm projelerde).
- İsimlendirme: tip/metot/property `PascalCase`, yerel değişken/parametre `camelCase`, private alan `_camelCase`, sabit `PascalCase`, arayüz `IPascalCase`.
- Dosya başına bir public tip. Dosya adı = tip adı.
- `async` metotlar `Async` son ekiyle; `CancellationToken` son parametre.
- LINQ sorgularında okunabilirlik; gereksiz `ToList()` materyalizasyonundan kaçın.
- Exception yutma yok; beklenen hatalar için Result/özel exception, beklenmeyen için global middleware.
- DI: somut tipe değil arayüze bağımlılık. Servisler `scoped`, stateless yardımcılar `singleton`.
- Sihirli sabit yok; `const`/enum/options kullan.

## TypeScript / React
- `strict: true`. `any` yerine `unknown` + daraltma.
- Bileşen dosyaları `PascalCase.tsx`, hook'lar `useX.ts`, yardımcılar `camelCase.ts`.
- Named export tercih edilir (default export yalnızca sayfa/route bileşenlerinde).
- Prop tipleri açık interface ile; opsiyoneller `?` ile.
- Yan etkiler `useEffect` içinde net bağımlılık dizisiyle.
- Türetilebilir değeri state'te tutma; `useMemo`/hesaplama kullan.

## Git / commit
- Anlamlı, küçük commit'ler. Mesaj formatı: `<alan>: <özet>` (ör. `backend: add Lead entity`).
- Migration ve şema değişiklikleri ayrı commit.

## Genel
- Yorumlar "neden"i anlatır, "ne"yi değil. Çevredeki kodun yorum yoğunluğuna uy.
- Ölü kod ve kullanılmayan import bırakma.
- Sırlar (secret) repoya girmez; `.env.example` ile belgelenir.
