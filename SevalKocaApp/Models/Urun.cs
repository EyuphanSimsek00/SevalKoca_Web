namespace SevalKocaApp.Models
{
    public class Urun
    {
        public int UrunID { get; set; }
        public int KategoriID { get; set; }
        public string UrunAdi { get; set; }
        public decimal Fiyat { get; set; }
        public string GorselURL { get; set; }
        public string Aciklama { get; set; }
        public int StokMiktari { get; set; }
    }
}