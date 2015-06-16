using System;
using BellTowerEscape.Commands;
using BellTowerEscape.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BellTowerEscape.Tests
{
    [TestClass]
    public class GameTest
    {
        [TestMethod]
        public void GameExists()
        {
            var game = new Game();
            Assert.IsNotNull(game, "Game should exist");
        }

        [TestMethod]
        public void GamesCanBeSeeded()
        {
            
        }
        
        [TestMethod]
        public void PlayersCanLogin()
        {
            var game = new Game();

            var result = game.LogonPlayer("MyCoolAgent");
            var garbage = new Guid();
            var parsedCorrectly = Guid.TryParse(result.AuthToken, out garbage);
            Assert.IsTrue(parsedCorrectly);

            var other = game.LogonPlayer("Player2");
            parsedCorrectly = Guid.TryParse(other.AuthToken, out garbage);
            Assert.IsTrue(parsedCorrectly);

            Assert.AreNotEqual(other.AuthToken, result.AuthToken);

            // If the same player logs on grab the same game id
            result = game.LogonPlayer("Player2");
            Assert.AreEqual(result.GameId, other.GameId);

        }

        [TestMethod]
        public void PlayersCanIssueOneCommandPerElevatorPerTurn()
        {
            var game = new Game();

            var logonResult = game.LogonPlayer("MyCoolAgent");

            var command = new MoveCommand()
            {
                AuthToken = logonResult.AuthToken,
                Direction = "UP",
                Id = 0
            };

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            Assert.AreEqual(elevator.Floor, 1);

            // should fail on the second attempt and not advance the elevator
            result = game.MoveElevator(command);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(elevator.Floor, 1);

            // should work on the other elevator
            game.Elevators.TryGetValue(1, out elevator);
            command.Id = 1;
            result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(elevator.Floor, 1);


        }

        [TestMethod]
        public void PlayersCanIssueMoveUpCommands()
        {
            var game = new Game();

            var logonResult = game.LogonPlayer("MyCoolAgent");

            var command = new MoveCommand()
            {
                AuthToken = logonResult.AuthToken,
                Direction = "UP",
                Id = 0
            };

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            Assert.AreEqual(elevator.Floor, 1);

            // should fail on the second attempt and not advance the elevator
            result = game.MoveElevator(command);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(elevator.Floor, 1);

            // should work on the other elevator
            game.Elevators.TryGetValue(1, out elevator);
            command.Id = 1;
            result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(elevator.Floor, 1);

        }

        [TestMethod]
        public void PlayerCanIssueMoveDownCommand()
        {
            
        }

        [TestMethod]
        public void PlayersCanLoadUnloadElevators()
        {
            
        }

        [TestMethod]
        public void PeoplePreferEmptierElevators()
        {
            
        }

        [TestMethod]
        public void PeopleWillBeFrustratedAfterXFloors()
        {
            
        }

        [TestMethod]
        public void PlayersCanWin()
        {
            
        }

        [TestMethod]
        public void PlayersCanMove()
        {
            var game = new Game();

            var logonResult = game.LogonPlayer("MyCoolAgent");

            var command = new MoveCommand()
            {
                AuthToken = logonResult.AuthToken,
                Direction = "UP",
                Id = 0
            };

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            Assert.AreEqual(elevator.Floor, 1);

        }

        [TestMethod]
        public void TurnsAdvanceAfterTime()
        {
            var game = new Game();
            Assert.AreEqual(game.Turn, 0);
            game.Update(Game.TURN_DURATION);
            game.Update(Game.SERVER_PROCESSING);
            Assert.AreEqual(game.Turn, 1);

            game.Update(Game.TURN_DURATION);
            Assert.AreEqual(game.Turn, 1);
            game.Update(Game.SERVER_PROCESSING);
            Assert.AreEqual(game.Turn, 2);
        }

        [TestMethod]
        public void GameEndsAfterMaxTurns()
        {
            var game = new Game();
            Assert.AreEqual(game.Turn, 0);
            for (var i = 0; i < Game.MAX_TURN; i++)
            {
                game.Update(Game.TURN_DURATION);
                game.Update(Game.SERVER_PROCESSING);
            }
            Assert.AreEqual(game.Turn, Game.MAX_TURN);
            Assert.AreEqual(game.GameOver, true);
        }
    }
}
