using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Remapper
{
    class CoordinateRead
    {
        private decimal latitude;
        private decimal longitude;
        private decimal utcTime;

        private CoordinateRead(decimal lat, decimal lon, decimal time)
        {
            this.latitude = lat;
            this.longitude = lon;
            this.utcTime = time;
        }

        public decimal Latitude { get { return latitude; } }
        public decimal Longitude { get { return longitude; } }
        public decimal UTCTime { get { return utcTime; } }

        public static CoordinateRead FromGGA(string gga, DateTime utcReadStart)
        {
            string[] ggaElements = gga.Split(',');

            if (!decimal.TryParse(ggaElements[1], out decimal time)) return null;
            if (time < utcReadStart.Hour * 10000m + utcReadStart.Minute * 100m + utcReadStart.Second * 1m) return null;
            if (!decimal.TryParse((ggaElements[3] == "S" ? "-" : "") + ggaElements[2], out decimal uncLat)) return null;
            if (!decimal.TryParse((ggaElements[5] == "W" ? "-" : "") + ggaElements[4], out decimal uncLon)) return null;



            return new CoordinateRead(FromNMEAFormat(uncLat), FromNMEAFormat(uncLon), time);
        }

        private static decimal FromNMEAFormat(decimal uncorrected)
        { 
            /* NMEA latitude and longitude data are in an odd format. The degrees and minutes are before the 
             decimal point, and the fractional portion is decimal minutes.  This method converts from this mix
             of degrees and minutes to decimal degrees. */
            int degrees = (int)uncorrected / 100; // truncation eliminates fractional portion, division cuts the minutes out
            int minutes = (int)uncorrected % 100; // truncation eliminates fractional portion, modulus gives the minutes

            decimal decMinutes = minutes + (uncorrected - decimal.Truncate(uncorrected)); // gets minutes and fractional part as decimal minutes

            return degrees + decMinutes / 60m; // dividing by 60 converts minutes to decimal degrees
        }

        public override string ToString()
        {
            return latitude + " " + longitude + " " + utcTime;
        }
    }
}
