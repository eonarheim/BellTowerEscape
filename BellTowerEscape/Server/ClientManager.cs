using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BellTowerEscape.Hubs;
using Microsoft.AspNet.SignalR;

namespace BellTowerEscape.Server
{
    public class ClientManager
    {

        /// <summary>
        /// Updates the client game
        /// </summary>
        /// <param name="game"></param>
        public static void UpdateClientGame(Game game)
        {
            GetHubContext().Clients.Group("game" + game.Id).update(game);
        }

        public static IHubContext GetHubContext()
        {
            return GlobalHost.ConnectionManager.GetHubContext<ElevatorHub>();
        }

    }
}