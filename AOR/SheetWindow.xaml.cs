using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using AOR.ModelView;

namespace AOR
{
    public partial class SheetWindow : Window
    {
        public SheetWindow()
        {
            InitializeComponent();
            DataContext = Bindings.GetInstance();
        }

        public async Task<List<BitmapImage>> GetPdfPages(string path)
        {
            List<BitmapImage> output = new List<BitmapImage>();
            StorageFile storagePdfFile = await StorageFile.GetFileFromPathAsync(path);
            PdfDocument pdfDocument = await PdfDocument.LoadFromFileAsync(storagePdfFile);
            for (uint i = 0; i < pdfDocument.PageCount; i++)
            {
                using (PdfPage page = pdfDocument.GetPage(i))
                {
                    using (InMemoryRandomAccessStream memStream = new InMemoryRandomAccessStream())
                    {
                        await page.RenderToStreamAsync(memStream);
                        var bi = new BitmapImage(); 
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = memStream.AsStream();
                        bi.EndInit();
                        output.Add(bi);
                    }
                }
            }
            return output;
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            string path = Path.GetFullPath("..\\..\\Data\\Sprawozdanie2.pdf");
            List<BitmapImage> pages = await GetPdfPages(path);
            Bindings.GetInstance().CurrentSheet = pages[1];
        }
    }
}