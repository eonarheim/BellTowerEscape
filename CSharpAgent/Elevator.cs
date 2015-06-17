using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent
{
    public class Elevator
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public int CurrentFloor { get; set; }
        public List<Meeple> Meeples { get; set; }
    }
}
