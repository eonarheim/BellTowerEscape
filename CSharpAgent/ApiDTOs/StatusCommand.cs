using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent.ApiDTOs
{
    public class StatusCommand
    {
        public string AuthToken { get; set; }
        public int GameId { get; set; }
    }
}
