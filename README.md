# Real-Time Stock Market XML Feed API

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Bu proje, modern bir ASP.NET Core Web API'si aracılığıyla gerçek zamanlı borsa verilerini sunan kapsamlı bir sistemdir. Projenin temel amacı, XML teknolojilerinin (XSD, DTD, XSLT, LINQ to XML) modern web servisleri mimarilerinde nasıl etkin bir şekilde kullanılabileceğini göstermektir. Sistem, JSON formatında veri sağlayan harici bir Finnhub API'sini tüketir, bu verileri özel ve şema ile doğrulanmış bir XML formatına dönüştürür ve JWT tabanlı kimlik doğrulama ile güvence altına alınmış RESTful endpoint'ler üzerinden sunar.

Proje, hem sunucu tarafı API'yi hem de bu API'yi tüketen, Vanilla JavaScript ile yazılmış dinamik bir istemci (frontend) uygulamasını içerir.

Bu, Mersin Üniversitesi Bilgisayar Mühendisliği Bölümü "XML ve Web Servisleri" dersi final projesi olarak geliştirilmiştir.

## 🌟 Öne Çıkan Özellikler

- **XML Odaklı Mimari**: Tüm veri iletişimi, güçlü bir veri sözleşmesi sağlamak için özel olarak tasarlanmış ve doğrulanmış XML formatı üzerinden yapılır.
- **Güvenli Kimlik Doğrulama**: Endpoint'lere erişim, endüstri standardı olan **JWT (JSON Web Token)** ile korunmaktadır. Rol tabanlı yetkilendirme (`Admin`, `User`) mevcuttur.
- **XML Şema Doğrulama**: API tarafından üretilen XML'in yapısal bütünlüğü ve doğruluğu, sunucu tarafında programatik olarak **XSD (XML Schema Definition)** ve **DTD (Document Type Definition)** şemalarına karşı doğrulanır.
- **Dinamik XML Dönüşümü**: **XSLT (Extensible Stylesheet Language Transformations)** kullanılarak, ham XML verileri sunucu tarafında dinamik olarak kullanıcı dostu bir HTML raporuna dönüştürülür.
- **Gelişmiş XML Sorgulama**: **LINQ to XML** kullanılarak sunucu tarafında karmaşık XML sorgulama ve filtreleme yetenekleri sergilenir.
- **Harici API Entegrasyonu**: Gerçek zamanlı borsa verileri için **Finnhub API**'si ile entegrasyon.
- **Katmanlı Mimari**: Sorumlulukların net bir şekilde ayrılması için `Controller`, `Service` ve `Repository` katmanlarından oluşan temiz bir backend mimarisi.
- **İnteraktif Frontend**: Verileri tablo formatında görüntüleyen, TradingView ile zengin grafikler sunan ve kullanıcı tercihlerini (CRUD) yöneten, sıfırdan **Vanilla JavaScript** ile yazılmış bir Single-Page Application (SPA).
- **Kapsamlı Test**: Swagger UI ile interaktif test, Thunder Client/Postman ile endpoint testleri ve xUnit/Moq ile birim/entegrasyon testleri.

## 📸 Ekran Görüntüleri

*(Buraya projenizden birkaç ekran görüntüsü veya GIF ekleyin. Raporunuzdaki görseller harika, onları kullanabilirsiniz!)*

**Giriş ve Kayıt Ekranı:**
`![Giriş Ekranı](./path/to/your/login-image.png)`

**Ana Veri Görüntüleme ve Tercih Düzenleme:**
`![Ana Veri Görüntüleme](./path/to/your/dashboard-image.png)`

**TradingView Grafik Entegrasyonu:**
`![Grafik Entegrasyonu](./path/to/your/chart-image.png)`

**XSLT ile Oluşturulan HTML Raporu:**
`![HTML Raporu](./path/to/your/report-image.png)`

## 🛠️ Teknoloji Yığını

