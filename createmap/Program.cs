using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace createmap
{
    class Program
    {
        public static void Main(string[] args)
        {
            Geocode coder = new Geocode();
            List<VotingArea> areas = coder.Run("../../data/korok_new.csv", false).GetAwaiter().GetResult();            
            Console.ReadKey();
        }
    }
}
