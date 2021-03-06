﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public int totalTimeProcessing = TURN_DURATION + SERVER_PROCESSING;
        public static int TIME_TO_WAIT_FOR_SECOND_PLAYER = 60000; // 1 minute
        public static int MAX_TURN = 500;
        public static bool IsRunningLocally = HttpContext.Current.Request.IsLocal;

        private static int _NUMBER_OF_ELEVATORS = 4;
        public static int NUMBER_OF_FLOORS = 12;
        private static int _MAX_PEOPLE_TO_ADD_PER_FLOOR = 2;

        private HighFrequencyTimer _gameLoop = null;
        public ConcurrentDictionary<string, Player> Players = new ConcurrentDictionary<string, Player>();
        private ConcurrentDictionary<string, Player> _authTokens = new ConcurrentDictionary<string, Player>(); 
        

        public int Seed { get; set; }
        public Random Random { get; set; }
        public int Id { get; set; }
        public bool Processing { get; set; }
        public int Turn { get; set; }
        public bool GameOver { get; set; }
        private bool _started { get; set; }
        public bool Waiting { get; set; }

        public bool _processingComplete { get; set; }

        private object synclock = new object();

        private long elapsedWaitTime = 0;
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
            // dirty filthy hacks
            var evenElevators = Random.Next(NUMBER_OF_FLOORS);
            var oddElevators = Random.Next(NUMBER_OF_FLOORS);
            for (int i = 0; i < _NUMBER_OF_ELEVATORS; i++)
            {
                Elevators.GetOrAdd(i, new Elevator(){Id = i, Floor = (i % 2 == 0) ? evenElevators : oddElevators, Meeples = new List<Meeple>()});
            }
            Floors = new ConcurrentDictionary<int, Floor>();

            for (int i = 0; i < NUMBER_OF_FLOORS; i++)
            {
                Floors.GetOrAdd(i, new Floor() {Meeples = new List<Meeple>(), Number = i});
            }

            AddMeepleToFloors();

            Turn = 0;
            Running = false;
            _started = false;
            _gameLoop = new HighFrequencyTimer(60, this.Update);
        }

        private void AddMeepleToFloors()
        {
            Floors[Random.Next(NUMBER_OF_FLOORS)].SpawnMeeple(this, Random.Next(_MAX_PEOPLE_TO_ADD_PER_FLOOR));
        }

        /// <summary>
        /// Logs player with a certain name into the game and returns an authorization token
        /// </summary>
        /// <param name="playerName"></param>
        /// <returns></returns>
        public LogonResult LogonPlayer(string playerName)
        {
            var result = new LogonResult();
            if (!Players.ContainsKey(playerName))
            {
                var newPlayer = new Player()
                {
                    AuthToken = System.Guid.NewGuid().ToString(),
                    PlayerName = playerName
                };

                var success = Players.TryAdd(playerName, newPlayer);
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

        public void StartDemoAgent(LogonResult demoResult, string playerName)
        {
            var agentTask = Task.Factory.StartNew(() =>
            {
                string endpoint = "";
                if (IsRunningLocally)
                {
                    endpoint = "http://localhost:3193";
                }
                else {
                    endpoint = "http://elevators.azurewebsites.net";
                }
                AgentBase sweetDemoAgent = new AgentBase(playerName, endpoint);
                sweetDemoAgent.Start(demoResult).Wait();
            });
        }

        public List<ElevatorLite> GetElevatorsForPlayer(string token)
        {
            return this.Elevators.Values.Where(e => e.PlayerToken == token).Select(Mapper.Map<ElevatorLite>).ToList();
        }

        public List<ElevatorLite> GetElevatorsForOtherPlayer(string token)
        {
            return this.Elevators.Values.Where(e => e.PlayerToken != token).Select(Mapper.Map<ElevatorLite>).ToList();
        }

        public int GetEnemyDelivered(string token)
        {
            return this._authTokens.Values.Where(e => e.AuthToken != token).First().Score;
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

            if (!this._started)
            {
                status = "Game waiting for Logons";
            }

            if (this.Waiting)
            {
                status = "Waiting for another player to join...";
            }

            if (this.GameOver)
            {
                if (this.Waiting) {
                    status = "Other players never joined";
                }
                else if (this._authTokens.Values.GroupBy(e => e.Score).First().Count() == this.Players.Count())
                {
                    status = "Game Over - It's a TIE!";
                }
                else
                {
                    var winningPlayer = this._authTokens.Values.OrderByDescending(e => e.Score).First().PlayerName;
                    status = "Game Over - " + winningPlayer + " wins!";
                }
            }

            if (this.Running)
            {
                status = "Game Running";
            }

            return new StatusResult()
            {
                EnemyElevators = GetElevatorsForOtherPlayer(command.AuthToken),
                MyElevators = GetElevatorsForPlayer(command.AuthToken),
                TimeUntilNextTurn = (int) (_started ?  
                    (SERVER_PROCESSING + TURN_DURATION - this.elapsedTotalTurn - this.elapsedServerTime) : Waiting ? SERVER_PROCESSING + TURN_DURATION : this.gameStartCountdown),
                Delivered = _authTokens[command.AuthToken].Score,
                EnemyDelivered = this.Waiting ? 0 : GetEnemyDelivered(command.AuthToken),
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
                    elevator.Floor = Math.Min(elevator.Floor + 1, NUMBER_OF_FLOORS - 1);
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
            if (this.Waiting)
            {
                elapsedWaitTime += delta;
                if (elapsedWaitTime > TIME_TO_WAIT_FOR_SECOND_PLAYER)
                {
                    GameOver = true;
                    this.Stop();
                }
                return;
            }

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
                this.Stop();
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

            if (Processing && !_processingComplete)
            {
                // score meeples
                foreach (var elevator in Elevators.Values.ToList())
                {
                    foreach (var meeple in elevator.Meeples.ToList())
                    {
                        if (elevator.Floor == meeple.Destination && elevator.IsStopped)
                        {
                            if (meeple.Patience >= 0)
                            {
                                elevator.Meeples.Remove(meeple);
                                _authTokens[elevator.PlayerToken].Score++;
                            }
                        }
                    }
                }

                // for each floor
                for (int i = 0; i < Floors.Count; i++)
                {
                    // let's just not bother when there are no meeples
                    if (Floors[i].Meeples.Count <= 0) { continue; }

                    // get all stopped elevators based on capacity
                    List<Elevator> elevatorsStoppedOnFloor = Elevators.Values.Where(e => e.IsStopped && e.Floor == i).ToList();
                    // ensure "fairness" by shuffling
                    elevatorsStoppedOnFloor.Shuffle(this);
                    // sort ascnedingly
                    elevatorsStoppedOnFloor = elevatorsStoppedOnFloor.OrderByDescending(e => e.FreeSpace).ToList();
                    var elevatorGroups = elevatorsStoppedOnFloor.GroupBy(e => e.FreeSpace);

                    // each group should have the same amount of free space
                    foreach (var eGroup in elevatorGroups)
                    {
                        // SHUFFLE IT AGAIN!
                        var eGroupShuffled = eGroup.ToList();
                        eGroupShuffled.Shuffle(this);
                        
                        // just kill it, then again max of 4 elevators, possibly not worth it. BUT bell tower is expandable!
                        if (Floors[i].Meeples.Count <= 0) { break; }

                        // keep going for the amount of free space in the elevator
                        for (int j = eGroup.Key; j > 0; j--)
                        {
                            // add 1 meeple to the elevator at a time
                            foreach (Elevator elevator in eGroupShuffled)
                            {
                                if (Floors[i].Meeples.Count > 0 && elevator.FreeSpace > 0)
                                {
                                    Meeple meeple = Floors[i].Meeples[0];
                                    Floors[i].Meeples.Remove(meeple);
                                    elevator.Meeples.Add(meeple);
                                    meeple.InElevator = true;
                                }
                            }
                        }

                    }
                }

                // for each elevator check Meeple frustration
                foreach (var elevator in Elevators.Values.ToList())
                {
                    foreach (var meeple in elevator.Meeples.ToList())
                    {
                        meeple.Update();
                        if (meeple.Patience < 0 && elevator.IsStopped)
                        {
                            // GET OFF. If the meeple is on the floor it wanted, you still get negative points.
                            if (meeple.Destination != elevator.Floor)
                            {
                                // TODO: It may or may not be worth having that meeple be frustrated to the point where they don't want to get back on your elevators
                                meeple.FrustratedAtPlayer = elevator.PlayerToken;
                                Floor floor;
                                Floors.TryGetValue(elevator.Floor, out floor);
                                meeple.ResetMeeple(elevator.Floor);
                                floor.Meeples.Add(meeple);
                                _authTokens[elevator.PlayerToken].Score--;
                            }
                            elevator.Meeples.Remove(meeple);
                        }

                    }
                }

                // add new Meeples
                AddMeepleToFloors();

                // clear state variables
                foreach (var elevator in Elevators.Values)
                {
                    elevator.IsStopped = false; //so, if your AI skips a turn, you can't fake a "stopped" elevator ploy
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