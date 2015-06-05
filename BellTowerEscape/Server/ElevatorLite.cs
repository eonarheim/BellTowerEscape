using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class ElevatorLite
    {
        public string Owner { get; set; }
        public int CurrentFloor { get; set; }
        public int NumberOfMeeple { get; set; }
    }
}