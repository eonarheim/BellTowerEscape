import AgentBase

class SampleAgent(AgentBase.AgentBase):

	def __init__(self, name, endpoint):
		self.myElevators = {}
		AgentBase.AgentBase.__init__(self, name, endpoint)

	def update(self, game_status):
		if not self.myElevators:
			for i, val in enumerate(game_status.my_bank):
				if i % 2 == 0:
					self.myElevators[val.id] = "MoveUp"
				else:
					self.myElevators[val.id] = "MoveDown"

		peopleGoingUp = []
		peopleGoingDown = []
		maxFloor = len(game_status.people_waiting)
		minFloor = 0

		for i, people_on_floor in enumerate(game_status.people_waiting):
			peopleGoingUp.append(people_on_floor.people_going_up)
			peopleGoingDown.append(people_on_floor.people_going_down)

		for elevator in game_status.my_bank:
			currentId = elevator.id
			currentFloor = elevator.floor
			drop_off = False
			pick_up = False
			for person in elevator.people:
				if currentFloor == person.desired_floor:
					drop_off = True

			if not drop_off:
				if self.myElevators[currentId] == "MoveUp":
					if currentFloor >= maxFloor:
						self.myElevators[currentId] = "MoveDown"
				elif self.myElevators[currentId] == "MoveDown":
					if currentFloor <= minFloor:
						self.myElevators[currentId] = "MoveUp"
				else:
					print("ruh roh")

			if self.myElevators[currentId] == "MoveUp":
				if peopleGoingUp[currentFloor] > 0:
					pick_up = True

			if self.myElevators[currentId] == "MoveDown":
				if peopleGoingDown[currentFloor] > 0:
					pick_up = True

			if drop_off:
				self.move_elevator(currentId, "stop")
			elif pick_up and elevator.free_space > 0:
				self.move_elevator(currentId, "stop")
			else:
				if self.myElevators[currentId] == "MoveUp":
					self.move_elevator(currentId, "up")
				elif self.myElevators[currentId] == "MoveDown":
					self.move_elevator(currentId, "down")
				else:
					print("Looks like this elevator doesn't have a function...")

agent = SampleAgent("Sample Python Agent", "http://localhost:3193/")
agent.start()



