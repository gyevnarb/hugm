using System;
using System.Collections.Generic;
using System.Linq;
using hugm.graph;

namespace createmap
{
    public class AreaNode : Node
    {
        /// <summary>
        /// Group voting areas with same voting location
        /// </summary>
        public List<VotingArea> Areas { get; private set; }

        public AreaNode(int id) : base(id) { }
        public AreaNode(int id, List<VotingArea> areas) : base(id) => Areas = areas;

        public override string ToString()
        {
            return string.Format($"ID = {ID}; FormatteAddress = {Areas.First().FormattedAddress}");
        }
    }
}