### Backend (StockMarket.Api)

- **Framework**: ASP.NET Core 9
- **Dil**: C#
- **XML Teknolojileri**:
  - `XmlSerializer` (Nesne-XML bağlama)
  - `XDocument` (LINQ to XML ile sorgulama)
  - `XmlReader` / `XmlSchemaSet` (XSD ve DTD doğrulaması)
  - `XslCompiledTransform` (XSLT dönüşümü)
- **Kimlik Doğrulama**: `Microsoft.AspNetCore.Authentication.JwtBearer` (JWT)
- **Veri Kaynağı**:
  - **Harici**: Finnhub API (RESTful JSON)
  - **Dahili**: In-Memory & `users.json` (Kalıcı olmayan kullanıcı verileri için)
- **API Dokümantasyonu**: Swashbuckle (Swagger/OpenAPI)
- **Test**: xUnit, Moq

### Frontend (StockMarket.Client)

- **Dil**: Vanilla JavaScript (ES6+)
- **İşaretleme**: HTML5
- **Stil**: CSS3 (Flexbox ile modern tasarım)
- **API İletişimi**: Fetch API
- **XML İşleme**: `DOMParser` (Tarayıcı tabanlı XML ayrıştırma)
- **Grafikler**: TradingView Widgets

## 🏗️ Mimari

Sistem, klasik bir **İstemci-Sunucu** mimarisini takip eder.

- **Frontend (StockMarket.Client)**: Kullanıcı arayüzünü sunar, kullanıcı etkileşimlerini yönetir ve backend API'sine istekler gönderir.
- **Backend (StockMarket.Api)**: RESTful prensiplerine uygun olarak tasarlanmış olup **Katmanlı Mimari** kullanır:
  - **Presentation Layer (Controllers)**: Gelen HTTP isteklerini karşılar, doğrular ve uygun servislere yönlendirir.
  - **Business Logic Layer (Services)**: Uygulamanın çekirdek iş mantığını içerir. Harici API'den veri çekme, XML işleme ve kimlik doğrulama gibi işlemleri yönetir.
  - **Data Access Layer (Repositories)**: Veri depolama mekanizmasını soyutlar. Bu projede, kullanıcı verileri için basit bir in-memory repository (JSON dosyası ile desteklenmiş) kullanılır.

**Backend Proje Yapısı:**
```
/StockMarket.Api
|-- Controllers/        // API endpoint'leri
|   |-- AuthController.cs
|   |-- StocksController.cs
|   |-- UserPreferencesController.cs
|   `-- ValidationController.cs
|-- Models/             // Veri modelleri ve DTO'lar
|   |-- Stock.cs        // XML serileştirme modeli
|   |-- User.cs
|   `-- AuthDtos.cs
|-- Services/           // İş mantığı
|   |-- IAuthService.cs
|   |-- IStockDataService.cs
|   `-- IUserRepository.cs
|-- Transforms/         // XSLT dosyaları
|   `-- StocksToHtml.xslt
|-- stockdata.xsd       // XML Şema Tanımı
|-- stockdata.dtd       // Belge Türü Tanımı
|-- users.json          // Kullanıcı veritabanı
|-- Program.cs          // Uygulama başlangıç ve konfigürasyon
`-- appsettings.json    // Yapılandırma
```

## 🚀 Başlarken

Bu projeyi yerel makinenizde çalıştırmak için aşağıdaki adımları izleyin.

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) veya üstü
- Bir kod düzenleyici (örn: [Visual Studio Code](https://code.visualstudio.com/))
- [VS Code Live Server Eklentisi](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer) (Frontend'i çalıştırmak için)
- [Thunder Client](https://www.thunderclient.com/) / [Postman](https://www.postman.com/) (API endpoint'lerini test etmek için)

### Kurulum ve Çalıştırma

1.  **Projeyi Klonlayın:**
    ```bash
    git clone https://github.com/YOUR_USERNAME/StockMarketXmlProject.git
    cd StockMarketXmlProject
    ```

2.  **Backend (API) Ayarları:**
    a. `StockMarket.Api` dizinine gidin.
    ```bash
    cd StockMarket.Api
    ```
    b. **Finnhub API Anahtarını Yapılandırın:**
       [Finnhub.io](https://finnhub.io/)'dan ücretsiz bir API anahtarı alın.
       `appsettings.json` dosyasını açın ve `Finnhub:ApiKey` değerini kendi anahtarınızla değiştirin.
       ```json
       "Finnhub": {
         "ApiKey": "YOUR_FINNHUB_API_KEY_HERE"
       }
       ```
    c. **Bağımlılıkları Yükleyin ve Çalıştırın:**
       ```bash
       dotnet restore
       dotnet run
       ```
    d. API şimdi `http://localhost:5190` (veya terminalde belirtilen başka bir port) üzerinden çalışıyor olmalıdır. API dokümantasyonunu ve test arayüzünü görmek için `http://localhost:5190/swagger` adresine gidin.

