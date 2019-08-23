using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace createmap
{
    public class LatLong
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string FormattedAddress { get; set; }

        public LatLong()
        {
            Latitude = 0.0;
            Longitude = 0.0;
            FormattedAddress = "";
        }

        public LatLong(double lat, double lng, string adr)
        {
            Latitude = lat;
            Longitude = lng;
            FormattedAddress = adr;
        }
    }
}
