# PulseChat API - Postman Entegrasyon Kılavuzu 🚀

Bu kılavuz, **PulseChat Backend REST API** servislerini Postman üzerinde test edebilmeniz için hazırlanan koleksiyonu ve otomatik ortam yapılandırmasını içerir.

---

## 📦 Hazır Dosyalar

Proje ana dizininde 2 adet hazır dosya bulunmaktadır:
1. `PulseChat.postman_collection.json`: Tüm HTTP isteklerini, gövdelerini, başlıklarını ve otomatik test betiklerini içeren koleksiyon.
2. `PulseChat.postman_environment.json`: `baseUrl`, `accessToken`, `refreshToken`, `userId` gibi dinamik ortam değişkenlerini tutan Postman ortamı.

---

## ⚡ 1 Dakikada Kurulum ve İçe Aktarma (Import)

1. **Postman** uygulamasını açın.
2. Sol üstteki **`Import`** butonuna tıklayın.
3. Proje ana dizinindeki şu iki dosyayı sürükleyip bırakın:
   - `PulseChat.postman_collection.json`
   - `PulseChat.postman_environment.json`
4. Sağ üstteki Environment (Ortam) açılır menüsünden **`PulseChat Local Environment`** seçeneğini aktif edin.

---

## 🔑 Otomatik Token Yönetimi (Auto-Token Propagation)

Bu koleksiyon, **el ile token kopyala-yapıştır yapma zahmetini tamamen ortadan kaldırır**:

- **Kayıt Ol (`Register`)** veya **Giriş Yap (`Login`)** isteklerinden herhangi birini çalıştırdığınızda; dönen yanıttaki JWT `accessToken` ve `refreshToken` değerleri Postman Test Script'i tarafından otomatik olarak okunur ve `{{accessToken}}` ortam değişkenine yazılır.
- Koleksiyonun tepesinde ortak yetkilendirme (`Inherit auth from parent - Bearer Token`) tanımlandığı için; sonraki tüm istekler (`Create Room`, `Get Rooms`, `Friendships`, `Media Upload`) **otomatik olarak bu token ile yetkilendirilir.**

```javascript
// Register ve Login isteklerinin 'Tests' sekmesinde çalışan otomatik script:
var jsonData = pm.response.json();
if (jsonData.isSuccess && jsonData.data) {
    pm.environment.set("accessToken", jsonData.data.accessToken);
    pm.environment.set("refreshToken", jsonData.data.refreshToken);
    pm.environment.set("userId", jsonData.data.user.id);
}
```

---

## 📂 İstek Listesi ve Modüller

### 1. Auth Modülü
- `POST /api/auth/register` (Kayıt ol ve otomatik token al)
- `POST /api/auth/login` (Giriş yap ve token yenile)
- `POST /api/auth/refresh` (Refresh token rotation ile access token'ı tazele)
- `GET /api/auth/me` (Giriş yapmış kullanıcının profil detayları)

### 2. Chat Modülü
- `POST /api/chat/rooms` (Yeni grup veya ikili oda oluşturur - odayı kuran otomatik Admin olur)
- `GET /api/chat/rooms` (Kullanıcının dahil olduğu odaları listeler)
- `GET /api/chat/rooms/{{roomId}}/messages?limit=50` (Cursor-based mesaj geçmişini çeker)

### 3. Friendship Modülü
- `POST /api/friendship/request` (Hedef kullanıcı adına arkadaşlık isteği gönderir)
- `POST /api/friendship/respond` (Gelen isteği onaylar veya siler)
- `GET /api/friendship` (Onaylanmış arkadaş listesini döner)

### 4. Media Modülü
- `POST /api/media/upload` (MinIO S3 kovanına avatar veya sohbet eki yükler - max 25MB)

---

## 🛠️ Alternatif: VS Code / Visual Studio İçinde Test Etmek (.http)

Postman açmadan doğrudan IDE içinden test etmek isterseniz; `src/PulseChat.API/PulseChat.API.http` dosyası tüm bu isteklerle hazır durumdadır. İlgili isteğin üzerindeki **`Send Request`** butonuna basmanız yeterlidir.
