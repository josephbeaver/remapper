using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;

namespace Remapper
{
    class UnmappedMarkers
    {
        public List<Marker> Markers { get; }

        public int NumUnmapped { get { return Markers.Count; } }

        public UnmappedMarkers()
        {
            Markers = new List<Marker>();
        }


        public async Task<bool> GetMarkers(StorageFolder markerFolder)
        {
            StorageFileQueryResult all = markerFolder.CreateFileQueryWithOptions(new QueryOptions(CommonFileQuery.OrderByName, new[] { ".JPG" }));

            IReadOnlyList<StorageFile> files = await all.GetFilesAsync();

            foreach (StorageFile sf in files)
            {
                Marker m = await Marker.GetMarker(sf);
                Markers.Add(m);
            }

            return true;
        }

    }
}

