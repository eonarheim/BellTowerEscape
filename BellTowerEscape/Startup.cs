using Microsoft.Owin;
using Owin;
using BellTowerEscape;

[assembly: OwinStartup(typeof(BellTowerEscape.Startup))]
namespace BellTowerEscape
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}