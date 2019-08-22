using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace createmap
{
    class VotingArea
    {
        public string District { get; set; }
        public int AreaNo { get; set; }
        private string Street { get; set; }
        public string FormattedAddress { get; set; }
        public LatLong LatitudeLongitude { get; set; }

        public VotingArea(string[] input)
        {
            District = Regex.Match(input[1], "[MDCLXVI]+").Captures[0].Value;
            AreaNo = int.Parse(input[2]);
            Street = FormatStreetName(input[3]).Trim();

            if (input.Length > 5)
            {
                LatitudeLongitude = new LatLong(double.Parse(input[4]), double.Parse(input[5]), Street);
                FormattedAddress = Street;
            }
            else
            {
                LatitudeLongitude = new LatLong();           
            }            
        }

        public VotingArea(string dist, int no, string adr)
        {
            District = dist;
            AreaNo = no;
            Street = adr;
        }

        public string RawAddress()
        {
            return string.Format($"{Street}, Budapest {District}. kerület");
        }

        public override string ToString()
        {
            return FormattedAddress;
        }

        public string ToCsvString()
        {
            return string.Format($"BUDAPEST;Budapest {District}. ker.;{AreaNo};{FormattedAddress};{LatitudeLongitude.Latitude};{LatitudeLongitude.Longitude}");
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
