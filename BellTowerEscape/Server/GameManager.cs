using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BellTowerEscape.Commands;

namespace BellTowerEscape.Server
{
    public class GameManager
    {
        private static readonly GameManager _instance = new GameManager();
        public Dictionary<int, Game> Games { get; set; } 

        public static GameManager Instance
        {
            get { return _instance; }
        }

        private GameManager()
        {
            Games = new Dictionary<int, Game>();
        }

        public Game GetNewGame()
        {
            var game = new Game();
            Games.Add(game.Id, game);
            return game;
        }
        

        [ValidateAuthToken]
        [RecordCommand]
        public MoveResult Execute(MoveCommand command)
        {
            
            // TODO Validate Command authToken
            var game = Games[command.GameId];
            
            // TODO Execute command against game
            var result = game.MoveElevator(command);

            // TODO Return result
            return result;
        }
        [RecordCommand]
        public LogonResult Execute(LogonCommand command)
        {
            var game = GetNewGame();
            var demoResult = game.LogonPlayer("DemoAgent");
            game.StartDemoAgent(demoResult, "DemoAgent");
            var result = game.LogonPlayer(command.AgentName);
            game.Start();

            return result;
        }

        [RecordCommand]
        public LogonResult Execute(LogonP1Command command)
        {
            var game = GetNewGame();
            return game.LogonPlayer(command.AgentName);
        }

        [ValidateAuthToken]
        [RecordCommand]
        public LogonResult Execute(LogonP2Command command)
        {
            var game = Games[command.GameId];
            var result = game.LogonPlayer(command.AgentName);
            game.Start();

            return result;
        }

        public KillResult Execute(KillCommand command)
        {
            return new KillResult();
        }

        [ValidateAuthToken]
        public StatusResult Execute(StatusCommand command)
        {
            var game = Games[command.GameId];
            var result = game.GetStatus(command);
            return result;
        }
    }

    internal class RecordCommandAttribute : Attribute
    {
    }

    internal class ValidateAuthToken : Attribute
    {

        
    }
}