using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;

namespace createmap
{
    class Geocode
    {
        private static readonly HttpClient client = new HttpClient();
        private static readonly string apikey = "AIzaSyAP0RPakM5pf8grngjIAoDuaUWd5kjvXaY";
        private static readonly string geocodeSavePath = @"../../geocode.txt";
        private static readonly string latlongsSavePath = @"../../korok_new.csv";

        /// <summary>
        /// Fetch all voting area data
        /// </summary>
        /// <param name="csvPath">CSV file storing voting area data</param>
        /// <param name="geocoding">True if you want to append latitudinal and longitudinal info to the data</param>
        /// <returns>List of voting areas</returns>
        public async Task<List<VotingArea>> Run(string csvPath, bool geocoding)
        {
            try
            {
                Console.Write($"Loading csv at {csvPath}. ");
                List<VotingArea> areas = LoadCsv(csvPath);
                Console.WriteLine("Done");

                if (geocoding)
                {
                    Console.Write("Converting addresses to geocodes ");
                    List<string> geocodes = await GetAllGeocodes(areas);
                    Console.WriteLine("Done");

                    Console.Write("Extracting latlong info from responses. ");
                    List<LatLong> latlongs = GetLatLongs(geocodes);
                    Console.WriteLine("Done");

                    Console.Write($"Saving data to {latlongsSavePath}. ");
                    List<VotingArea> expanded = SaveLatLongs(latlongs, areas);
                    SaveToCsv(expanded);
                    Console.WriteLine("Done");

                    return expanded;
                }

                return areas;
            }
            catch (IOException ioex)
            {
                Console.WriteLine($"IOException occured!\nMessage: {ioex.Message}\nStackTrace: {ioex.StackTrace}");
            }
            catch (HttpRequestException httpex)
            {
                Console.WriteLine($"HttpRequestException occured!\nMessage: {httpex.Message}\nStackTrace: {httpex.StackTrace}");
            }
            return new List<VotingArea>();
        }

        private List<VotingArea> LoadCsv(string path)
        {
            List<string[]> lines = File.ReadAllLines(path).Skip(1).Select(x => x.Split(';')).ToList();
            List<VotingArea> areas = lines.Select(x => new VotingArea(x)).ToList();
            return areas;
        }

        private async Task<List<string>> GetAllGeocodes(List<VotingArea> areas)
        {
            List<string> geocodes = new List<string>();
            using (StreamWriter sw = new StreamWriter(geocodeSavePath))
            {               
                foreach (VotingArea area in areas)
                {
                    string address = area.RawAddress();
                    string geocode = await GetGeocode(address);
                    sw.WriteLine(geocode);
                    geocodes.Add(geocode);
                }
            }
            return geocodes;
        }

        private List<LatLong> GetLatLongs(List<string> geocodes)
        {
            List<LatLong> latlongs = new List<LatLong>();
            foreach (string geocode in geocodes)
            {
                JObject parse = JObject.Parse(geocode);
                var results = parse["results"][0];
                string adr = results["formatted_address"].ToString();
                double lat = results["geometry"]["location"]["lat"].ToObject<double>();
                double lng = results["geometry"]["location"]["lng"].ToObject<double>();

                LatLong ll = new LatLong(lat, lng, adr);
                latlongs.Add(ll);
            }
            return latlongs;
        }


        private List<VotingArea> SaveLatLongs(List<LatLong> lls, List<VotingArea> areas)
        {
            for (int i = 0; i < areas.Count; i++)
            {
                areas[i].FormattedAddress = lls[i].FormattedAddress;
                areas[i].LatitudeLongitude = lls[i];
            }
            return areas;
        }

        private void SaveToCsv(List<VotingArea> areas)
        {
            
            using (StreamWriter sw = new StreamWriter(latlongsSavePath, false, Encoding.UTF8))
            {
                sw.WriteLine("Település;Szavazókör;Szavazókör címe;Szélességi fok; Hosszúsági fok");
                foreach (VotingArea area in areas)
                {
                    sw.WriteLine(area.ToCsvString());
                }
            }
        }

        private async Task<string> GetGeocode(string address)
        {
            string mapsURL = "https://maps.googleapis.com/maps/api/geocode/json";
            string adrURL = AddressToURL(address);
            string requestURL = string.Format($"{mapsURL}?address={adrURL}&key={apikey}");
            var response = await client.PostAsync(requestURL, null);
            return await response.Content.ReadAsStringAsync();
        }

        private string AddressToURL(string address)
        {
            string[] sep = address.Split(' ');
            return string.Join("+", sep);
        }
    }

    class LatLong
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
