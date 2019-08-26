using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace hugm.map
{
    /// <summary>
    /// Abstract representation of a voting area
    /// </summary>
    [Serializable]
    public class VotingArea
    {
        /// <summary>
        /// Culture info for parsing numbers
        /// </summary>
        private static CultureInfo culture = new CultureInfo("hu"); 

        /// <summary>
        /// Unique ID of the area
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The real world district number in Budapest
        /// </summary>
        public string District { get; set; }

        /// <summary>
        /// The voting area number in the district
        /// </summary>
        public int AreaNo { get; set; }

        /// <summary>
        /// Unformatted street address
        /// </summary>
        private string Street { get; set; }

        /// <summary>
        /// Fully formatted location address
        /// </summary>
        public string FormattedAddress { get; set; }

        /// <summary>
        /// Coordinate of the voting area
        /// </summary>
        public Coord LatitudeLongitude { get; set; }

        /// <summary>
        /// Initialise voting area from CSV
        /// </summary>
        /// <param name="input">A line of CSV input</param>
        public VotingArea(string[] input)
        {
            if (input.Length > 5)
            {
                ID = int.Parse(input[0]);
                District = Regex.Match(input[2], "[MDCLXVI]+").Captures[0].Value;
                AreaNo = int.Parse(input[3]);
                Street = FormatStreetName(input[4]).Trim();
                LatitudeLongitude = new Coord(double.Parse(input[5], culture), double.Parse(input[6], culture), Street);
                FormattedAddress = Street;
            }
            else
            {
                ID = -1;
                District = Regex.Match(input[1], "[MDCLXVI]+").Captures[0].Value;
                AreaNo = int.Parse(input[2]);
                Street = FormatStreetName(input[3]).Trim();
                LatitudeLongitude = new Coord();           
            }            
        }

        /// <summary>
        /// Construct semi-formatted address from CSV data
        /// </summary>
        /// <returns>Semi-formatted address suitable for lookup</returns>
        public string RawAddress()
        {
            return string.Format($"{Street}, Budapest {District}. kerület");
        }

        /// <summary>
        /// Convert area to string
        /// </summary>
        /// <returns>Formatted address of area</returns>
        public override string ToString()
        {
            return FormattedAddress;
        }

        /// <summary>
        /// Convert area to CSV line
        /// </summary>
        /// <returns>CSV-formatted representation of the area</returns>
        public string ToCsvString()
        {
            return string.Format($"{ID};BUDAPEST;Budapest {District}. ker.;{AreaNo};{FormattedAddress};{LatitudeLongitude.Lat};{LatitudeLongitude.Lng}");
        }

        private string FormatStreetName(string adr)
        {
            if (adr.IndexOf('(') >= 0)
            {
                return Regex.Match(adr, @"(.+)\(.*\)").Groups[1].Value;
            }
            return adr;
        }
    }
}
