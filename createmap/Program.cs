using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace createmap
{
    class Program
    {
        static void Main(string[] args)
        {
            var areas = LoadCsv(@"../../korok.csv");
            Console.ReadKey();
        }

        static List<VotingArea> LoadCsv(string path)
        {
            List<string[]> lines = File.ReadAllLines(path).Skip(1).Select(x => x.Split(';')).ToList();
            List<VotingArea> areas = lines.Select(x => new VotingArea(x)).ToList();
            return areas;
        }
    }
}
