# PulseChat Yük Testi (Load Testing) Kılavuzu

Bu dizin, **PulseChat** sisteminin yüksek eşzamanlılık (1.000 – 2.000 Concurrent Users) ve düşük gecikmeli SignalR / WebSocket mesajlaşma performansını ölçmek için hazırlanmış **Apache JMeter** test altyapısını içerir.

---

## 📁 Dizin Yapısı

- **`jmeter/apache-jmeter-5.6.3/`**: Hazır kurulu Apache JMeter ve WebSocket Sampler eklentileri.
- **`pulsechat-loadtest.jmx`**: REST (Auth & Oda) ve SignalR WebSocket el sıkışma / canlı mesajlaşma test senaryosu.
- **`run-loadtest.ps1`**: Testi CLI üzerinden koşturup tarayıcınızda otomatik **HTML Dashboard Raporu** açan PowerShell betiği.
- **`start-jmeter-gui.bat`**: JMeter grafiksel arayüzünü (GUI) hazır senaryo yüklü olarak başlatan çift-tıklama betiği.
- **`setup-jmeter.ps1`**: Kurulumu otomatikleştiren yapılandırma script'i.

---

## 🚀 Testi Çalıştırmadan Önce (Gereksinimler)

Testin başarılı olması için arka plan servislerinin ve API'nin ayakta olması gerekir:

1. **Docker Servislerini Başlatın:**
   ```powershell
   cd c:\Projeler\PulseChat\PulseChat.API
   docker compose up -d
   ```
   *(PostgreSQL, MongoDB, Redis, RabbitMQ ve MinIO ayağa kalkacaktır).*

2. **Backend API'yi Başlatın:**
   ```powershell
   cd c:\Projeler\PulseChat\PulseChat.API
   dotnet run --project src/PulseChat.API
   ```
   *(API `http://localhost:5000` adresinde çalışacaktır).*

---

## 📊 Testin Koşulması (2 Farklı Yöntem)

### Yöntem 1: Otomatik CLI & HTML Raporu (Önerilen)
PowerShell terminalinde doğrudan aşağıdaki komutu çalıştırabilirsiniz:

```powershell
# 50 sanal kullanıcı, 5 saniye ramp-up, kullanıcı başına 5 mesaj:
.\loadtests\run-loadtest.ps1 -Users 50 -RampUp 5 -Messages 5

# Daha yüksek yük (Örnek: 200 Kullanıcı, 10 saniye ramp-up):
.\loadtests\run-loadtest.ps1 -Users 200 -RampUp 10 -Messages 10
```

> **Not:** Test bittiğinde tarayıcınızda otomatik olarak grafiksel **HTML Dashboard Raporu** (`reports/report-xxx/index.html`) açılır. Buradan Throughput, Latency (Gecikme), Yanıt Süreleri ve Hata Oranlarını detaylıca inceleyebilirsiniz.

---

### Yöntem 2: Görsel Arayüz (GUI Modu)
Senaryoyu görsel olarak incelemek, request/response detaylarını izlemek veya elle çalıştırmak için:
- `loadtests/start-jmeter-gui.bat` dosyasına çift tıklayın.
- JMeter açıldığında sol ağaçta senaryo adımlarını görebilir ve üstteki yeşil **Başlat (Play)** butonuna basarak testi başlatabilirsiniz.

---

## 🛠️ Test Senaryosunun İçeriği (`pulsechat-loadtest.jmx`)

1. **PreProcessor (`rs` Tanımı):** SignalR protokolünün paket sonlandırıcı ASCII `0x1E` ayracı otomatik tanımlanır.
2. **REST: Register / Fallback Login:** Sanal kullanıcılar için sisteme kayıt olunur ve Bearer JWT token alınır.
3. **REST: Get or Create Room:** Sanal kullanıcının bir sohbet odası alması/oluşturması sağlanır.
4. **SignalR: Negotiate:** `/hubs/chat/negotiate` çağrısıyla WebSocket `connectionToken` elde edilir.
5. **WebSocket: Open Connection:** `ws://localhost:5000/hubs/chat` bağlantısı açılır.
6. **SignalR: Handshake:** `{"protocol":"json","version":1}\u001e` el sıkışması tamamlanır.
7. **SignalR: JoinRoom:** Sohbet odasına SignalR grubu üzerinden abone olunur.
8. **SignalR: SendMessage Döngüsü:** Belirlenen sayıda gerçek mesaj fırlatılır; sunucunun ACK süresi ve gecikmesi (RTT) anlık ölçülür.
9. **WebSocket: Close:** Test sonunda soket düzgünce kapatılır.
