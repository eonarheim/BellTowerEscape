using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BellTowerEscape.Commands;

namespace BellTowerEscape.Server
{
    public class Game
    {
        private static int _MAXID = 0;
        private HighFrequencyTimer _gameLoop = null;
        private ConcurrentDictionary<string, Player> _players = new ConcurrentDictionary<string, Player>(); 

        public Game()
        {
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
            if (!_players.Contains(playerName))
            {
                _players.Add(playerName);
                // I know guids are not crypto secure, for this game I don't think it matters
                var newAuthToken = System.Guid.NewGuid().ToString();
                _authTokensToPlayers.TryAdd(newAuthToken, playerName);
                _playersUpdatedThisTurn.TryAdd(playerName, false);
                

                System.Diagnostics.Debug.WriteLine("Player logon [{0}]:[{1}]", playerName, newAuthToken);
                result.AuthToken = newAuthToken;
                result.GameStartTime = _nextTick;

                var h = GetNextHill();
                _board.BuildHill(h.X, h.Y, playerName);
                CollectFood(playerName, 3);
            }
            result.GameId = Id;
            System.Diagnostics.Debug.WriteLine("Player {0} already logged on!", playerName);
            return result;
        }


        public void Update(long id)
        {
            
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