﻿using System;
using System.Collections.Generic;
using System.Linq;
using core.graph;

namespace core.map
{
    /// <summary>
    /// Specialised subclass to group voting areas
    /// </summary>
    [Serializable]
    public class AreaNode : Node
    {
        /// <summary>
        /// Group voting areas with same voting location
        /// </summary>
        public List<VotingArea> Areas { get; private set; }

        /// <summary>
        /// Get coordinates of first areas in Areas
        /// </summary>
        public Coord LatitudeLongitude { get { return Areas[0].LatitudeLongitude; } }

        /// <summary>
        /// Empty voting areas with ID
        /// </summary>
        /// <param name="id">ID to assign</param>
        public AreaNode(int id) : base(id) => Areas = new List<VotingArea>();

        /// <summary>
        /// Initialise voting areas
        /// </summary>
        /// <param name="id">ID of node</param>
        /// <param name="areas">Areas the node is grouping</param>
        public AreaNode(int id, List<VotingArea> areas) : base(id) => Areas = areas;

        public int Population
        {
            get
            {
                int sum = 0;
                foreach (var a in Areas) sum += a.Results.Osszes;
                return sum;
            }
        }

        public int ElectorialDistrict
        {
            get
            {
                return Areas[0].ElectoralDistrict;
            }
            set
            {
                foreach (var a in Areas) a.ElectoralDistrict = value;
            }
        }

        public bool Atjelentkezes
        {
            get { return Areas[0].Atjelentkezes; }
        }

        public VoteResult Results
        {
            get { return Areas[0].Results; }
        }

        public override string ToJSON()
        {
            string baseString = base.ToJSON();
            baseString = baseString.Substring(1, baseString.Length - 2);
            return "{" + baseString + $", \"district\": {ElectorialDistrict}, \"area\": {Areas.First().AreaNo}" + "}";
        }

        public virtual bool Equals(AreaNode n)
        {
            return ID == n.ID;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


        /// <summary>
        /// Human-readable string of node
        /// </summary>
        /// <returns>ID and Formatted Address of node</returns>
        public override string ToString()
        {
            return ID.ToString(); //string.Format($"ID = {ID}; Kerület = {Areas.First().ElectoralDistrict}; Kör = {Areas.First().AreaNo}");
        }
    }
}