using System;
using System.Collections.Generic;
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
                ElevatorId = 0
            };

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            elevator.Floor = 0;

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            game.Elevators.TryGetValue(0, out elevator);
            Assert.AreEqual(elevator.Floor, 1);

            // should fail on the second attempt and not advance the elevator
            result = game.MoveElevator(command);
            Assert.IsFalse(result.Success);
            Assert.AreEqual(elevator.Floor, 1);

            // should work on the other elevator
            game.Elevators.TryGetValue(1, out elevator);
            command.ElevatorId = 1;
            elevator.Floor = 0;
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
                ElevatorId = 0
            };

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            elevator.Floor = 3;

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            game.Elevators.TryGetValue(0, out elevator);
            Assert.AreEqual(elevator.Floor, 4);

        }

        [TestMethod]
        public void PlayerCanIssueMoveDownCommand()
        {
            var game = new Game();

            var logonResult = game.LogonPlayer("MyCoolAgent");

            var command = new MoveCommand()
            {
                AuthToken = logonResult.AuthToken,
                Direction = "Up",
                ElevatorId = 0
            };

            game.Update(Game.START_DELAY);

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            //Assert.AreEqual(elevator.Floor, 1);
            // elevators now start on a random floor.
            int oldFloor = elevator.Floor;

            game.Update(Game.TURN_DURATION);
            game.Update(Game.SERVER_PROCESSING);
            command.Direction = "Down";

            result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);
            game.Elevators.TryGetValue(0, out elevator);
            if (oldFloor > 0)
            {
                Assert.AreEqual(elevator.Floor, oldFloor - 1);
            }
            else if (oldFloor == 0)
            {
                Assert.AreEqual(elevator.Floor, 0);
            }
            else
            {
                // not possible
                Assert.Fail();
            }

        }

        [TestMethod]
        public void PlayersCanLoadElevatorsFairly()
        {
            var game = new Game();

            var logonResult = game.LogonPlayer("MyCoolAgent");

            var command = new MoveCommand()
            {
                AuthToken = logonResult.AuthToken,
                Direction = "stop",
                ElevatorId = 0
            };

            game.Update(Game.START_DELAY);
            
            game.Elevators[0].Floor = 0;
            game.Elevators[1].Floor = 0;
            var e0 = game.Elevators[0];
            var e1 = game.Elevators[1];
            e0.Meeples.Add(new Meeple(game, game.Floors[0]));
            e0.Meeples.Add(new Meeple(game, game.Floors[0]));

            game.Elevators[2].Floor = 2;
            game.Elevators[3].Floor = 2;

            var meeples = new List<Meeple>()
            {
                new Meeple(game, game.Floors[0]){Destination = 10},
                new Meeple(game, game.Floors[0]){Destination = 7}
            };
            game.Floors[0].Meeples = meeples;
            
            

            var result = game.MoveElevator(command);
            command.ElevatorId = 1;
            game.MoveElevator(command);

            foreach (var meeple in meeples)
            {
                Assert.IsFalse(meeple.InElevator);
            }

            game.Update(Game.TURN_DURATION);
            game.Update(Game.SERVER_PROCESSING);

            Assert.AreEqual(game.Floors[0].Number, 0);
            Assert.IsTrue(result.Success);
            Assert.AreEqual(e0.IsStopped, true);
            Assert.AreEqual(e0.Floor, 0);
            Assert.AreEqual(e0.Meeples.Count, 2);

            Assert.AreEqual(e1.IsStopped, true);
            Assert.AreEqual(e1.Floor, 0);
            Assert.AreEqual(e1.Meeples.Count, 2);
            foreach (var meeple in e1.Meeples)
            {
                Assert.IsTrue(meeple.InElevator);
            }
            
            Assert.AreEqual(game.Floors[0].Meeples.Count, 0);
        }

        [TestMethod]
        public void PlayersCantMoveElevatorsTheyDontOwn()
        {
            var game = new Game();

            var logonResult = game.LogonPlayer("MyCoolAgent");

            var command = new MoveCommand()
            {
                AuthToken = logonResult.AuthToken,
                Direction = "up",
                ElevatorId = 2
            };

            var result = game.MoveElevator(command);
            Assert.IsFalse(result.Success);

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
                ElevatorId = 0
            };

            Elevator elevator;
            game.Elevators.TryGetValue(0, out elevator);
            elevator.Floor = 0;

            var result = game.MoveElevator(command);
            Assert.IsTrue(result.Success);

            game.Elevators.TryGetValue(0, out elevator);
            Assert.AreEqual(elevator.Floor, 1);

        }

        [TestMethod]
        public void TurnsAdvanceAfterTime()
        {
            var game = new Game();
            game.Update(Game.START_DELAY);
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
            game.Update(Game.START_DELAY);
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
