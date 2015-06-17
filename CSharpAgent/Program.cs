using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            var endpoint = "http://localhost:3193/";
            if (args.Length == 2)
            {
                endpoint = args[1];
            }
            try
            {
                var agent = new Agent("Mr. Roboto", endpoint);
                agent.Start().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("Ooops! Something went wrong! {0}", e.Message);
            }
        }
    }
}
