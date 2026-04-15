# Finansal Veri Platformu Proje Planý (.NET 8 MVC)

Bu proje `HisseFiyatlari.com`, `hissefiyatlari.com` ve `bankafaiz.com` için ortak kullanýlacak çekirdek web uygulamasýdýr. 

## Kullanýlacak Teknolojiler
* **Backend:** .NET 8 ASP.NET Core MVC
* **Veritabaný:** MSSQL Server & Entity Framework Core (Code First)
* **Arka Plan Görevleri:** Hangfire (API istekleri ve eski veri temizliði için)
* **Frontend:** HTML5, CSS3, Bootstrap 5 (Mobil uyumluluk için), jQuery (DataTables.js - Excel export için)
* **Mail Altyapýsý:** MailKit / SMTP (Ýletiþim formu için)

## Adým Adým Kurulum ve Geliþtirme

### Adým 1: Proje Kurulumu ve Mimari
1. Visual Studio üzerinden yeni bir `ASP.NET Core Web App (Model-View-Controller)` projesi oluþtur.
2. Proje yapýsýný katmanlara ayýr veya klasörle (Models, Views, Controllers, Services, Data).
3. `appsettings.json` içerisine MSSQL Connection String'i ekle.
4. Entity Framework Core, Hangfire ve API çaðrýlarý için RestSharp/HttpClient paketlerini NuGet üzerinden kur.

### Adým 2: Veritabaný ve Modellerin (Entity) Oluþturulmasý
Aþaðýdaki tablolar için modelleri oluþtur:
* **MarketData:** API'den gelen verilerin tutulacaðý ana tablo (Id, DataType[Altin/Hisse/Faiz], Name, Price, ChangeRate, RecordDate).
* **Menus:** Dinamik menü yönetimi (Id, Title, Url, Content, IsActive).
* **Sponsors:** Sponsor yönetimi (Id, Name, LogoUrl, TargetUrl, Order).
* **Settings:** Sistem ayarlarý (Id, AdsenseCode, AnalyticsCode, FooterText, FacebookLink, TwitterLink, InstagramLink vb.).

### Adým 3: Arka Plan Görevleri (Hangfire Entegrasyonu)
1. `Program.cs` içerisinde Hangfire servisini MSSQL depolamasý yapacak þekilde yapýlandýr.
2. **FetchApiDataJob:** Belirlenen aralýklarla (örneðin 2 saatte bir) çalýþýp API'den veri çeken ve `MarketData` tablosuna o günün tarihiyle insert/update yapan metodu yaz. Gece 12'den sonra otomatik olarak yeni güne ait kayýtlarý oluþturacak mantýðý kurgula.
3. **CleanUpOldDataJob:** Her gece 01:00'da çalýþacak ve `RecordDate`'i üzerinden 5 gün geçmiþ olan verileri `MarketData` tablosundan silecek metodu yaz.

### Adým 4: Admin Panel Geliþtirmesi
1. `Areas/Admin` oluþtur.
2. Basit bir yetkilendirme (Cookie Authentication) ekle.
3. Menü, Sponsor, Sosyal Medya ve Sistem ayarlarýnýn (Adsense vb.) CRUD (Ekle, Sil, Güncelle, Listele) iþlemlerini yapacak Controller ve View'larý oluþtur.

### Adým 5: Önyüz (UI) ve Görünüm
1. Ýþ Yatýrým sitesi referans alýnarak layout (`_Layout.cshtml`) tasarýmýný yap.
2. Kayan üst menüyü CSS animasyonu veya basit bir JS ticker ile oluþtur.
3. Orta alana veritabanýndan çekilen güncel (ve geçmiþ sekmeli) `MarketData` verilerini bas. DataTables.js kurarak "Excel'e Aktar" butonunu aktif et.
4. Sað sütuna `Sponsors` tablosundan aktif sponsorlarý alt alta listele.
5. Footer'ý `Settings` tablosundan gelen verilerle dinamik hale getir (Sosyal medya ikonlarý boþsa/pasifse `if` bloðu ile gizle).
6. Dinamik sayfalar (Hakkýmýzda vb.) için ortak bir Action oluþtur ve URL/Slug'a göre menü içeriðini veritabanýndan çekerek göster.

### Adým 6: Ýletiþim Formu ve SEO
1. Ýletiþim sayfasýndaki formu POST edildiðinde `bilgi@ebiscube.com` adresine gönderecek SMTP entegrasyonunu yap.
2. Uygulama ayaða kalkarken veritabanýndaki dinamik linkleri ve statik sayfalarý okuyup otomatik `sitemap.xml` üretecek bir middleware veya Controller yaz.
3. Mobil cihazlarda test ederek tablolarýn ve menülerin düzgün (responsive) davrandýðýndan emin ol.