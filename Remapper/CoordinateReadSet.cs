using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remapper
{
    class CoordinateReadSet
    {
        public const int ReadSetSize = 120;

        private List<CoordinateRead> coords;
        private List<decimal> lats;
        private List<decimal> longs;
        private decimal simpleMeanLat;
        private decimal trimmedMeanLat;
        private decimal simpleMeanLong;
        private decimal trimmedMeanLong;
        private decimal trimmedLatRange;
        private decimal trimmedLongRange;

        public int Count { get { return coords.Count; } }

        public decimal? LatEstimate { get {
                if (Count < 3) return (decimal?)null;
                if (Count < 20) return simpleMeanLat;
                return trimmedMeanLat;
            } }
        public decimal? LongEstimate { get {
                if (Count < 3) return (decimal?)null;
                if (Count < 20) return simpleMeanLong;
                return trimmedMeanLong;
            } }

        public decimal? LatError { get {
                if (Count < 20) return (decimal?)null;
                return trimmedLatRange;
            } }

        public decimal? LongError { get
            {
                if (Count < 20) return (decimal?)null;
                return trimmedLongRange;
            } }
        public CoordinateReadSet()
        {
            coords = new List<CoordinateRead>();
            lats = new List<decimal>(ReadSetSize);
            longs = new List<decimal>(ReadSetSize);
        }

        public void AddRead(CoordinateRead read)
        {
            simpleMeanLat = (simpleMeanLat * Count + read.Latitude) / (Count + 1m);
            simpleMeanLong = (simpleMeanLong * Count + read.Longitude) / (Count + 1m);
            lats.Add(read.Latitude);
            longs.Add(read.Longitude);
            coords.Add(read);
            if (Count >= 20) CalculateTrimmedMeans();
        }

        private void CalculateTrimmedMeans()
        {
            int trimSize = Count / 6;  // for every 6 points, we want to trim 1 from each end
            lats.Sort();
            longs.Sort();
            decimal latSum = 0m;
            decimal longSum = 0m;

            for (int i = trimSize; i < Count - trimSize; i++)
            {
                latSum += lats[i];
                longSum += longs[i];
            }

            trimmedMeanLat = latSum / (Count - trimSize - trimSize);
            trimmedMeanLong = longSum / (Count - trimSize - trimSize);
            trimmedLatRange = (lats[Count - trimSize - 1] - lats[trimSize]) * 111000m;
            trimmedLongRange = (decimal)((double)(longs[Count - trimSize - 1] - longs[trimSize]) * 111000.0 * Math.Cos(((double)trimmedMeanLat) * Math.PI / 180.0));

        }

    }
}
