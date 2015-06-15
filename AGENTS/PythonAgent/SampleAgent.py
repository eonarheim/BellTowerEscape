import AgentBase

class SampleAgent(AgentBase.AgentBase):

    def __init__(self, name, endpoint):
    	self.myElevators = {}
        AgentBase.AgentBase.__init__(self, name, endpoint)

    def update(self, game_status):
    	if not self.myElevators:
    		for i, val in enumerate(game_status.my_bank):
    			if i % 2 == 0:
    				self.myElevators[val.Id] = "MoveUp"
    			else:
    				self.myElevators[val.Id] = "MoveDown"

    	peopleGoingUp = []
    	peopleGoingDown = []
    	maxFloor = 0
    	minFloor = float("inf")

    	for people_on_floor in game_status.people_waiting:
    		peopleGoingUp[people_on_floor.floor] = people_on_floor.people_going_up
    		peopleGoingDown[people_on_floor.floor] = people_on_floor.people_going_down
    		if people_on_floor.floor > maxFloor:
    			maxFloor = people_on_floor
    		if people_on_floor < minFloor:
    			minFloor = people_on_floor

    	for elevator in game_status.my_bank:
    		currentId = elevator.id
    		currentFloor = elevator.floor
    		transferPeople = False
    		for person in elevator.people:
    			if currentFloor == person.desired_floor:
    				transferPeople = True

    		if self.myElevators[currentId] == "MoveUp":
    			if peopleGoingUp[currentFloor] and peopleGoingUp[currentFloor] > 0:
    				transferPeople = True

    		if self.myElevators[currentId] == "MoveDown":
    			if peopleGoingDown[currentFloor] and peopleGoingDown[currentFloor] > 0:
    				transferPeople = True

    		if not transferPeople:
    			if self.myElevators[currentId] == "MoveUp":
    				if currentFloor >= maxFloor:
    					self.myElevators[currentId] = "MoveDown"
    			else if self.myElevators[currentId] == "MoveDown":
    				if currentFloor <= minFloor:
    					self.myElevators[currentId] = "MoveUp"
    			else
    				print("ruh roh")

    		if transferPeople:
    			self.move_elevator(currentId, "TransferPeople")
    		else
    			if self.myElevators[currentId] == "MoveUp":
    				self.move_elevator(currentId, "MoveUp")
    			else if self.myElevators[currentId] == "MoveDown":
    				self.move_elevator(currentId, "MoveDown")
    			else
    				print("Looks like this elevator doesn't have a function...")

agent = SampleAgent("Sample Python Agent", "http://localhost/")
agent.start()



