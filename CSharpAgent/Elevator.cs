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
        public int Floor { get; set; }
        public int FreeSpace { get; set; }
        public List<Meeple> Meeples { get; set; }
    }
}