3.  **Frontend (Client) Ayarları:**
    a. Projenin ana dizininden `StockMarket.Client` klasörünü VS Code ile açın.
    b. `index.html` dosyasına sağ tıklayın ve **"Open with Live Server"** seçeneğini seçin.
    c. Frontend uygulaması tarayıcınızda `http://127.0.0.1:5500` (veya benzeri bir port) adresinde açılacaktır.

4.  **Testleri Çalıştırma:**
    Backend'in birim ve entegrasyon testlerini çalıştırmak için `StockMarket.Api` dizininde aşağıdaki komutu kullanın:
    ```bash
    dotnet test
    ```

## 🔌 API Endpoint'leri

API'nin ana endpoint'leri aşağıda listelenmiştir. `[Auth]` ile işaretlenenler geçerli bir Bearer Token gerektirir.

| Endpoint | Metot | Açıklama | Yetki | Veri Formatı |
| :--- | :--- | :--- | :--- | :--- |
| `/api/v1/auth/register` | `POST` | Yeni bir kullanıcı kaydeder. | Herkese Açık | JSON |
| `/api/v1/auth/login` | `POST` | Kullanıcı girişi yapar ve JWT döndürür. | Herkese Açık | JSON |
| `/api/v1/stocks` | `GET` | Takip edilen tüm hisselerin listesini XML olarak döndürür. | `[Auth]` | XML |
| `/api/v1/stocks/{ticker}` | `GET` | Belirtilen tek bir hissenin verisini XML olarak döndürür. | `[Auth]` | XML |
| `/api/v1/stocks/report/html` | `GET` | Tüm hisse verilerini XSLT ile HTML raporuna dönüştürür. | Herkese Açık | HTML |
| `/api/v1/stocks/query-with-xdocument` | `GET` | LINQ to XML ile hisseleri fiyata göre filtreler (`?minPrice=...`).| `[Auth]` | XML |
| `/api/v1/validation/xsd-check-all-stocks` | `GET` | API'nin XML çıktısını XSD şemasına göre doğrular. | Herkese Açık | JSON |
| `/api/v1/validation/dtd-check-all-stocks` | `GET` | API'nin XML çıktısını DTD şemasına göre doğrular. | Herkese Açık | JSON |
| `/api/v1/user/preferences` | `GET` | Giriş yapmış kullanıcının tüm tercihlerini getirir. | `[Auth]` | JSON |
| `/api/v1/user/preferences/{ticker}` | `PUT` | Bir hisse için kullanıcı tercihi oluşturur veya günceller. | `[Auth]` | JSON |
| `/api/v1/user/preferences/{ticker}` | `DELETE`| Bir hisse için kullanıcı tercihini siler. | `[Auth]` | JSON |
| `/api/v1/stocks/track` | `POST` | (`Admin` rolü) Takip edilecek yeni bir hisse sembolü ekler. | `[Admin]`| JSON |
| `/api/v1/stocks/track/{ticker}` | `DELETE`| (`Admin` rolü) Bir hisse sembolünü takipten çıkarır. | `[Admin]`| JSON |

