using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;

namespace BellTowerEscape.Hubs
{
    public class ElevatorHub : Hub
    {
        /// <summary>
        /// Client asks to listen to game so add them to the Game{Id} group
        /// </summary>
        /// <param name="gameId"></param>
        public void Listen(int gameId)
        {
            Groups.Add(Context.ConnectionId, "game" + gameId);
        }

    }
}