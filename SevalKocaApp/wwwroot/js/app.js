/* --- 1. BEDEN SEÇİMİ (Sadece urun-detay.html sayfasında çalışır) --- */
const sizeBtns = document.querySelectorAll('.size-btn');

if (sizeBtns.length > 0) {
    sizeBtns.forEach(btn => {
        btn.addEventListener('click', function() {
            // Önce tüm butonların rengini sıfırla
            sizeBtns.forEach(b => {
                b.style.borderColor = '#ddd';
                b.style.backgroundColor = 'transparent';
                b.style.color = '#333';
            });
            
            // Sadece tıklanan butonu koyu yap (Aktif durumu)
            this.style.borderColor = '#333';
            this.style.backgroundColor = '#333';
            this.style.color = '#fff';
        });
    });
}

/* --- 2. SEPETE EKLEME VE SEPET SAYACINI ARTIRMA --- */
const addToCartBtn = document.querySelector('.btn-add-to-cart');
const cartCountElements = document.querySelectorAll('.cart-count'); // Header'daki sepet sayısı
let cartTotal = 0;

if (addToCartBtn) {
    addToCartBtn.addEventListener('click', function() {
        // Sepete tıklandığında butona kısa süreli bir efekt verelim
        const originalText = this.innerText;
        this.innerText = "EKLENDİ ✔";
        this.style.backgroundColor = "#4caf50"; // Yeşil renk
        
        // Sayacı artır ve ekrana yazdır
        cartTotal++;
        cartCountElements.forEach(el => el.innerText = cartTotal);

        // 2 saniye sonra butonu eski haline getir
        setTimeout(() => {
            this.innerText = originalText;
            this.style.backgroundColor = "#222";
        }, 2000);
    });
}

/* --- 3. ÜRÜN GALERİSİNDE FOTOĞRAF DEĞİŞTİRME --- */
const mainImage = document.querySelector('.main-image img');
const thumbnails = document.querySelectorAll('.thumbnail-list img');

if (mainImage && thumbnails.length > 0) {
    thumbnails.forEach(thumb => {
        thumb.addEventListener('click', function() {
            // Küçük fotoğrafa tıklandığında, ana fotoğrafın kaynağını (src) değiştir
            const newSrc = this.getAttribute('src');
            mainImage.setAttribute('src', newSrc);

            // Tüm küçük fotoğraflardan "active" sınıfını kaldır, tıklanana ekle
            thumbnails.forEach(t => t.classList.remove('active'));
            this.classList.add('active');
        });
    });
}