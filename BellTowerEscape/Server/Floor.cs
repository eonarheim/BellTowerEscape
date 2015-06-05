using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class Floor
    {
        public int Number { get; private set; }
        public List<Meeple> Meeples { get; set; }

        public void SpawnMeeple()
        {
            
        }
    }
}