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

    def get_turn_info(self):
        r = requests.get(self.__endpoint+'/api/game/{0}/turn'.format(self.game_id), headers=self.__headers)
        data = r.json
        self.time_to_next_turn = data['MillisecondsUntilNextTurn']
        return self._json_object_hook(data)

    def get_game_info(self):
        r = requests.post(self.__endpoint+'/api/game/{0}/status/{1}'.format(self.game_id,self.auth_token), headers=self.__headers)
        data = r.json()
        bank = self.__parse_elevator(data['MyBank'])
        people_waiting = map(self.__parse_people_waiting, data['PeopleWaiting'])
        self.time_to_next_turn = data['MillisecondsUntilNextTurn']

        return GameStatus(data['IsGameOver'], data['Status'], data['GameId'], data['Turn'],
                          data['MillisecondsUntilNextTurn'], bank, people_waiting)

    def move_elevator(self, elevator, function):
        duplicate_request = reduce(lambda prev, nxt: prev and nxt.elevator_id == elevator.id, self.__pending_move_requests, False)
        if duplicate_request:
            print "WARNING! A move request has already been issued for elevator {0}" .format(elevator.id)
            return False

        self.__pending_move_requests.append(MoveElevatorRequest(elevator, function))
        return True

    def update(self, game_state):
        pass #UPDATE GAME LOGIC HERE or in that extended class...!

    def start(self):
        self.logon()
        is_running = False

        if not is_running:

            is_running = True
            while is_running:

                gs = self.get_game_info()
                if gs.is_game_over:
                    is_running = False
                    print("Game Over!")
                    print(gs.status)
                    break

                self.update(gs)
                self.send_update(self.__pending_move_requests)
                self.__pending_move_requests = []

                if self.time_to_next_turn > 0:
                    sleep(self.time_to_next_turn/1000)

    def send_update(self, move_elevator_requests):
        data = json.dumps({'AuthToken': self.auth_token, 'GameId': self.game_id,
                           'MoveElevatorRequests': self.__serialize__elevator_requests(move_elevator_requests)})
        r = requests.post(self.__endpoint+'/api/game/update'.format(self.game_id, self.auth_token), data=data, headers=self.__headers)

    def __serialize__elevator_requests(self, move_elevator_requests):
        ret_data = []
        for move_elevator_request in move_elevator_requests:
            ret_data.append({'ElevatorID': move_elevator_request.elevator_id, 'Function': move_elevator_request.function})
        return ret_data

    def __parse_elevator(self, elevator_json):
        people = map(self.__parse_Meeple, elevator_json['People'])
        return Elevator(elevator_json['ElevatorID'], elevator_json['Floor'], elevator_json['NumberOfOccupants'], people)

    def __parse_Meeple(self, meeple_json):
        return Meeple(meeple_json['PersonID'], meeple_json['DesiredFloor'], meeple_json['CurrentTurn'])

    def __parse_people_waiting(self, people_waiting_json):
        return PeopleWaiting(people_waiting_json['Floor'], people_waiting_json['NumberOfPeopleWaiting'],
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
        self.number_of_occupants = number_of_occupants
        self.people = people

class Meeple:
    def __init__(self, unique_id, desired_floor, turns_frustration):
        self.id = unique_id
        self.desired_floor = desired_floor
        self.turns_frustration = turns_frustration

class PeopleWaiting:
    def __init__(self, floor, number_of_people, people_going_up, people_going_down):
        self.floor = floor
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

