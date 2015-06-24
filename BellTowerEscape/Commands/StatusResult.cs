using System.Collections.Generic;
using BellTowerEscape.Server;

namespace BellTowerEscape.Commands
{
    public class StatusResult
    {
        public int Id { get; set; }
        public int Turn { get; set; }
        public int Delivered { get; set; }
        public int EnemyDelivered { get; set; }
        public List<ElevatorLite> MyElevators { get; set; } 
        public List<ElevatorLite> EnemyElevators { get; set; }
        public List<FloorLite> Floors { get; set; }
        public bool IsGameOver { get; set; }
        public string Status { get; set; }
        public int TimeUntilNextTurn { get; set; }
    }
}