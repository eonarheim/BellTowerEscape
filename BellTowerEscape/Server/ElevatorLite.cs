using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class ElevatorLite
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public int Floor { get; set; }
        public List<MeepleLite> Meeples { get; set; }
    }
}