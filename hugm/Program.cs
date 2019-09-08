using System;
using hugm.graph;
using hugm.map;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Write("Source graph path: ");
            string source = Console.ReadLine();
            Console.Write("Destination graph path: ");
            string destination = Console.ReadLine();
            Console.Write("District in roman numerals: ");
            string district = Console.ReadLine();
            Graph s = AreaUtils.Load(source);
            Graph d = AreaUtils.Load(destination);
            Graph m = AreaUtils.MergeDistrictFromGraph(s, d, district);
            if (m != null) AreaUtils.Save("merge.bin", m);
            Console.WriteLine("Merged successfully");
            Console.ReadLine();
        }
    }
}
