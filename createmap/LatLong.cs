using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace createmap
{
    public class Coord
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string FormattedAddress { get; set; }

        public Coord()
        {
            Lat = 0.0;
            Lng = 0.0;
            FormattedAddress = "";
        }

        public Coord(double lat, double lng, string adr)
        {
            Lat = lat;
            Lng = lng;
            FormattedAddress = adr;
        }
    }
}
