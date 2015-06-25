using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using BellTowerEscape.Commands;
using BellTowerEscape.Server;

namespace BellTowerEscape.Controllers
{
    public class GameController : ApiController
    {
        private readonly GameManager _gameManager;

        public GameController()
        {
            _gameManager = GameManager.Instance;
        }

        /// <summary>
        /// Gets the list of current games
        /// </summary>
        /// <returns></returns>
        [Route("api/game")]
        public IList<Game> Get()
        {

            return _gameManager.Games.Values.Where(g => g.Running).ToList();
        }

        /// <summary>
        /// Initiates an agent logon with the simulation server by name. Once an agent is logged on, 
        /// a logon result is returned with the id and starting time of the next game.
        /// </summary>
        /// <param name="agentName"></param>
        /// <returns>LogonResult</returns>
        [HttpPost]
        [Route("api/game/logon")]
        public LogonResult Logon(LogonCommand logon)
        {
            if (IsValidLogonRequest(logon))
            {
                return _gameManager.Execute(logon);
            }
            return null;
        }
        [HttpPost]
        [Route("api/game/logonP1")]
        public LogonResult LogonP1(LogonP1Command logon)
        {
            if (IsValidLogonRequest(logon))
            {
                return _gameManager.Execute(logon);
            }
            return null;
        }

        [HttpPost]
        [Route("api/game/logonP2")]
        public LogonResult LogonP2(LogonP2Command logon)
        {
            if (IsValidP2LogonRequest(logon))
            {
                return _gameManager.Execute(logon);
            }
            return null;
        }

        [HttpPost]
        [Route("api/game/move")]
        public MoveResult Move(MoveCommand move)
        {
            if (IsValidMoveRequest(move))
            {
                return _gameManager.Execute(move);
            }
            return null;
        }

        [HttpPost]
        [Route("api/game/status")]
        public StatusResult Status(StatusCommand status)
        {
            if (IsValidStatusRequest(status))
            {
                return _gameManager.Execute(status);
            }
            return null;
        }

        private bool IsValidStatusRequest(StatusCommand status)
        {
            if (status != null && !string.IsNullOrWhiteSpace(status.AuthToken) && (status.GameId >= 0))
            {
                return true;
            }
            return false;
        }


        private bool IsValidMoveRequest(MoveCommand move)
        {
            if (move != null && !string.IsNullOrWhiteSpace(move.AuthToken) && move.GameId >= 0)
            {
                if (move.Direction.Equals("up", StringComparison.InvariantCultureIgnoreCase) ||
                    move.Direction.Equals("down", StringComparison.InvariantCultureIgnoreCase) ||
                    move.Direction.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsValidLogonRequest(LogonCommand logon)
        {
            if (logon != null && !string.IsNullOrWhiteSpace(logon.AgentName))
            {
                return true;
            }
            return false;

        }

        private bool IsValidP2LogonRequest(LogonP2Command logon)
        {
            if (IsValidLogonRequest(logon))
            {
                if (!string.IsNullOrWhiteSpace(logon.AuthToken) && logon.GameId >= 0)
                {
                    return true;
                }
            } 
            
            return false;

        }
    }
}
