using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent.ApiDTOs
{
    public class StatusResult
    {
        public int Id { get; set; }
        public int Turn { get; set; }
        public int Delivered { get; set; }
        public List<Elevator> MyElevators { get; set; }
        public List<Elevator> EnemyElevators { get; set; }
        public List<Floor> Floors { get; set; }
        public bool IsGameOver { get; set; }
        public string Status { get; set; }
        public int TimeUntilNextTurn { get; set; }
    }
}
