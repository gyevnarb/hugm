using System;
using System.Collections.Generic;
using System.Linq;

namespace core.map
{
    public enum Parties
    {
        FIDESZ,
        OSSZEFOGAS,
        JOBBIK,
        LMP
    }

    [Serializable()]
    public class VoteResult
    {
        public int FideszKDNP { get; set; }

        public int Osszefogas { get; set; }

        public int Jobbik { get; set; }

        public int LMP { get; set; }

        public int Megjelent { get; set; }

        public int Osszes { get; set; }

        public string Gyoztes
        {
            get
            {
                if (FideszKDNP > Osszefogas && FideszKDNP > Jobbik && FideszKDNP > LMP) return "FideszKDNP";
                if (Osszefogas > FideszKDNP && Osszefogas > Jobbik && Osszefogas > LMP) return "Osszefogas";
                if (Jobbik > Osszefogas && Jobbik > FideszKDNP && Jobbik > LMP) return "Jobbik";
                return "LMP";
            }
        }

        public VoteResult(string[] input) : this(int.Parse(input[7]), int.Parse(input[8]), int.Parse(input[9]), int.Parse(input[10]), int.Parse(input[11]), int.Parse(input[12])) { }

        public void Add(VoteResult vr)
        {
            FideszKDNP += vr.FideszKDNP;
            Osszefogas += vr.Osszefogas;
            Jobbik += vr.Jobbik;
            LMP += vr.LMP;
            Megjelent += vr.Megjelent;
            Osszes += vr.Osszes;
        }

        public VoteResult()
        {
            FideszKDNP = Osszefogas = Jobbik = LMP = Megjelent = Osszes = 0;
        }

        public VoteResult(int fidesz, int osszefogas, int jobbik, int lmp, int megj, int osszes)
        {
            FideszKDNP = fidesz;
            Osszefogas = osszefogas;
            Jobbik = jobbik;
            LMP = lmp;
            Megjelent = megj;
            Osszes = osszes;
        }

        public VoteResult Clone()
        {
            return new VoteResult
            {
                FideszKDNP = this.FideszKDNP,
                Osszefogas = this.Osszefogas,
                Jobbik = this.Jobbik,
                LMP = this.LMP,
                Megjelent = this.Megjelent,
                Osszes = this.Osszes,
            };
        }

        public List<int> ResultList()
        {
            return new List<int> { FideszKDNP, Osszefogas, Jobbik, LMP };
        }

        public Dictionary<Parties, int> PartiesVotes()
        {
            return new Dictionary<Parties, int> {
                { Parties.FIDESZ, FideszKDNP },
                { Parties.OSSZEFOGAS, Osszefogas },
                { Parties.JOBBIK, Jobbik },
                { Parties.LMP, LMP }
            };
        }

        public KeyValuePair<Parties, int> Max()
        {
            return PartiesVotes().OrderBy(kv => kv.Key).First();
        }

        internal void RePop(string text, double v)
        {
            switch (text)
            {
                case "FIDESZ":
                    {
                        int repop = (int)(FideszKDNP * v);
                        int gain = repop / 3;
                        int correction = 0;
                        if (gain * 3 != repop) correction = repop - gain * 3;
                        FideszKDNP -= repop;
                        Osszefogas += gain;
                        LMP += gain + correction;
                        Jobbik += gain;
                        break;
                    }
                case "OSSZEFOGAS":
                    {
                        int repop = (int)(Osszefogas * v);
                        int gain = repop / 3;
                        int correction = 0;
                        if (gain * 3 != repop) correction = repop - gain * 3;
                        Osszefogas -= repop;
                        FideszKDNP += gain;
                        LMP += gain + correction;
                        Jobbik += gain;
                        break;
                    }
                case "JOBBIK":
                    {
                        int repop = (int)(Jobbik * v);
                        int gain = repop / 3;
                        int correction = 0;
                        if (gain * 3 != repop) correction = repop - gain * 3;
                        Jobbik -= repop;
                        Osszefogas += gain;
                        LMP += gain + correction;
                        FideszKDNP += gain;
                        break;
                    }
                case "LMP":
                    {
                        int repop = (int)(LMP * v);
                        int gain = repop / 3;
                        int correction = 0;
                        if (gain * 3 != repop) correction = repop - gain * 3;
                        LMP -= repop;
                        Osszefogas += gain;
                        FideszKDNP += gain + correction;
                        Jobbik += gain;
                        break;
                    }
            }
        }
    }
}
