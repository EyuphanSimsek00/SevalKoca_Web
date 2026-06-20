
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

        // Ana Sayfa (Index.cshtml) Yüklendiğinde Çalışacak Metot (ARAMA DESTEKLİ)
       // Ana Sayfa (Index.cshtml) Yüklendiğinde Çalışacak Metot
        public IActionResult Index()
        {
            List<Urun> urunler = new List<Urun>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // Filtresiz, saf SQL sorgusu: Sadece tüm ürünleri getir
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

        // Kategori Sayfasına Yönlendirme Metodu
        public IActionResult Kategori()
        {
            // Bu sayfada göstereceğimiz ürünleri tutacak boş liste
            List<Urun> kategoriUrunleri = new List<Urun>();

            using (SqlConnection con = new SqlConnection(_connectionString))
            {
                // DİKKAT: Sadece "Üst Giyim" (KategoriID = 1) olanları çekiyoruz!
                string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL FROM Urunler WHERE KategoriID = 1";
                
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

                            kategoriUrunleri.Add(u);
                        }
                    }
                }
            }

            // Doldurduğumuz listeyi Kategori sayfasına fırlatıyoruz
            return View(kategoriUrunleri);
        }

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
                string query = "SELECT UrunID, UrunAdi, Fiyat, GorselURL FROM Urunler WHERE UrunID = @gelenId";
                
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

        // --- YENİ EKLENEN ADMİN KODLARI BAŞLANGICI ---

        // ADMIN: Ürün Ekleme Sayfasını Açan Metot (GET)
        public IActionResult UrunEkle()
        {
            // 🔒 KORUMA KALKANI
            if (HttpContext.Session.GetString("GirisYapildi") != "Evet")
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        // ADMIN: Formdan Gelen Verileri SQL'e Kaydeden Metot (POST)
        [HttpPost]
        public IActionResult UrunEkle(Urun yeniUrun)
        {
            // 🔒 KORUMA KALKANI
            if (HttpContext.Session.GetString("GirisYapildi") != "Evet")
            {
                return RedirectToAction("Login");
            }

            // Basit bir doğrulama: Model boş değilse işlemleri yap
            if (yeniUrun != null)
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    // SQL Ekleme Sorgumuz
                    // "EkleneMeTarihi" kelimesindeki fazla "e" harfini kaldırdık -> "EklenmeTarihi" oldu
                    string query = "INSERT INTO Urunler (KategoriID, UrunAdi, Fiyat, GorselURL, StokMiktari, EklenmeTarihi) " +
                                   "VALUES (1, @urunAdi, @fiyat, @gorselUrl, 15, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        // Formdan gelen dinamik verileri sorgumuza güvenli bir şekilde bağlıyoruz
                        cmd.Parameters.AddWithValue("@urunAdi", yeniUrun.UrunAdi);
                        cmd.Parameters.AddWithValue("@fiyat", yeniUrun.Fiyat);
                        cmd.Parameters.AddWithValue("@gorselUrl", yeniUrun.GorselURL ?? "assets/ceket.jpg");

                        con.Open();
                        cmd.ExecuteNonQuery(); // Sorguyu çalıştır ve SQL'e satırı ekle
                    }
                }
            }

            // Ürün başarıyla eklendikten sonra kullanıcıyı otomatik olarak Ana Sayfaya (Index) fırlat
            return RedirectToAction("Index");
        }
        
        // --- YENİ EKLENEN ADMİN KODLARI BİTİŞİ ---
        
        // ADMIN: Tüm Ürünleri Tablo Halinde Listeleyen Yönetim Sayfası
        public IActionResult UrunYonetimi()
        {
            // 🔒 KORUMA KALKANI
            if (HttpContext.Session.GetString("GirisYapildi") != "Evet")
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
            // 🔒 KORUMA KALKANI
            if (HttpContext.Session.GetString("GirisYapildi") != "Evet")
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
        //  OTURUM YÖNETİMİ METOTLARI (LOGIN / LOGOUT)
        // ==========================================

        // LOGIN: Giriş Sayfasını Açan Metot (GET)
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN: Klasik C# Sorgusu ile Giriş Kontrolü (POST)
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == "admin" && password == "1234")
            {
                // Bilgiler doğruysa hafızaya oturumun açıldığını kaydediyoruz
                HttpContext.Session.SetString("GirisYapildi", "Evet");
                
                // Başarılı girişte doğrudan korumalı panele yönlendir
                return RedirectToAction("UrunYonetimi");
            }

            // Hatalıysa ekranda gösterilecek mesajı yükle
            ViewBag.HataMesaji = "Kullanıcı adı veya şifre hatalı!";
            return View();
        }

        // LOGOUT: Oturumu Tamamen Temizleme ve Çıkış Yapma
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Hafızadaki oturum verilerini sıfırla
            return RedirectToAction("Index"); // Ana sayfaya güvenle geri gönder
        }
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
    }
}

