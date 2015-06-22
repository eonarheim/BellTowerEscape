using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAgent.ApiDTOs;

namespace CSharpAgent
{
    public class Agent : AgentBase
    {
        private enum Direction { Up, Down, Stop } ;
        private Dictionary<int, Direction> elevatorStatus = new Dictionary<int, Direction>();

        public Agent(string name, string endpoint = "http://localhost:3193/") 
            : base(name, endpoint)
        {
        }

        public override void Update(StatusResult status)
        {
            // the elevators need a little direction in life!
            if (elevatorStatus.Count == 0)
            {
                for (int i = 0; i < status.MyElevators.Count; i++)
                {
                    Elevator currentElevator = status.MyElevators[i];

                    // even elevators go up!
                    if (i % 2 == 0)
                    {
                        elevatorStatus.Add(currentElevator.Id, Direction.Up);
                    }
                    else
                    {
                        elevatorStatus.Add(currentElevator.Id, Direction.Down);
                    }
                }
            }

            // todo implement your agent's logic here. We have implemented a basic elevator sweep alg for you!
            List<int> peopleGoingUp = new List<int>();
            List<int> peopleGoingDown = new List<int>();

            // figure out where people are going up/down
            for (int i = 0; i < status.Floors.Count; i++)
            {
                Floor currentFloor = status.Floors[i];
                peopleGoingUp.Add(currentFloor.GoingUp);
                peopleGoingDown.Add(currentFloor.GoingDown);
            }

            foreach (Elevator currentElevator in status.MyElevators)
            {
                int currentFloor = currentElevator.Floor;
                int currentId = currentElevator.Id;

                bool pickUp = false;
                bool dropOff = false;

                // figure out if we should drop some people off
                foreach (Meeple meep in currentElevator.Meeples)
                {
                    if (meep.Destination == currentFloor)
                    {
                        dropOff = true;
                    }
                }

                // elevators might be switching directions yo. Especially if they are at the top/bottom of the building
                if (!dropOff)
                {
                    if (elevatorStatus[currentId] == Direction.Up)
                    {
                        if (currentFloor >= status.Floors.Count-1)
                        {
                            elevatorStatus[currentId] = Direction.Down;
                        }
                    }
                    else if (elevatorStatus[currentId] == Direction.Down)
                    {
                        if (currentFloor <= 0)
                        {
                            elevatorStatus[currentId] = Direction.Up;
                        }
                    }

                }

                // sometimes we just need to pick some people up along the way...
                if (elevatorStatus[currentId] == Direction.Up)
                {
                    if (peopleGoingUp[currentFloor] > 0)
                    {
                        pickUp = true;
                    }
                }

                // and possibly if they are going the other way as well!
                if (elevatorStatus[currentId] == Direction.Down)
                {
                    if (peopleGoingDown[currentFloor] > 0)
                    {
                        pickUp = true;
                    }
                }

                // time to figure out the move!
                string elevatorDirection = "";
                if (dropOff)
                {
                    elevatorDirection = Direction.Stop.ToString();
                }
                else if (pickUp && currentElevator.FreeSpace > 0)
                {
                    elevatorDirection = Direction.Stop.ToString();
                }
                else
                {
                    if (elevatorStatus[currentId] == (int)Direction.Up)
                    {
                        elevatorDirection = Direction.Up.ToString();
                    }
                    else
                    {
                        elevatorDirection = Direction.Down.ToString();
                    }
                }

                // send it out!
                MoveElevator(currentElevator, elevatorDirection);

            }
            
        }
    }
}
