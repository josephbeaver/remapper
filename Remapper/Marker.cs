using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Popups;

namespace Remapper
{
    class Marker
    {
        private BitmapImage fullImage;
        private string markerID;
        private decimal latitude;
        private decimal longitude;

        public BitmapImage FullImage { get { return fullImage; } }
        public string MarkerID { get { return markerID; } }

        private Marker(string filename)
        {
            markerID = filename.Substring(0, 6);
            fullImage = new BitmapImage();
        }

        public static async Task<Marker> GetMarker(StorageFile sf)
        {
            Marker m = new Marker(sf.DisplayName);
            await m.fullImage.SetSourceAsync(await sf.OpenAsync(FileAccessMode.Read)) ;

 //           var messageDialog = new MessageDialog(sf.Name);
 //           await messageDialog.ShowAsync();
            return m;
        }
    }
}
