# Backend API Kuralları

## Genel
- **Controller içinde iş kuralı yok.** Controller: request alır → Application servisini çağırır → `ApiResponse<T>` döner.
- Her endpoint DTO ile çalışır; **entity asla doğrudan dönülmez**.
- Her mutation `IUnitOfWork.SaveChangesAsync(ct)` ile atomik tamamlanır.
- Identity kullanıcı modeli `ApplicationUser` ile genişletilir.
- Role + policy bazlı authorization kullanılır.
- Refresh token DB'de **hash'li** tutulur (düz metin asla).
- JWT secret `appsettings` içinde değil; environment / user-secrets / secret manager'da.
- Global exception middleware kullanılır.
- API response formatı standarttır (aşağıda).
- EF Core sorgularında varsayılan `AsNoTracking`.
- Migration isimleri anlamlıdır.

## Standart yanıt zarfı
```csharp
public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
```
```json
{ "success": true, "message": "İşlem başarılı", "data": {}, "errors": [] }
```

## HTTP durum kodları
| Durum | Kod |
|---|---|
| Başarılı okuma/işlem | 200 |
| Oluşturma | 201 |
| Geçersiz giriş / validasyon | 400 |
| Kimlik doğrulama yok/geçersiz | 401 |
| Yetki yok | 403 |
| Bulunamadı | 404 |
| Çakışma (ör. tekrar eden e-posta) | 409 |
| Rate limit aşımı | 429 |
| Sunucu hatası | 500 |

## Validation
- FluentValidation; her komut/DTO için ayrı validator.
- Validasyon hataları `ApiResponse.Errors` listesine, 400 ile döner.

## Rate limiting policy'leri
| Policy | Hedef | Strateji |
|---|---|---|
| `auth-login` | Brute force koruması | IP + kullanıcı adı, düşük limit |
| `auth-refresh` | Token abuse | Kullanıcı + token bazlı |
| `urun-arama` | Yoğun sorgu | Sliding window (kullanıcı/IP) |
| `admin-sensitive` | Kritik admin işlemleri | Çok düşük limit + audit log |

> Limitler deployment öncesi yük testiyle doğrulanmalı.

## Async & iptal
- Tüm IO `async`; controller ve servis imzalarında `CancellationToken` taşınır.

## Loglama
- `ILogger<T>`. İstek korelasyonu, hata seviyesinde stack trace (yalnızca sunucu logunda, client'a değil).
