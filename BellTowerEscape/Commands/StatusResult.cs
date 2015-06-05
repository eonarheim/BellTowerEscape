using System.Collections.Generic;
using BellTowerEscape.Server;

namespace BellTowerEscape.Commands
{
    public class StatusResult
    {
        public int Id { get; set; }
        public int Turn { get; set; }
        public int Delivered { get; set; }
        public List<ElevatorLite> Elevators { get; set; }
        public List<FloorLite> Floors { get; set; }
        public int TimeUntilNextTurn { get; set; }
    }
}