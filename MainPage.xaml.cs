
using Gelir_Gider_uygulamasi;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Formats.Tar;
using System.Linq;
//Verileri filtrelemek(tarihe göre süzmek gibi) için 
using System.Text.Json;
//Verileri telefonun hafızasına kaydederken listeyi metne (JSON) çevirmek için

namespace Gelir_Gider_uygulamasi
{
    public class IslemKaydi
    {   
        public string Ad { get; set; }
        public double Tutar { get; set; }
        public System.DateTime Tarih { get; set; }
        public string Tur { get; set; }
        public string GorunumMetni => $"{Tarih:dd.MM.yyyy} - {Ad}: {Tutar} TL";
    }
    public partial class MainPage : ContentPage
    {
        
        List<IslemKaydi> TumIslemler = new List<IslemKaydi>(); //tüm islemler tüm kayıtları tututor
        double toplam = 0;
        bool islemYapiliyor = false;

        ObservableCollection<IslemKaydi> GelirlerListesi = new ();
        ObservableCollection<IslemKaydi> GiderlerListesi = new ();
        // ObservableCollection = Ekranda listelenen ve değişiklik olduğunda ekranı anında güncelleyen canlı liste
        public MainPage()
        {
            islemYapiliyor = true;
            InitializeComponent(); //XAML arayüzünü kod ile birleştirir.
            
            GelirListesi.ItemsSource = GelirlerListesi;
            GiderListesi.ItemsSource = GiderlerListesi;
            //itemssource = Arka plandaki listeyi ekrandaki listeyen(CollectionView)
            FiltreSecim.SelectedIndex = 0;
            islemYapiliyor = false; // Her şey hazır, kilidi aç
            Filtrele(null, null);
            VerileriYukle();
        }
        private void VerileriKaydet()
        {
            try // hata oluşursa çökmeyi engeller
            {
                string jsonMetin = JsonSerializer.Serialize(TumIslemler);
                //Listeyi(nesneleri) uzun bir metne dönüştürür.
                Preferences.Default.Set("kayitli_liste", jsonMetin);
                //metni cihazın kalıcı hafızasına "kayitli_liste" adıyla yazar.

            }
            catch //Eğer kayıt sırasında bir sorun çıkarsa (hafıza dolu vb.) buraya atlar
            {
                
            }

        }
        private void VerileriYukle()
        {
            string jsonMetin = Preferences.Default.Get("kayitli_liste", "");
            // Hafızada "kayitli_liste" isminde bir şey varsa getir, yoksa boşluk kalir sade
            if (!string.IsNullOrEmpty(jsonMetin))
            {
                var yuklenenListe = JsonSerializer.Deserialize<List<IslemKaydi>>(jsonMetin);
                // JsonSerializer.Deserialize: Metni (JSON) tekrar IslemKaydi listesine çevirir.
                if (yuklenenListe != null)
                {
                    
                    
                        TumIslemler = yuklenenListe; // Ana listeyi eski verilere eşle.
                        GenelToplamiGuncelle();
                      
                    
                   
                }
            }
        }
        private void OnIslemSecildi(object sender, EventArgs e)
        {
            if (tursecim.SelectedItem == null) return;
            string secim = tursecim.SelectedItem.ToString();

            Gelirler.IsVisible = (secim == "Gelir");
            Giderler.IsVisible = (secim == "Gider");

            GelirListesi.IsVisible = true;
            GiderListesi.IsVisible = true;
        }
        private void OnEkleClicked(object sender, EventArgs e)
        {
            if (tursecim.SelectedItem == null)
                return;
            string secim = tursecim.SelectedItem.ToString();
            IslemKaydi yeniIslem = new IslemKaydi { Tarih = DateTime.Now, Tur = secim };
            if (secim == "Gelir")
            {
                if (double.TryParse(Gelirtutar.Text, out double miktar))
                //Metin kutusundaki yazıyı ondalıklı sayıya çevirmeyi dene. Başarırsan değeri miktar içine koy
                {
                    yeniIslem.Ad = Gelirad.Text;
                    yeniIslem.Tutar = miktar;
                    yeniIslem.Tarih = (System.DateTime)GelirTarih.Date;
                    
                }
            }
            else
            {
                if (double.TryParse(Gidertutar.Text, out double miktar))
                {
                    yeniIslem.Ad = Giderad.Text;
                    yeniIslem.Tutar = miktar;
                    yeniIslem.Tarih = (System.DateTime)GiderTarih.Date;
                    
                }
            }

             TumIslemler.Add(yeniIslem);
            VerileriKaydet();
            GenelToplamiGuncelle();
            Filtrele(null, null); // Listeyi güncelle
            Temizle();
            
        }
        private async void OnSilClicked(object sender, EventArgs e)
        {

            bool onay = await DisplayAlert("Silme Onayı",
                                           "Bu işlemi silmek istediğinize emin misiniz?",
                                           "Evet",
                                           "Hayır");
            if (onay)
            {
                var buton = (Button)sender;
                var silinecekIslem = (IslemKaydi)buton.CommandParameter;
                if (silinecekIslem != null)
                {
                    // 2. Ana listeden (TumIslemler) sil
                    TumIslemler.Remove(silinecekIslem);

                    // 3. Toplam bakiyeyi güncelle (Gelirse düş, giderse ekle)
                   

                    // 5. Değişiklikleri kalıcı hafızaya kaydet (Uygulama açıldığında geri gelmesin)
                    VerileriKaydet();

                    // 6. Ekrandaki listeleri yenilemek için Filtrele metodunu tekrar çağır
                    GenelToplamiGuncelle();
                    Filtrele(null, null);
                }
            }
        }
        private void Temizle()
        {
            Gelirad.Text = Gelirtutar.Text = Giderad.Text = Gidertutar.Text = "";
            GelirTarih.Date = DateTime.Today;
            GiderTarih.Date = DateTime.Today;
        }