## 🏛️ XML'in Kalbi: Yapı ve Doğrulama

Bu projenin ayırt edici özelliği XML'i birincil veri taşıma formatı olarak kullanmasıdır.

### Örnek XML Çıktısı

```xml
<?xml version="1.0" encoding="utf-16"?>
<StockDataFeed xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns="http://www.example.com/stockdata/v1">
  <Stock TickerSymbol="AAPL">
    <CompanyName>Apple Inc</CompanyName>
    <CurrentPrice>201.08</CurrentPrice>
    <Change>0.08</Change>
    <PercentChange>0.000398</PercentChange>
    <Volume>0</Volume>
    <LastUpdated>2025-06-27T20:00:00Z</LastUpdated>
    <Description><![CDATA[AAPL için Finnhub'dan alınan borsa verisi.]]></Description>
  </Stock>
  <!-- Diğer <Stock> elementleri... -->
</StockDataFeed>
```

### XSD Şeması

Bu yapı, aşağıdaki gibi bir XSD şeması ile sıkı bir şekilde doğrulanır. Bu şema, veri türlerini, zorunlu alanları, eleman sırasını ve özel format kurallarını (örn: `TickerSymbol` için regex) uygular.

```xml
<!-- stockdata.xsd -->
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema"
           targetNamespace="http://www.example.com/stockdata/v1"
           xmlns:tns="http://www.example.com/stockdata/v1"
           elementFormDefault="qualified">

  <xs:element name="StockDataFeed">
    <xs:complexType>
      <xs:sequence>
        <xs:element name="Stock" type="tns:StockType" minOccurs="0" maxOccurs="unbounded"/>
      </xs:sequence>
    </xs:complexType>
  </xs:element>

  <xs:complexType name="StockType">
    <xs:sequence>
      <xs:element name="CompanyName" type="xs:string"/>
      <xs:element name="CurrentPrice" type="xs:decimal"/>
      <xs:element name="Change" type="xs:decimal"/>
      <!-- ...diğer elementler... -->
    </xs:sequence>
    <xs:attribute name="TickerSymbol" type="tns:TickerSymbolType" use="required"/>
  </xs:complexType>

  <xs:simpleType name="TickerSymbolType">
    <xs:restriction base="xs:string">
      <xs:pattern value="[A-Z]{1,5}"/>
    </xs:restriction>
  </xs:simpleType>
</xs:schema>
```

## 📈 Gelecek Planları ve Geliştirmeler

Bu proje, üzerine inşa edilebilecek sağlam bir temel sunmaktadır. Potansiyel geliştirmeler şunları içerir:

-   [ ] **Kalıcı Veritabanı**: In-memory depolamayı PostgreSQL veya SQL Server gibi bir veritabanı ve Entity Framework Core ile değiştirmek.
-   [ ] **Gerçek Zamanlı Güncellemeler**: SignalR kullanarak sunucudan istemciye anlık veri akışı (push) sağlamak.
-   [ ] **Gelişmiş Önbellekleme**: Finnhub API'sine yapılan çağrıları azaltmak ve yanıt sürelerini iyileştirmek için Redis gibi bir önbellekleme katmanı eklemek.
-   [ ] **Docker ile Konteynerleştirme**: Dağıtımı basitleştirmek ve taşınabilirliği artırmak için backend API'sini bir Docker konteynerine almak.
-   [ ] **CI/CD Pipeline**: GitHub Actions ile otomatik derleme, test ve dağıtım süreçleri oluşturmak.

## 📄 Lisans

Bu proje MIT Lisansı altında lisanslanmıştır. Detaylar için `LICENSE` dosyasına bakınız.
