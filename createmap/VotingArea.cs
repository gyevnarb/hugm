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
        public string Address { get; set; }

        public VotingArea(string[] input)
        {
            District = Regex.Match(input[1], "[MDCLXVI]+").Captures[0].Value;
            AreaNo = int.Parse(input[2]);
            Address = GetAddress(input[3]);
        }

        public VotingArea(string dist, int no, string adr)
        {
            District = dist;
            AreaNo = no;
            Address = adr;
        }

        public override string ToString()
        {
            return string.Format($"{Address}, Budapest {District}. kerület");
        }

        private string GetAddress(string adr)
        {
            if (adr.IndexOf('(') >= 0)
            {
                return Regex.Match(adr, @"(.+)\(.*\)").Groups[1].Value;
            }
            return adr;
        }
    }
}
