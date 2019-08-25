using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm.map
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

        public Coord(double lat, double lng) : this(lat, lng, "") { }

        public Coord(double lat, double lng, string adr)
        {
            Lat = lat;
            Lng = lng;
            FormattedAddress = adr;
        }

        public static Coord operator +(Coord c1, Coord c2)
        {
            return new Coord(c1.Lat + c2.Lat, c1.Lng + c2.Lng);
        }

        public static Coord operator -(Coord c1, Coord c2)
        {
            return new Coord(c1.Lat - c2.Lat, c1.Lng - c2.Lng);
        }

        public static Coord operator /(Coord c, double d)
        {
            return new Coord(c.Lat / d, c.Lng / d);
        }
    }

    public static class CoordExtensions
    {
        public static Coord Average(this IEnumerable<Coord> coords)
        {
            Coord average = new Coord();
            double n = 0.0;
            foreach (Coord c in coords)
            {
                average += c;
                n++;
            }
            return average / n;
        }
    }
}
