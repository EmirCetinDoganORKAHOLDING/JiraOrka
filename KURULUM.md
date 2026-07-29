# Tezgah - Kurulum Kılavuzu

## Gereksinimler
- .NET 8 SDK → https://dotnet.microsoft.com/download/dotnet/8.0

## Kurulum

### 1. Terminalde proje klasörüne girin
```bash
cd /path/to/Tezgah
```

### 2. Bağımlılıkları yükleyin
```bash
dotnet restore
```

### 3. Uygulamayı başlatın
```bash
dotnet run
```

Uygulama otomatik olarak `jiraorka.db` SQLite veritabanını oluşturur.

### 4. Tarayıcıda açın
```
http://localhost:5000
```

## Giriş Bilgileri (Varsayılan)

| Alan | Değer |
|------|-------|
| Email | admin@jiraorka.com |
| Şifre | Admin@123 |

## Gmail Entegrasyonu

`appsettings.json` dosyasını açıp şu bölümü doldurun:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "senin-gmail@gmail.com",
  "SenderPassword": "gmail-uygulama-sifresi",
  "SenderName": "Tezgah"
}
```

> **Not:** Gmail'de "Uygulama Şifresi" kullanmanız gerekiyor.
> Google Hesabı → Güvenlik → 2 Adımlı Doğrulama açık olmalı → Uygulama Şifreleri

## Özellikler

- ✅ Kanban Board (sürükle-bırak)
- ✅ Görev oluşturma, düzenleme, atama
- ✅ Yetki grubu yönetimi (süper admin)
- ✅ Kullanıcı yönetimi
- ✅ Gmail ile email bildirimleri
- ✅ Yorum sistemi
- ✅ Proje yönetimi
- ✅ Öncelik ve son tarih takibi
- ✅ Responsive tasarım

## Proje Yapısı

```
Tezgah/
├── Controllers/     → HTTP istekleri
├── Models/          → Veri modelleri
├── ViewModels/      → Form modelleri
├── Data/            → DB bağlantısı & init
├── Repositories/    → Dapper CRUD
├── Services/        → Email, current user
├── Views/           → Razor UI
└── wwwroot/         → CSS, JS
```
