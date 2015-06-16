using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class Elevator
    {
        public Elevator()
        {
            Meeples = new List<Meeple>();
            IsStopped = false;
        }
        public static int Capacity = 5;
        public string PlayerToken { get; set; }
        public int Floor { get; set; }
        public List<Meeple> Meeples { get; set; }
        public bool IsStopped { get; set; }
        // used to keep track of issued commands
        public bool Done { get; set; }

        public int FreeSpace
        {
            get { return Capacity - Meeples.Count; }
        }


    }
}