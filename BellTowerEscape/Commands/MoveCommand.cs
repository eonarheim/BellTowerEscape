using BellTowerEscape.Server;

namespace BellTowerEscape.Commands
{
    public class MoveCommand : IGameCommand
    {
        public string AuthToken { get; set; }
        public int GameId { get; set; }
        public int ElevatorId { get; set; }
        public string Direction { get; set; }
    }
}