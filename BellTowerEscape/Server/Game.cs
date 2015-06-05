using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using BellTowerEscape.Commands;

namespace BellTowerEscape.Server
{
    public class Game
    {
        private static int _MAXID = -1;
        private static int _START_DELAY = 5000; // 5 seconds
        private static int _TURN_DURATION = 2000; // 2 seconds
        private static int _SERVER_PROCESSING = 2000; // 2 seconds

        private static int _NUMBER_OF_ELEVATORS = 4;
        private static int _NUMBER_OF_FLOORS = 12;

        private HighFrequencyTimer _gameLoop = null;
        private ConcurrentDictionary<string, Player> _players = new ConcurrentDictionary<string, Player>();
        private ConcurrentDictionary<string, Player> _authTokens = new ConcurrentDictionary<string, Player>(); 

        public int Seed { get; set; }
        public Random Random { get; set; }
        public int Id { get; set; }
        public bool Processing { get; set; }

        private object synclock = new object();

        private long elapsedPlayerTime = 0;
        private long elapsedServerTime = 0;


        public ConcurrentDictionary<int, Elevator> Elevators { get; set;}
        

        public Game(int? seed, int? id) : base()
        {
            if (seed != null && seed.HasValue)
            {
                Random = new Random(seed.Value);
            }

            if (id != null && id.HasValue)
            {
                Id = id.Value;
            }
        }

        public Game()
        {
            if (Id < 0)
            {
                Id = _MAXID++;
            }
            Elevators = new ConcurrentDictionary<int, Elevator>();
            for (int i = 0; i < _NUMBER_OF_ELEVATORS; i++)
            {
                Elevators.GetOrAdd(i, new Elevator(){Floor = 0, Meeples = new List<Meeple>()});
            }
            _gameLoop = new HighFrequencyTimer(60, this.Update);
        }

        /// <summary>
        /// Logs player with a certain name into the game and returns an authorization token
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public LogonResult LogonPlayer(string playerName)
        {
            var result = new LogonResult();
            if (!_players.ContainsKey(playerName))
            {
                var newPlayer = new Player()
                {
                    AuthToken = System.Guid.NewGuid().ToString(),
                    PlayerName = playerName
                };

                var success = _players.TryAdd(playerName, newPlayer);

                if (success)
                {
                    System.Diagnostics.Debug.WriteLine("Player logon [{0}]:[{1}]", newPlayer.PlayerName, newPlayer.AuthToken);    
                }

                _allocateElevators(newPlayer.AuthToken);
                result.AuthToken = newPlayer.AuthToken;
                result.GameId = Id;
                result.GameStart = _START_DELAY;
            }
            result.GameId = Id;
            System.Diagnostics.Debug.WriteLine("Player {0} already logged on!", playerName);
            return result;
        }

        private void _allocateElevators(string token)
        {
            lock (synclock)
            {
                var count = _NUMBER_OF_ELEVATORS/2;
                foreach (var elevator in Elevators.Values)
                {
                    if (count <= 0) break;
                    if(string.IsNullOrEmpty(elevator.PlayerToken))
                    {
                        count--;
                        elevator.PlayerToken = token;
                    }
                }
            }
        }

        public MoveResult MoveElevator(MoveCommand command)
        {
            var result = new MoveResult();
            var token = command.AuthToken;
            var id = command.Id;
            Elevator elevator;
            var exists = Elevators.TryGetValue(id, out elevator);

            var error = _validateMoveElevatorErrors(command);

            if (error == null && elevator != null)
            {
                elevator.Done = true;
                // we have validated control and existance of the elevator
                if (command.Direction.ToLower() == "up")
                {
                    elevator.Floor = Math.Min(elevator.Floor + 1, _NUMBER_OF_FLOORS);
                    elevator.IsMoving = true;
                    result.Message = string.Format("Moved elevator {0} up successfully", command.Id);
                }
                else if (command.Direction.ToLower() == "down")
                {
                    elevator.Floor = Math.Max(elevator.Floor - 1, 0);
                    elevator.IsMoving = true;
                    result.Message = string.Format("Moved elevator {0} down successfully", command.Id);
                }
                else if (command.Direction.ToLower() == "stop")
                {
                    elevator.IsMoving = false;
                    result.Message = string.Format("Stopped elevator {0} successfully", command.Id);
                }
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(error.Message);
                return error;
            }

            return result;

        }

        private MoveResult _validateMoveElevatorErrors(MoveCommand command)
        {
            var token = command.AuthToken;
            var id = command.Id;
            Elevator elevator;
            var exists = Elevators.TryGetValue(id, out elevator);

            if (!exists)
            {
                return new MoveResult()
                {
                    Success = false,
                    Message = string.Format("Elevator for id {0} does not exist", id)
                };
            }

            if (string.IsNullOrEmpty(command.Direction))
            {
                return new MoveResult()
                {
                    Success = false,
                    Message = string.Format("Could not move elevator on empty direction")
                };
            }

            if (command.Direction.ToLower() != "up" &&
                command.Direction.ToLower() != "down" &&
                command.Direction.ToLower() != "stop")
            {
                return new MoveResult()
                {
                    Success = false,
                    Message = string.Format("Could not move elevator on invalid direction {0}", command.Direction)
                };
            }

            if (elevator.Done)
            {
                return new MoveResult()
                {
                    Success = false,
                    Message = string.Format("Elevator has already been moved")
                };
            }

            if (Processing)
            {
                return new MoveResult()
                {
                    Success = false,
                    Message = string.Format("Server is processing, please try again soon")
                };
            }
            return null;
        }


        public void Update(long delta)
        {
            if (!Processing && (this.elapsedPlayerTime += delta) >= _TURN_DURATION)
            {
                Processing = true;
                this.elapsedPlayerTime = 0;
            }

            if (Processing && (this.elapsedServerTime += delta) >= _SERVER_PROCESSING)
            {
                Processing = false;
                this.elapsedServerTime = 0;
            }

            if (Processing)
            {
                // for each floor move people to stopped elevators
                // for each elevator check Meeple frustration


                // clear state variables
            }
            
            // publish viz update
        }

        public void Start()
        {
            _gameLoop.Start();
        }

        public void Stop()
        {
            _gameLoop.Stop();
        }


    }
}