using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class Meeple
    {
        public int Id { get; set; }
        public bool InElevator { get; set; }
        public int Destination { get; set; }
        public int Patience { get; set; }
    }
}