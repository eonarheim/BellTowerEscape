using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BellTowerEscape.Commands
{
    public class LogonP2Command : LogonCommand
    {
        public string AuthToken { get; set; }
        public int GameId { get; set; }
    }
}