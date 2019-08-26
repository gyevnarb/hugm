using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hugm.map
{
    public class VoteResult
    {
        public int FideszKDNP { get; set; }

        public int Osszefogas { get; set; }

        public int Jobbik { get; set; }

        public int LMP { get; set; }

        public int Megjelent { get; set; }

        public int Osszes { get; set; }

        public VoteResult(string[] input) : this(int.Parse(input[7]), int.Parse(input[8]), int.Parse(input[9]), int.Parse(input[10]), int.Parse(input[11]), int.Parse(input[12])) { }

        public VoteResult(int fidesz, int osszefogas, int jobbik, int lmp, int megj, int osszes)
        {
            FideszKDNP = fidesz;
            Osszefogas = osszefogas;
            Jobbik = jobbik;
            LMP = lmp;
            Megjelent = megj;
            Osszes = osszes;
        }
    }
}
