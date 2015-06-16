using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Server
{
    public class Meeple
    {
        private static int _maxId = 0;
        public static int FrustrationCoefficient = 2;

        public Meeple(Game game, Floor floor) : base()
        {
            CurrentFloor = floor.Number;
            Destination =  (int) Math.Floor(game.Random.NextDouble() * Game.NUMBER_OF_FLOORS);
            Patience = Math.Abs(Destination - CurrentFloor) * Meeple.FrustrationCoefficient + 3;
            Id = _maxId++;
            InElevator = false;
        }

        public void Update()
        {
            Patience--;
        }

        public int Id { get; private set; }
        public bool InElevator { get; set; }
        public int Destination { get; set; }
        public int CurrentFloor { get; set; }
        public int Patience { get; private set; }
    }
}