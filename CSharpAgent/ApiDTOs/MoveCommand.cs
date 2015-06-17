using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent.ApiDTOs
{
    public class MoveCommand
    {
        public string AuthToken { get; set; }
        public int GameId { get; set; }
        public int ElevatorId { get; set; }
        public string Direction { get; set; }
    }
}
