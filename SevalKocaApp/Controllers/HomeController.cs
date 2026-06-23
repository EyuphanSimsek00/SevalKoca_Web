
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SevalKocaApp.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http; // Session işlemleri için gerekli kütüphane

namespace SevalKocaApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _connectionString;

        // appsettings.json dosyasından veritabanı yolumuzu çekiyoruz
        public HomeController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Ana Sayfa (Index.cshtml) Yüklendiğinde Çalışacak Metot
        public IActionResult Index()
        {
            List<Urun> urunler = new List<Urun>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL FROM Urunler";
                
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Urun u = new Urun();
                            u.UrunID = Convert.ToInt32(reader["UrunID"]);
                            u.UrunAdi = reader["UrunAdi"].ToString();
                            u.Fiyat = Convert.ToDecimal(reader["Fiyat"]);
                            u.GorselURL = reader["GorselURL"].ToString();
                            urunler.Add(u);
                        }
                    }
                }
            }
            return View(urunler);
        }

        // ==========================================
        //  ARAMA METODU
        // ==========================================

        // ARAMA: Navbar'daki arama ikonundan gelen arama sorgusunu işler
        public IActionResult Ara(string q)
        {
            List<Urun> sonuclar = new List<Urun>();

            // Arama terimi boş değilse sorguyu çalıştır
            if (!string.IsNullOrWhiteSpace(q))
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    // LIKE '%aranan%' mantığıyla ürün adına göre arama
                    string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL FROM Urunler WHERE UrunAdi LIKE @aranan";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@aranan", "%" + q + "%");
                        con.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Urun u = new Urun();
                                u.UrunID = Convert.ToInt32(reader["UrunID"]);
                                u.UrunAdi = reader["UrunAdi"].ToString();
                                u.Fiyat = Convert.ToDecimal(reader["Fiyat"]);
                                u.GorselURL = reader["GorselURL"].ToString();
                                sonuclar.Add(u);
                            }
                        }
                    }
                }
            }

            // Aranan kelimeyi View'da gösterebilmek için ViewBag'e yazıyoruz
            ViewBag.AramaTerimi = q;
            return View(sonuclar);
        }

        // ==========================================
        //  KATEGORİ SAYFASI (DİNAMİK FİLTRELEME + SIRALAMA)
        // ==========================================

        // Kategori Sayfasına Yönlendirme Metodu (Dinamik KategoriID + Sıralama desteği)
        public IActionResult Kategori(int? id, string siralama)
        {
            List<Urun> kategoriUrunleri = new List<Urun>();
            string kategoriAdi = "TÜM ÜRÜNLER";

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // 1. Eğer kategori ID geliyorsa, o kategorinin adını çek
                if (id.HasValue)
                {
                    string kategoriQuery = "SELECT KategoriAdi FROM Kategoriler WHERE KategoriID = @katId";
                    using (SqlCommand katCmd = new SqlCommand(kategoriQuery, con))
                    {
                        katCmd.Parameters.AddWithValue("@katId", id.Value);
                        con.Open();
                        object sonuc = katCmd.ExecuteScalar();
                        if (sonuc != null)
                        {
                            kategoriAdi = sonuc.ToString().ToUpper();
                        }
                        con.Close();
                    }
                }

                // 2. Ürünleri çek (filtreli veya filtresiz)
                string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL FROM Urunler";

                if (id.HasValue)
                {
                    query += " WHERE KategoriID = @katId";
                }

                // Sıralama parametresine göre ORDER BY ekle
                switch (siralama)
                {
                    case "fiyat_artan":
                        query += " ORDER BY Fiyat ASC";
                        break;
                    case "fiyat_azalan":
                        query += " ORDER BY Fiyat DESC";
                        break;
                    case "isim_az":
                        query += " ORDER BY UrunAdi ASC";
                        break;
                    default:
                        query += " ORDER BY UrunID DESC"; // Varsayılan: en yeni önce
                        break;
                }

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (id.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@katId", id.Value);
                    }

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Urun u = new Urun();
                            u.UrunID = Convert.ToInt32(reader["UrunID"]);
                            u.UrunAdi = reader["UrunAdi"].ToString();
                            u.Fiyat = Convert.ToDecimal(reader["Fiyat"]);
                            u.GorselURL = reader["GorselURL"].ToString();
                            kategoriUrunleri.Add(u);
                        }
                    }
                }
            }

            // View'a gönderilecek bilgiler
            ViewBag.KategoriAdi = kategoriAdi;
            ViewBag.SeciliKategoriID = id;
            ViewBag.SeciliSiralama = siralama;
            return View(kategoriUrunleri);
        }

        // ==========================================
        //  ÜRÜN DETAY SAYFASI (DİNAMİK AÇIKLAMA DESTEKLİ)
        // ==========================================

        // Ürün Detay Sayfasına Yönlendirme Metodu
        public IActionResult UrunDetay(int id)
        {
            // 1. GÜVENLİK: Eğer linkte ID numarası yoksa, direkt ana sayfaya yolla.
            if (id == 0) 
            {
                return RedirectToAction("Index"); 
            }

            Urun secilenUrun = null; // Başlangıçta boş bırakıyoruz.

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Aciklama sütunu da sorguya eklendi (dinamik açıklama için)
                string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL, Aciklama FROM Urunler WHERE UrunID = @gelenId";
                
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@gelenId", id);
                    
                    con.Open();
                    
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read()) // Veritabanında eşleşen ürün bulunduysa:
                        {
                            secilenUrun = new Urun();
                            secilenUrun.UrunID = Convert.ToInt32(reader["UrunID"]);
                            secilenUrun.UrunAdi = reader["UrunAdi"].ToString();
                            secilenUrun.Fiyat = Convert.ToDecimal(reader["Fiyat"]);
                            secilenUrun.GorselURL = reader["GorselURL"].ToString();
                            secilenUrun.Aciklama = reader["Aciklama"]?.ToString();
                        }
                    }
                }
            }

            // 2. GÜVENLİK: Veritabanında bu ID'ye sahip bir ürün yoksa, yine sayfayı çökertme, ana sayfaya yolla.
            if (secilenUrun == null)
            {
                return RedirectToAction("Index");
            }

            // Her şey yolundaysa, bulduğun ürünü HTML'e (View'a) gönder.
            return View(secilenUrun);
        }

        // ==========================================
        //  ADMİN: ÜRÜN EKLEME (KATEGORİ DROPDOWN DESTEKLİ)
        // ==========================================

        // ADMIN: Ürün Ekleme Sayfasını Açan Metot (GET)
        public IActionResult UrunEkle()
        {
            // 🔒 KORUMA KALKANI: Sadece Admin rolündeki kullanıcı erişebilir
            if (HttpContext.Session.GetString("KullaniciRol") != "Admin")
            {
                return RedirectToAction("Login");
            }

            // Kategoriler tablosundan tüm kategorileri çekip ViewBag ile View'a gönderiyoruz
            List<dynamic> kategoriler = new List<dynamic>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT KategoriID, KategoriAdi FROM Kategoriler";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            kategoriler.Add(new
                            {
                                KategoriID = Convert.ToInt32(reader["KategoriID"]),
                                KategoriAdi = reader["KategoriAdi"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Kategoriler = kategoriler;
            return View();
        }

        // ADMIN: Formdan Gelen Verileri SQL'e Kaydeden Metot (POST)
        [HttpPost]
        public IActionResult UrunEkle(Urun yeniUrun)
        {
            // 🔒 KORUMA KALKANI: Sadece Admin rolündeki kullanıcı erişebilir
            if (HttpContext.Session.GetString("KullaniciRol") != "Admin")
            {
                return RedirectToAction("Login");
            }

            // Basit bir doğrulama: Model boş değilse işlemleri yap
            if (yeniUrun != null)
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    // SQL Ekleme Sorgumuz — KategoriID artık formdan geliyor (hardcoded değil)
                    string query = "INSERT INTO Urunler (KategoriID, UrunAdi, Fiyat, GorselURL, StokMiktari, EklenmeTarihi) " +
                                   "VALUES (@kategoriId, @urunAdi, @fiyat, @gorselUrl, 15, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Formdan gelen dinamik verileri sorgumuza güvenli bir şekilde bağlıyoruz
                        cmd.Parameters.AddWithValue("@kategoriId", yeniUrun.KategoriID);
                        cmd.Parameters.AddWithValue("@urunAdi", yeniUrun.UrunAdi);
                        cmd.Parameters.AddWithValue("@fiyat", yeniUrun.Fiyat);
                        cmd.Parameters.AddWithValue("@gorselUrl", yeniUrun.GorselURL ?? "assets/ceket.jpg");

                        con.Open();
                        cmd.ExecuteNonQuery(); // Sorguyu çalıştır ve SQL'e satırı ekle
                    }
                }
            }

            // Ürün başarıyla eklendikten sonra kullanıcıyı otomatik olarak Yönetim Paneli'ne fırlat
            return RedirectToAction("UrunYonetimi");
        }

        // ==========================================
        //  ADMİN: ÜRÜN YÖNETİM PANELİ
        // ==========================================

        // ADMIN: Tüm Ürünleri Tablo Halinde Listeleyen Yönetim Sayfası
        public IActionResult UrunYonetimi()
        {
            // 🔒 KORUMA KALKANI: Sadece Admin rolündeki kullanıcı erişebilir
            if (HttpContext.Session.GetString("KullaniciRol") != "Admin")
            {
                return RedirectToAction("Login");
            }

            List<Urun> urunler = new List<Urun>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL FROM Urunler";
                
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Urun u = new Urun();
                            u.UrunID = Convert.ToInt32(reader["UrunID"]);
                            u.UrunAdi = reader["UrunAdi"].ToString();
                            u.Fiyat = Convert.ToDecimal(reader["Fiyat"]);
                            u.GorselURL = reader["GorselURL"].ToString();
                            urunler.Add(u);
                        }
                    }
                }
            }
            return View(urunler);
        }

        // ADMIN: Seçilen Ürünü Veritabanından Silen Metot
        public IActionResult UrunSil(int id)
        {
            // 🔒 KORUMA KALKANI: Sadece Admin rolündeki kullanıcı erişebilir
            if (HttpContext.Session.GetString("KullaniciRol") != "Admin")
            {
                return RedirectToAction("Login");
            }

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // SQL Delete Sorgusu: Sadece tıklanan ID'ye sahip ürünü sil
                string query = "DELETE FROM Urunler WHERE UrunID = @gelenId";
                
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@gelenId", id);
                    con.Open();
                    cmd.ExecuteNonQuery(); // Sorguyu çalıştır
                }
            }

            // Silme işlemi bittikten sonra sayfayı tazelemek için tekrar listeye geri dön
            return RedirectToAction("UrunYonetimi");
        }

        // ==========================================
        //  OTURUM YÖNETİMİ METOTLARI (LOGIN / KAYIT OL / LOGOUT)
        // ==========================================

        // LOGIN: Giriş Sayfasını Açan Metot (GET)
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN: Veritabanı Tabanlı Giriş Kontrolü (POST)
        [HttpPost]
        public IActionResult Login(string email, string sifre)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Kullanıcılar tablosundan email ve şifre eşleşmesi kontrol ediliyor
                string query = "SELECT KullaniciID, Ad, Soyad, Rol FROM Kullanicilar WHERE Email = @email AND Sifre = @sifre";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@email", email ?? "");
                    cmd.Parameters.AddWithValue("@sifre", sifre ?? "");

                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Bilgiler doğruysa Session'a kullanıcı bilgilerini kaydet
                            HttpContext.Session.SetString("GirisYapildi", "Evet");
                            HttpContext.Session.SetString("KullaniciAd", reader["Ad"].ToString());
                            HttpContext.Session.SetString("KullaniciSoyad", reader["Soyad"].ToString());
                            HttpContext.Session.SetString("KullaniciRol", reader["Rol"].ToString());
                            HttpContext.Session.SetInt32("KullaniciID", Convert.ToInt32(reader["KullaniciID"]));

                            // Rol kontrolü: Admin ise panele, User ise ana sayfaya yönlendir
                            if (reader["Rol"].ToString() == "Admin")
                            {
                                return RedirectToAction("UrunYonetimi");
                            }
                            return RedirectToAction("Index");
                        }
                    }
                }
            }

            // Hatalıysa ekranda gösterilecek mesajı yükle
            ViewBag.HataMesaji = "E-posta veya şifre hatalı!";
            return View();
        }

        // KAYIT OL: Yeni Kullanıcı Kaydı (POST)
        [HttpPost]
        public IActionResult KayitOl(string ad, string soyad, string email, string sifre)
        {
            // Basit doğrulama
            if (string.IsNullOrWhiteSpace(ad) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sifre))
            {
                ViewBag.KayitHata = "Tüm alanları doldurun!";
                ViewBag.AktifSekme = "kayit"; // Kayıt sekmesini açık tut
                return View("Login");
            }

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Önce bu e-posta ile kayıtlı kullanıcı var mı kontrol et
                string kontrolQuery = "SELECT COUNT(*) FROM Kullanicilar WHERE Email = @email";
                using (SqlCommand kontrolCmd = new SqlCommand(kontrolQuery, con))
                {
                    kontrolCmd.Parameters.AddWithValue("@email", email);
                    con.Open();
                    int kayitliMi = (int)kontrolCmd.ExecuteScalar();
                    con.Close();

                    if (kayitliMi > 0)
                    {
                        ViewBag.KayitHata = "Bu e-posta adresi zaten kayıtlı!";
                        ViewBag.AktifSekme = "kayit";
                        return View("Login");
                    }
                }

                // Yeni kullanıcıyı veritabanına ekle (Rol varsayılan olarak "User")
                string query = "INSERT INTO Kullanicilar (Ad, Soyad, Email, Sifre, Rol, KayitTarihi) " +
                               "VALUES (@ad, @soyad, @email, @sifre, 'User', GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@ad", ad);
                    cmd.Parameters.AddWithValue("@soyad", soyad);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@sifre", sifre);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }
            }

            // Kayıt başarılı, giriş sayfasına yönlendir ve başarı mesajı göster
            ViewBag.BasariMesaji = "Kayıt başarılı! Şimdi giriş yapabilirsiniz.";
            return View("Login");
        }

        // LOGOUT: Oturumu Tamamen Temizleme ve Çıkış Yapma
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Hafızadaki oturum verilerini sıfırla
            return RedirectToAction("Index"); // Ana sayfaya güvenle geri gönder
        }

        // ==========================================
        //  SEPET İŞLEMLERİ
        // ==========================================

        // SEPET: Ürün bilgilerini tek bir metin (string) haline getirip hafızaya yazan basit metot
        [HttpPost]
        public IActionResult SepeteEkle(int urunId, string secilenBeden)
        {
            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                string query = "SELECT UrunAdi, Fiyat, GorselURL FROM Urunler WHERE UrunID = @id";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@id", urunId);
                    con.Open();
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // 1. Veritabanından verileri değişkene al
                            string urunAdi = reader["UrunAdi"].ToString();
                            string fiyat = reader["Fiyat"].ToString();
                            string gorsel = reader["GorselURL"].ToString();
                            string beden = string.IsNullOrEmpty(secilenBeden) ? "Standart" : secilenBeden;

                            // 2. Ürün bilgilerini aralarına "|" (dik çizgi) koyarak yapıştır.
                            string yeniUrunMetni = $"{urunAdi}|{fiyat}|{gorsel}|{beden}";

                            // 3. Hafızadaki eski sepet metnini çağır
                            string eskiSepet = HttpContext.Session.GetString("Sepetim");

                            if (string.IsNullOrEmpty(eskiSepet))
                            {
                                // Eğer sepet boşsa, sadece bu ürünü metin olarak kaydet
                                HttpContext.Session.SetString("Sepetim", yeniUrunMetni);
                            }
                            else
                            {
                                // Eğer sepette başka ürünler varsa, eski metnin sonuna "*" (yıldız) koyup yenisini ekle
                                HttpContext.Session.SetString("Sepetim", eskiSepet + "*" + yeniUrunMetni);
                            }
                        }
                    }
                }
            }

            // İşlem bitince kullanıcıyı ürün sayfasına geri yolla
            return RedirectToAction("UrunDetay", new { id = urunId });
        }
        
        // SEPET: Eklenen ürünü sepetten çıkaran basit metot (Sıra numarasına göre çalışır)
        public IActionResult SepettenSil(int id)
        {
            string sepetMetni = HttpContext.Session.GetString("Sepetim");
            
            if (!string.IsNullOrEmpty(sepetMetni))
            {
                // 1. Hafızadaki metni tekrar listeye çevir
                var urunler = sepetMetni.Split('*', StringSplitOptions.RemoveEmptyEntries).ToList();

                // 2. Eğer gönderilen sıra numarası geçerliyse, o sıradaki ürünü listeden at
                if (id >= 0 && id < urunler.Count)
                {
                    urunler.RemoveAt(id);

                    // 3. Kalan ürünleri tekrar yıldızla birleştirip hafızaya kaydet
                    string yeniSepet = string.Join("*", urunler);
                    HttpContext.Session.SetString("Sepetim", yeniSepet);
                }
            }

            // İşlem bitince kullanıcının az önce bulunduğu sayfayı tazele (Refresh efekti)
            return Redirect(Request.Headers["Referer"].ToString());
        }

        // ==========================================
        //  SATIN ALMA SAYFASI (SADECE UI — VERİTABANI KAYDI YOK)
        // ==========================================

        // SATIN AL: Sepet özetini gösterip teslimat formu sunan Checkout sayfası
        public IActionResult SatinAl()
        {
            // Session'daki sepet verisini View'a taşımak için ViewBag kullanıyoruz
            string sepetMetni = HttpContext.Session.GetString("Sepetim") ?? "";
            ViewBag.SepetMetni = sepetMetni;
            return View();
        }
    }
}
