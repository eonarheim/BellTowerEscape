using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Web;
using AutoMapper;
using BellTowerEscape.Commands;
using BellTowerEscape.Utility;

namespace BellTowerEscape.Server
{
    public class Game
    {
        private static int _MAXID = 0;
        public static int START_DELAY = 5000; // 5 seconds
        public static int TURN_DURATION = 2000; // 2 seconds
        public static int SERVER_PROCESSING = 2000; // 2 seconds
        public static int MAX_TURN = 500;

        private static int _NUMBER_OF_ELEVATORS = 4;
        public static int NUMBER_OF_FLOORS = 12;

        private HighFrequencyTimer _gameLoop = null;
        private ConcurrentDictionary<string, Player> _players = new ConcurrentDictionary<string, Player>();
        private ConcurrentDictionary<string, Player> _authTokens = new ConcurrentDictionary<string, Player>(); 
        

        public int Seed { get; set; }
        public Random Random { get; set; }
        public int Id { get; set; }
        public bool Processing { get; set; }
        public int Turn { get; set; }
        public bool GameOver { get; set; }
        private bool _started { get; set; }

        public bool _processingComplete { get; set; }

        private object synclock = new object();

        private long elapsedTotalTurn = 0;
        private long elapsedServerTime = 0;
        private long gameStartCountdown = START_DELAY;


        public ConcurrentDictionary<int, Elevator> Elevators { get; set;}
        public ConcurrentDictionary<int, Floor> Floors { get; set; }
        public bool Running { get; set; }


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
            if (Random == null)
            {
                Random = new Random();
            }
            
            Id = _MAXID++;
            
            Elevators = new ConcurrentDictionary<int, Elevator>();
            for (int i = 0; i < _NUMBER_OF_ELEVATORS; i++)
            {
                Elevators.GetOrAdd(i, new Elevator(){Id = i, Floor = 0, Meeples = new List<Meeple>()});
            }
            Floors = new ConcurrentDictionary<int, Floor>();
            for (int i = 0; i < NUMBER_OF_FLOORS; i++)
            {
                Floors.GetOrAdd(i, new Floor() {Meeples = new List<Meeple>(), Number = i});
            }

            Turn = 0;
            Running = false;
            _started = false;
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
                var success2 = _authTokens.TryAdd(newPlayer.AuthToken, newPlayer);

                if (success && success2)
                {
                    System.Diagnostics.Debug.WriteLine("Player logon [{0}]:[{1}]", newPlayer.PlayerName,
                        newPlayer.AuthToken);
                }

                _allocateElevators(newPlayer.AuthToken);
                result.AuthToken = newPlayer.AuthToken;
                result.GameId = Id;
                result.GameStart = (int) this.gameStartCountdown;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Player {0} already logged on!", playerName);
            }
            result.GameId = Id;
            
