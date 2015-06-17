using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent.ApiDTOs
{
    public class LogonResult
    {
        public string AuthToken { get; set; }
        public int GameId { get; set; }
        public int GameStart { get; set; }
    }
}
