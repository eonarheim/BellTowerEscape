using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent.ApiDTOs
{
    public class MoveCommand
    {
        public MoveCommand(string p1, int p2, int currentId, string elevatorDirection)
        {
            // TODO: Complete member initialization
            this.AuthToken = p1;
            this.GameId = p2;
            this.ElevatorId = currentId;
            this.Direction = elevatorDirection;
        }
        public string AuthToken { get; set; }
        public int GameId { get; set; }
        public int ElevatorId { get; set; }
        public string Direction { get; set; }
    }
}
