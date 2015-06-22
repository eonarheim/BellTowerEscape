import requests
import json
from time import sleep


class AgentBase:

    def __init__(self, name, endpoint):
        self.__headers = {"Accept": "application/json", "Content-Type": "application/json"}
        self.__endpoint = endpoint
        self.__pending_move_requests = []
        self.game_id = None
        self.time_to_next_turn = 0
        self.auth_token = None
        self.name = name

    def logon(self):
        params = {'AgentName': self.name}
        r = requests.post(self.__endpoint+'/api/game/logon', data=json.dumps(params), headers=self.__headers)
        self.game_id = r.json()['GameId']
        self.auth_token = r.json()['AuthToken']

    def get_game_info(self):
        params = {'AuthToken': self.auth_token, 'GameId': self.game_id}
        r = requests.post(self.__endpoint+'/api/game/status', data=json.dumps(params), headers=self.__headers)
        data = r.json()
        bank = map(self.__parse_elevator, data['MyElevators'])
        people_waiting = map(self.__parse_people_waiting, data['Floors'])
        self.time_to_next_turn = data['TimeUntilNextTurn']

        return GameStatus(data['IsGameOver'], data['Status'], data['Id'], data['Turn'],
                          data['TimeUntilNextTurn'], bank, people_waiting)

    def move_elevator(self, elevator, function):
        data = json.dumps({'AuthToken': self.auth_token, 'GameId': self.game_id,
                           'ElevatorId': elevator, 'Direction': function})
        r = requests.post(self.__endpoint+'/api/game/move', data=data, headers=self.__headers)
        print(r.json())

    def update(self, game_state):
        pass #UPDATE GAME LOGIC HERE or in that extended class...!

    def start(self):
        self.logon()
        is_running = False

        if not is_running:

            is_running = True
            while is_running:
                print("NEW TURN")
                gs = self.get_game_info()
                if gs.is_game_over:
                    is_running = False
                    print("Game Over!")
                    print(gs.status)
                    break

                self.update(gs)
                gs = self.get_game_info()
                if self.time_to_next_turn > 0:
                    sleep(self.time_to_next_turn/1000)

    def __serialize__elevator_requests(self, move_elevator_requests):
        ret_data = []
        for move_elevator_request in move_elevator_requests:
            ret_data.append({'ElevatorID': move_elevator_request.elevator_id, 'Function': move_elevator_request.function})
        return ret_data

    def __parse_elevator(self, elevator_json):
        people = map(self.__parse_Meeple, elevator_json['Meeples'])
        return Elevator(elevator_json['Id'], elevator_json['Floor'], elevator_json['FreeSpace'], people)

    def __parse_Meeple(self, meeple_json):
        return Meeple(meeple_json['Id'], meeple_json['Destination'], meeple_json['Patience'])

    def __parse_people_waiting(self, people_waiting_json):
        return PeopleWaiting(people_waiting_json['NumberOfMeeple'],
                            people_waiting_json['GoingUp'], people_waiting_json['GoingDown'])

#DTOs
class MoveElevatorRequest:
    def __init__(self, elevator_id, function):
        self.function = function
        self.elevator_id = elevator_id

class Elevator:
    def __init__(self, unique_id, floor, number_of_occupants, people):
        self.id = unique_id
        self.floor = floor
        self.free_space = number_of_occupants
        self.people = people

class Meeple:
    def __init__(self, unique_id, desired_floor, turns_frustration):
        self.id = unique_id
        self.desired_floor = desired_floor
        self.turns_frustration = turns_frustration

class PeopleWaiting:
    def __init__(self, number_of_people, people_going_up, people_going_down):
        self.number_of_people = number_of_people
        self.people_going_up = people_going_up
        self.people_going_down = people_going_down

class GameStatus:
    def __init__(self, is_game_over, status, game_id, turn, milliseconds_until_next_turn, my_bank, people_waiting):
        self.is_game_over = is_game_over
        self.status = status
        self.game_id = game_id
        self.turn = turn
        self.milliseconds_until_next_turn = milliseconds_until_next_turn
        self.my_bank = my_bank
        self.people_waiting = people_waiting

