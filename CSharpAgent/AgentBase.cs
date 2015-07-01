﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpAgent.ApiDTOs;

namespace CSharpAgent
{
 
    public class AgentBase
    {
        private bool _isRunning = false;
        private readonly HttpClient _client = null;

        private List<MoveCommand> _pendingMoveRequests = new List<MoveCommand>();

        public AgentBase(string name, string endpoint)
        {
            Name = name;
            // connect to api and handle gzip compressed messasges
            _client = new HttpClient() { BaseAddress = new Uri(endpoint) };
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected async Task<LogonResult> Logon()
        {
            var response = await _client.PostAsJsonAsync("api/game/logon", new LogonCommand()
            {
                AgentName = Name
            });
            var result = await response.Content.ReadAsAsync<LogonResult>();
            AuthToken = result.AuthToken;
            GameId = result.GameId;
            Console.WriteLine("Your game Id is " + result.GameId);
            return result;
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
            Console.WriteLine(string.Format("\nTURN: {0} \t DELIVERED: {1} \t ENEMY DELIVERED {2}", result.Turn, result.Delivered, result.EnemyDelivered));
            foreach (Elevator e in result.EnemyElevators)
            {
                Console.WriteLine(string.Format("Enemy Elevator {0} is on floor {1} with {2} Meeples", e.Id, e.Floor, e.Meeples.Count));
            }
            foreach (Elevator e in result.MyElevators)
            {
                Console.WriteLine(string.Format("My Elevator {0} is on floor {1} with {2} Meeples", e.Id, e.Floor, e.Meeples.Count));
            }
            return result;
        }

        protected async Task<List<MoveResult>> SendUpdate(List<MoveCommand> moveCommands)
        {
            var results = new List<MoveResult>();
            foreach (var moveCommand in moveCommands)
            {
                // Console.WriteLine(string.Format("posting move {0} for elevator {1}", moveCommand.Direction, moveCommand.ElevatorId));
                var response = await _client.PostAsJsonAsync("api/game/move", moveCommand);
                var result = await response.Content.ReadAsAsync<MoveResult>();
                Console.WriteLine(result.Message);
                results.Add(result);
            }
            
            return results;
        }

        public bool MoveElevator(Elevator elevator, string direction)
        {
            // Prevent dup move requests
            if (this._pendingMoveRequests.Any(m => m.ElevatorId == elevator.Id))
            {
                Console.WriteLine("WARNING! A move request has already been issued for elevator {0}", elevator.Id);
                return false;
            }

            this._pendingMoveRequests.Add(new MoveCommand(AuthToken, GameId, elevator.Id, direction));
            return true;
        }

        public virtual void Update(StatusResult status)
        {

        }


        public async Task Start()
        {
            await Logon();
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
