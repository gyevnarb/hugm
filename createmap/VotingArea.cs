using System.Text.RegularExpressions;

namespace createmap
{
    public class VotingArea
    {
        public int ID { get; set; }
        public string District { get; set; }
        public int AreaNo { get; set; }
        private string Street { get; set; }
        public string FormattedAddress { get; set; }
        public LatLong LatitudeLongitude { get; set; }

        public VotingArea(string[] input)
        {
            if (input.Length > 5)
            {
                ID = int.Parse(input[0]);
                District = Regex.Match(input[2], "[MDCLXVI]+").Captures[0].Value;
                AreaNo = int.Parse(input[3]);
                Street = FormatStreetName(input[4]).Trim();
                LatitudeLongitude = new LatLong(double.Parse(input[5]), double.Parse(input[6]), Street);
                FormattedAddress = Street;
            }
            else
            {
                ID = -1;
                District = Regex.Match(input[1], "[MDCLXVI]+").Captures[0].Value;
                AreaNo = int.Parse(input[2]);
                Street = FormatStreetName(input[3]).Trim();
                LatitudeLongitude = new LatLong();           
            }            
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
            return string.Format($"{ID};BUDAPEST;Budapest {District}. ker.;{AreaNo};{FormattedAddress};{LatitudeLongitude.Latitude};{LatitudeLongitude.Longitude}");
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
