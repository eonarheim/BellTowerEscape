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

            // Gotta pick a floor that's not this one.
            int goingUp = game.Random.Next(2);
            // GOING UP, unless we're on the top floor
            if (goingUp == 1 && CurrentFloor != Game.NUMBER_OF_FLOORS - 1)
            {
                Destination = game.Random.Next(CurrentFloor + 1, Game.NUMBER_OF_FLOORS);
            }

            // GOING DOWN, unless we're on the bottom floor
            else if (CurrentFloor != 0)
            {
                Destination = game.Random.Next(CurrentFloor);
            }

            // GOING UP, because we're on the bottom floor
            else
            {
                Destination = game.Random.Next(0, Game.NUMBER_OF_FLOORS);
            }

            // because meeple get frustrated
            Patience = Math.Abs(Destination - CurrentFloor) * Meeple.FrustrationCoefficient + 3;
            Id = _maxId++;
            InElevator = false;
        }

        public void Update()
        {
            Patience--;
        }

        public void ResetMeeple(int floor)
        {
            InElevator = false;
            CurrentFloor = floor;
            Patience = Math.Abs(Destination - CurrentFloor) * Meeple.FrustrationCoefficient + 3;
        }

        public int Id { get; private set; }
        public bool InElevator { get; set; }
        public int Destination { get; set; }
        public int CurrentFloor { get; set; }
        public int Patience { get; private set; }
        public string FrustratedAtPlayer { get; set; }

    }
}