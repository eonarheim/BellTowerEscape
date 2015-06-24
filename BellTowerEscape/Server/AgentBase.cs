using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BellTowerEscape.Commands;
using BellTowerEscape.Utility;

namespace BellTowerEscape.Server
{
 
    public class AgentBase
    {
        private bool _isRunning = false;
        private readonly HttpClient _client = null;
        private enum Direction { Up, Down, Stop } ;
        private Dictionary<int, Direction> elevatorStatus = new Dictionary<int, Direction>();

        private List<MoveCommand> _pendingMoveRequests = new List<MoveCommand>();

        public AgentBase(string name, string endpoint)
        {
            Name = name;
            // connect to api and handle gzip compressed messasges
            _client = new HttpClient() { BaseAddress = new Uri(endpoint) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected async Task<StatusResult> UpdateGameState()
        {
            var response = await _client.PostAsJsonAsync("api/game/status", new StatusCommand()
            {
                AuthToken = AuthToken,
                GameId = GameId
            });
            var result = await response.Content.ReadAsAsync<StatusResult>();
            TimeToNextTurn = result.TimeUntilNextTurn;
            Console.WriteLine(string.Format("\nTURN: {0} \t DELIVERED: {1}", result.Turn, result.Delivered));
            foreach (ElevatorLite e in result.MyElevators)
            {
                Console.WriteLine(string.Format("Elevator {0} is on floor {1} with {2} Meeples", e.Id, e.Floor, e.Meeples.Count));
            }
            return result;
        }

        protected async Task<List<MoveResult>> SendUpdate(List<MoveCommand> moveCommands)
        {
            var results = new List<MoveResult>();
            foreach (var moveCommand in moveCommands)
            {
                Console.WriteLine(string.Format("posting move {0} for elevator {1}", moveCommand.Direction, moveCommand.ElevatorId));
                var response = await _client.PostAsJsonAsync("api/game/move", moveCommand);
                var result = await response.Content.ReadAsAsync<MoveResult>();
                results.Add(result);
            }
            
            return results;
        }

        public bool MoveElevator(ElevatorLite elevator, string direction)
        {
            // Prevent dup move requests
            if (this._pendingMoveRequests.Any(m => m.ElevatorId == elevator.Id))
            {
                Console.WriteLine("WARNING! A move request has already been issued for elevator {0}", elevator.Id);
                return false;
            }

            this._pendingMoveRequests.Add(new MoveCommand() { AuthToken = AuthToken, GameId = GameId, ElevatorId = elevator.Id, Direction = direction } );
            return true;
        }

        public virtual void Update(StatusResult status)
        {
            // the elevators need a little direction in life!
            if (elevatorStatus.Count == 0)
            {
                for (int i = 0; i < status.MyElevators.Count; i++)
                {
                    ElevatorLite currentElevator = status.MyElevators[i];

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
                FloorLite currentFloor = status.Floors[i];
                peopleGoingUp.Add(currentFloor.GoingUp);
                peopleGoingDown.Add(currentFloor.GoingDown);
            }

            foreach (ElevatorLite currentElevator in status.MyElevators)
            {
                int currentFloor = currentElevator.Floor;
                int currentId = currentElevator.Id;

                bool pickUp = false;
                bool dropOff = false;

                // figure out if we should drop some people off
                foreach (MeepleLite meep in currentElevator.Meeples)
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
                        if (currentFloor >= status.Floors.Count - 1)
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


        public async Task Start(LogonResult demoLogon)
        {
            AuthToken = demoLogon.AuthToken;
            GameId = demoLogon.GameId;

            if (!_isRunning)
            {
                _isRunning = true;
                while (_isRunning)
                {

                    var gs = await UpdateGameState();
                    if (gs.IsGameOver)
                    {
                        _isRunning = false;
                        Console.WriteLine("Game Over!");
                        Console.WriteLine(gs.Status);
                        _client.Dispose();
                        break;
                    }

                    Update(gs);
                    var ur = await SendUpdate(this._pendingMoveRequests);
                    this._pendingMoveRequests.Clear();
                    if (TimeToNextTurn > 0)
                    {
                        await Task.Delay((int) (TimeToNextTurn));
                    }
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
        }
        
        protected long TimeToNextTurn { get; set; }

        protected int GameId { get; set; }

        public string AuthToken { get; set; }

        public string Name { get; set; }
    }
}