        private void Filtrele(object sender, EventArgs e)
        {
            if (FiltreSecim == null || ToplamLabel == null || FiltreSecim.SelectedItem == null) return;

            string secim = FiltreSecim.SelectedItem.ToString();
            DateTime bugun = DateTime.Today;

            // Filtreleme kriterini belirle
            var sonuclar = TumIslemler.Where(x => //Ana listedeki her bir x elemanını tek tek kontrol eder.
                secim == "Hepsi" ||
                (secim == "Günlük" && x.Tarih.Date == bugun) ||
                (secim == "Haftalık" && x.Tarih >= bugun.AddDays(-7)) ||
                (secim == "Aylık" && x.Tarih.Month == bugun.Month && x.Tarih.Year == bugun.Year)
            ).ToList();

            // Ekrandaki listeleri temizle ve yeni sonuçları ekle
            
            GelirlerListesi.Clear();
            GiderlerListesi.Clear();
            double filtrelitoplam = 0;

            foreach (var item in sonuclar)
            {
                if (item.Tur == "Gelir")
                {
                    GelirlerListesi.Add(item);
                    filtrelitoplam += item.Tutar;
                }
                else
                {
                    GiderlerListesi.Add(item);
                    filtrelitoplam -= item.Tutar; 
                }
                
            }
            ToplamLabel.Text = $"{secim} Toplam: {filtrelitoplam} TL";
        }
            // Gelir Adı yazılıp Enter'a basılınca Tutar kutusuna geç
        private void OnGelirAdCompleted(object sender, EventArgs e)
        {
            Gelirtutar.Focus(); //İmleci (fokus)yazılab metin kutusuna veya kontrole atar
                                //Böylece kullanıcı sürekli seçmek zorunda kalmaz, Enter tuşuyla ilerler.
        }

        // Gider Adı yazılıp Enter'a basılınca Tutar kutusuna geç
        private void OnGiderAdCompleted(object sender, EventArgs e)
        {
            Gidertutar.Focus();
        }
        private void OnGelirTutarCompleted(object sender, EventArgs e)
        {
            GelirTarih.Focus();
        }

        private void OnGiderTutarCompleted(object sender, EventArgs e)
        {
            GiderTarih.Focus();
        }

        
        
        private void GenelToplamiGuncelle()
        {
            // Listeyi baştan sona tarayıp net bakiyeyi hesaplar
            toplam = TumIslemler.Sum(x => x.Tur == "Gelir" ? x.Tutar : -x.Tutar);

            // Eğer ToplamLabel'da her zaman filtreli değil genel bakiyeyi görmek istersen:
            // ToplamLabel.Text = $"Toplam: {toplam} TL";
        }
    }
}

