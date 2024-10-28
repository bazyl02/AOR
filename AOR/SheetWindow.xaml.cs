using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
            Uri uri = new Uri("F:\\Pulpit/XDDD.png");
            Bindings.GetInstance().CurrentSheet = new BitmapImage(uri);
        }

        public async Task<List<BitmapImage>> GetPdfPages(string path)
        {
            List<BitmapImage> output = new List<BitmapImage>();
            var storagePdfFile = await StorageFile.GetFileFromPathAsync(path);
            PdfDocument pdfDocument = await PdfDocument.LoadFromFileAsync(storagePdfFile);
            
            for (uint i = 0; i < pdfDocument.PageCount; i++)
            {
                using (PdfPage page = pdfDocument.GetPage(i))
                {
                    using (InMemoryRandomAccessStream memStream = new InMemoryRandomAccessStream())
                    {
                        PdfPageRenderOptions renderOptions = new PdfPageRenderOptions();
                        await page.RenderToStreamAsync(memStream);
                        using (Image image = Image.FromStream(memStream.AsStream()))
                        {
                            //Bitmap bmp = new Bitmap(image);
                            var bi = new BitmapImage(); 
                            bi.BeginInit();
                            bi.CacheOption = BitmapCacheOption.OnLoad;
                            bi.StreamSource = memStream.AsStream();
                            bi.EndInit();
                            
                            output.Add(bi);
                        }
                    }
                }
            }
            

            return output;
            
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            List<BitmapImage> pages = await GetPdfPages("F:\\Pulpit\\Sprawozdanie2.pdf");
            Bindings.GetInstance().CurrentSheet = pages[1];
        }
    }
}