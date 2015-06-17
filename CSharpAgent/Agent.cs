using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpAgent.ApiDTOs;

namespace CSharpAgent
{
    public class Agent : AgentBase
    {
        public List<string> moves = new List<string>(){"up", "down", "stop"};
        public Agent(string name, string endpoint = "http://localhost:3193/") 
            : base(name, endpoint)
        {
        }

        public override void Update(StatusResult status)
        {
            status.MyElevators.ForEach(e => MoveElevator(e, "up"));
            Console.WriteLine(string.Format("Current turn {0} time to next turn {1}ms.", status.Turn, status.TimeUntilNextTurn));
            
        }
    }
}
