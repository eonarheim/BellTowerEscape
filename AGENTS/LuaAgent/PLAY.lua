local http = require("socket.http")
local json = require("dkjson")
local ltn12 = require("ltn12")
local ffi = require("ffi")

-- allows us to use C std lib's Sleep(Windows)/Poll(osx/linux) function!
ffi.cdef[[
void Sleep(int ms);
int poll(struct pollfd *fds, unsigned long nfds, int timeout);
]]

local sleep
if ffi.os == "Windows" then
  function sleep(ms)
    ffi.C.Sleep(ms)
  end
else
  function sleep(ms)
    ffi.C.poll(nil, 0, ms)
  end
end

-- Internal class constructor
local class = function(...)
    local klass = {}
    klass.__index = klass
    klass.__call = function(_,...) return klass:new(...) end
    function klass:new(...)
        local instance = setmetatable({}, klass)
        klass.__init(instance, ...)
        return instance
    end
    return setmetatable(klass,{__call = klass.__call})
end

-- http request handler
local function httpRequest(url, method, header, data)
    --handle when no data is sent in
    local data = data or ""

    -- encode data as a json string
    local jsonString = json.encode(data)
    local source = ltn12.source.string(jsonString)

    -- create a response table
    local response = {}
    local save = ltn12.sink.table(response)

    -- add datasize to header
    local jsonsize = #jsonString
    local sizeHeader = header
    sizeHeader["content-length"] = jsonsize

    -- REQUEST IT!
    ok, code, headers = http.request{url = url, method = method, headers = sizeHeader, source = source, sink = save}

    if code ~= 200 then
        print("Error Code:", code, table.concat(response, "\n\n\n"))
        print(url)
        print(jsonString)
        sleep(4000)
    end

    return json.decode(table.concat(response))
end

-- could also be used to store info about elevators, if that's what you're into
local bank = class()
function bank:__init(gameStateElevators)
    self.elevators = {}
    for i = 1, #gameStateElevators do
        self.elevators[gameStateElevators[i].Id] = {}
        if i % 2 == 0 then
            self.elevators[gameStateElevators[i].Id].Function = "MoveUp"
        else
            self.elevators[gameStateElevators[i].Id].Function = "MoveDown"
        end
            
    end
end

-- initialize a client class
local client = class()
function client:__init(name)
    self.url = "http://localhost"
    self.headers = {["Accept"] = "application/json", ["Content-Type"] = "application/json"}
    
    self.pendingMoves = {} -- sample { {ElevatorID = 1, Function = "left"}, {ElevatorID = 2, Function = "right"} }
    
    self.GameId = nil
    self.timeToNextTurn = 0
    self.AuthToken = nil
    self.name = name

    self.bank = nil
end

function client:logon()
    local logon = { ["AgentName"] = self.name}
    local response = httpRequest(self.url .. "/api/game/logon", "POST", self.headers, logon)
    self.AuthToken = response.AuthToken
    self.GameId = response.GameId
    print("GameId", self.GameId)
end

function client:getTurnInfo()
    local turnInfo = string.format("/api/game/%d/turn",  self.GameId)
    local response = httpRequest(self.url .. turnInfo, "GET", self.headers)
    print(response.MillisecondsUntilNextTurn, "ms left")
    self.timeToNextTurn = response.MillisecondsUntilNextTurn
end

function client:getGameInfo()
    local game = string.format("/api/game/%d/status/%s", self.GameId, self.AuthToken)
    local data = httpRequest(self.url .. game, "POST", self.headers)
    self.timeToNextTurn = data.MillisecondsUntilNextTurn
    return data
end


function client:updateBanks(gameState)

    -- ELEVATOR SWEEP ALGO:
    --[[
        Each elevator is set to move up or down. As it goes in that direction, if there are other people to pick up going in that direction,
        those people are picked up. Once everyone in that direction is picked up, the direction changes and the elvator keeps going from there.
    --]]

    -- create the bank the first time around
    if not self.bank then
        self.bank = bank:new(gameState.MyBank)
    end

    -- figure out which floors people are going up/down on
    local peopleGoingUp = {}
    local peopleGoingDown = {}
    local maxFloor = 0
    local minFloor = math.huge

    for i, peopleOnFloor in ipairs(gameState.PeopleWaiting) do
        peopleGoingUp[peopleOnFloor.Floor] = peopleOnFloor.GoingUp
        peopleGoingDown[peopleOnFloor.Floor] = peopleOnFloor.GoingDown

        -- used when we want to switch elevator directions
        if peopleOnFloor.Floor > maxFloor then
            maxFloor = peopleOnFloor
        end

        if peopleOnFloor.Floor < minFloor then
            minFloor = peopleOnFloor.Floor
        end
    end

    for i, elevator in ipairs(gameState.MyBank) do
        local currentId = elevator.Id
        local currentFloor = elevator.Floor
            -- go through every person and see if they want to get off here
            local TransferPeople = false
            for i, person in ipairs(elevator.People) do
                if currentFloor == person.DesiredFloor then
                    TransferPeople = true
                end
            end

            -- See if we should pick up people on this floor heading up
            if self.bank.elevators[currentId].Function == "MoveUp" then
                if peopleGoingUp[currentFloor] and peopleGoingUp[currentFloor] > 0 then
                    TransferPeople = true
                end
            end

            -- See if we should pick up people on this floor heading down
            if self.bank.elevators[currentId].Function == "MoveDown" then
                if peopleGoingDown[currentFloor] and peopleGoingDown[currentFloor] > 0 then
                    TransferPeople = true
                end
            end

            -- ONE LAST THING. Figure out if we should swap this elevator's directions
            if not TransferPeople then
                if self.bank.elevators[currentId].Function == "MoveUp" then
                    if currentFloor >= maxFloor then
                        self.bank.elevators[currentId].Function = "MoveDown"
                    end
                elseif self.bank.elevators[currentId].Function == "MoveDown" then
                    if currentFloor <= minFloor then
                        self.bank.elevators[currentId].Function = "MoveUp"
                    end
                else
                    error("uhhhhhh. This elevator doesn't have a Functions?! WHAT YOU DO?!?!?!")
                end
            end

            if TransferPeople then 
                self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "TransferPeople"}
            else

                if self.bank.elevators[currentId].Function == "MoveUp" then
                    self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "MoveUp"}

                elseif self.bank.elevators[currentId].Function == "MoveDown" then
                    self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "MoveDown"}

                else
                    error("uhhhhhh. This elevator doesn't have a Functions?! WHAT YOU DO?!?!?!")
                end
            end
    end


end

function client:update(gameState)
    -- LAGS BRO
    self:getTurnInfo()

    -- figure out that bank!
    self:updateBanks(gameState)

    -- update turn info
    self:getTurnInfo()
end

function client:sendUpdate()
    local update = { AuthToken = self.AuthToken, GameId = self.GameId, MoveRequest = self.pendingMoves }
    local response = httpRequest(self.url .. "/api/game/update", "POST", self.headers, update)
    return response
end

function client:start()
    self:logon()
    local isRunning = true
    
    while isRunning do
        print("NEW TURN")
        local gameState = self:getGameInfo()
        if gameState.IsGameOver then
            isRunning = false
            print("the game is over")
            print(gameState.Status)
            break
        end

        -- update that game!
        self:update(gameState)

        -- send dem moves
        self:sendUpdate()

        -- clear the pending after sending
        self.pendingMoves = {}
        if self.timeToNextTurn > 0 then
            sleep(self.timeToNextTurn)
        end
    end

end

local agent = client:new("SampleLuaAgent")
agent:start()
