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
                int goingUp = game.Random.Next(2);
                Floor FloorToGoTo;
                bool success;
                
                // GOING UP, unless we're on the top floor
                if (goingUp == 1 && Number != Game.NUMBER_OF_FLOORS)
                {
                    success = game.Floors.TryGetValue(game.Random.Next(Number+1, Game.NUMBER_OF_FLOORS+1), out FloorToGoTo);
                }

                // GOING DOWN, unless we're on the bottom floor
                else if (Number != 0)
                {
                    success = game.Floors.TryGetValue(game.Random.Next(Number), out FloorToGoTo);
                }

                // GOING UP, because we're on the bottom floor
                else 
                {
                    success = game.Floors.TryGetValue(game.Random.Next(1, Game.NUMBER_OF_FLOORS+1), out FloorToGoTo);
                }

                // just double checking that we're not trying to send meeple to where they currently are
                if (success && FloorToGoTo.Number != Number) { Meeples.Add(new Meeple(game, FloorToGoTo)); };
            }
        }
    }
}