            return result;
        }

        public List<ElevatorLite> GetElevatorsForPlayer(string token)
        {
            return this.Elevators.Values.Where(e => e.PlayerToken == token).Select(Mapper.Map<ElevatorLite>).ToList();
        }

        public List<ElevatorLite> GetElevatorsForOtherPlayer(string token)
        {
            return this.Elevators.Values.Where(e => e.PlayerToken != token).Select(Mapper.Map<ElevatorLite>).ToList();
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

        public StatusResult GetStatus(StatusCommand command)
        {
            var status = "Initializing";

            if (this.GameOver)
            {
                status = "Game Over";
            }

            if (this.Running)
            {
                status = "Game Running";
            }

            if (!this._started)
            {
                status = "Game waiting for Logons";
            }


            return new StatusResult()
            {
                EnemyElevators = GetElevatorsForPlayer(command.AuthToken),
                MyElevators = GetElevatorsForPlayer(command.AuthToken),
                TimeUntilNextTurn = (int) (_started ?  
                    (SERVER_PROCESSING + TURN_DURATION - this.elapsedTotalTurn - this.elapsedServerTime) : this.gameStartCountdown),
                Delivered = _authTokens[command.AuthToken].Score,
                Floors = Floors.Values.Select(Mapper.Map<FloorLite>).ToList(),
                Id = this.Id,
                Turn = this.Turn,
                IsGameOver = this.GameOver,
                Status = status
            };
        }

        

        public MoveResult MoveElevator(MoveCommand command)
        {
            var result = new MoveResult();
            //var token = command.AuthToken;
            var id = command.ElevatorId;
            Elevator elevator;
            var exists = Elevators.TryGetValue(id, out elevator);
            
            var error = _validateMoveElevatorErrors(command);

            if (error == null && elevator != null)
            {
                elevator.Done = true;
                // we have validated control and existance of the elevator
                if (command.Direction.ToLower() == "up")
                {
                    elevator.Floor = Math.Min(elevator.Floor + 1, NUMBER_OF_FLOORS);
                    elevator.IsStopped = false;
                    result.Message = string.Format("Moved elevator {0} up successfully", command.ElevatorId);
                }
                else if (command.Direction.ToLower() == "down")
                {
                    elevator.Floor = Math.Max(elevator.Floor - 1, 0);
                    elevator.IsStopped = false;
                    result.Message = string.Format("Moved elevator {0} down successfully", command.ElevatorId);
                }
                else if (command.Direction.ToLower() == "stop")
                {
                    elevator.IsStopped = true;
                    result.Message = string.Format("Stopped elevator {0} successfully", command.ElevatorId);
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
            var id = command.ElevatorId;
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

            if (elevator.PlayerToken != token)
            {
                return new MoveResult()
                {
                    Success = false,
                    Message = string.Format("Can't move elevators you don't own ;) ID: {0}", command.ElevatorId)
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
            if (!_started)
            {
                this.gameStartCountdown -= delta;
                if (this.gameStartCountdown <= 0)
                {
                    _started = true;
                }

                return;
            }

            if (GameOver)
            {
                return;
            }

            this.elapsedTotalTurn += delta;
            if (!Processing && this.elapsedTotalTurn >= TURN_DURATION)
            {
                Processing = true;
            }

            if (this.elapsedTotalTurn >= SERVER_PROCESSING + TURN_DURATION)
            {
                Processing = false;
                _processingComplete = false;
                this.elapsedTotalTurn = 0;
                Turn++;
                // publish viz update every turn
                ClientManager.UpdateClientGame(this);
            }

            if (Processing)
            {
                // for each floor move people to stopped elevators
                for (var i = 0; i < Floors.Count; i++)
                {
                    var meepleOnFloor = Floors[i].Meeples.Count;
                    var elevatorsStoppedOnFloor = Elevators.Values.Where(e => e.IsStopped && e.Floor == i).OrderByDescending(e => e.FreeSpace).ToList();

                    foreach (var group in elevatorsStoppedOnFloor.GroupBy(e => e.FreeSpace, e => e))
                    {
                        while (meepleOnFloor > 0 && elevatorsStoppedOnFloor.Any(e => e.FreeSpace > 0))
                        {
                            var free = group.Key;
                            var elevators = group.ToList();
                            elevators.Shuffle(this);

                            foreach (var elevator in elevators)
                            {
                                var meeple = Floors[i].Meeples.FirstOrDefault();
                                if (meeple != null)
                                {
                                    Floors[i].Meeples.Remove(meeple);
                                    elevator.Meeples.Add(meeple);
                                    meeple.InElevator = true;

                                }
                            }
                            meepleOnFloor = Floors[i].Meeples.Count;
                        }
                        
                    }
                }

                // for each elevator check Meeple frustration
                foreach (var elevator in Elevators.Values)
                {
                    foreach (var meeple in elevator.Meeples)
                    {
                        meeple.Update();
                        if (meeple.Patience < 0)
                        {
                            // todo get off 
                        }

                    }
                }

                // score meeples
                foreach (var elevator in Elevators.Values)
                {
                    foreach (var meeple in elevator.Meeples)
                    {
                        if (elevator.Floor == meeple.Destination && !elevator.IsStopped)
                        {
                            elevator.Meeples.Remove(meeple);
                            _authTokens[elevator.PlayerToken].Score++;
                        }
                    }
                }


                // clear state variables
                foreach (var elevator in Elevators.Values)
                {
                    elevator.Done = false;
                }

                _processingComplete = true;
            }

            if (Turn >= MAX_TURN)
            {
                this.GameOver = true;
            }

            
        }

        

        public void Start()
        {
            Running = true;
            _gameLoop.Start();
        }

        public void Stop()
        {
            Running = false;
            _gameLoop.Stop();
        }


    }
}