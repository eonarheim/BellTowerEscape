using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class Elevator
    {
        public string PlayerToken { get; set; }
        public int Floor { get; set; }
        public List<Meeple> Meeples { get; set; }
        public bool IsMoving { get; set; }
        // used to keep track of issued commands
        public bool Done { get; set; }

    }
}