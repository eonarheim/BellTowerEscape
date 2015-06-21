using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class Floor
    {
        public int Number { get; set; }
        public List<Meeple> Meeples { get; set; }

        public void SpawnMeeple(Game game, int numberOfMeepleToSpawn)
        {
            for (int i = 0; i < numberOfMeepleToSpawn; i++)
            {
                Meeples.Add(new Meeple(game, this)); 
            }
        }
    }
}