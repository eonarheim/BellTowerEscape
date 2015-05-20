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

        public static GameManager Instance
        {
            get { return _instance; }
        }

        private GameManager()
        {
            
        }
        

        /*public IResult Execute<T>(T command) where T : IGameCommand
        {
            var type = typeof (T);
            var method = type.Name.Replace("Command", string.Empty);
            var me = this.GetType();

            // TODO record commands with timing info for replays!

            return (IResult) me.GetMethod(method).Invoke(Instance, new object[]{ command });
        }*/

        [ValidateAuthToken]
        [RecordCommand]
        public MoveResult Execute(MoveCommand command)
        {
            // TODO Validate Command authToken
            // TODO Execute command against game
            // TODO Return result
            return new MoveResult();
        }
        [RecordCommand]
        public LogonResult Execute(LogonCommand command)
        {
            return new LogonResult();   
        }

        public KillResult Execute(KillCommand command)
        {
            return new KillResult();
        }

        [ValidateAuthToken]
        public StatusResult Execute(StatusCommand command)
        {
            return new StatusResult();
        }
    }

    internal class RecordCommandAttribute : Attribute
    {
    }

    internal class ValidateAuthToken : Attribute
    {

        
    }
}