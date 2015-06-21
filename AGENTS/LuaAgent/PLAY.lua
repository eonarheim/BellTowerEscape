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
    print("RESPONS", response[1])
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
function client:__init(name, url)
    self.url = url
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
    print("AuthToken", self.AuthToken)
end

function client:getTurnInfo()
    local turnInfo = string.format("/api/game/%d/turn",  self.GameId)
    local response = httpRequest(self.url .. turnInfo, "GET", self.headers)
    print(response.TimeUntilNextTurn, "ms left")
    self.timeToNextTurn = response.MillisecondsUntilNextTurn
end

function client:getGameInfo()
    local gameInfo = { ["AuthToken"] = self.AuthToken, ["GameId"] = self.GameId }
    local game = "/api/game/status"
    local data = httpRequest(self.url .. game, "POST", self.headers, gameInfo)
    self.timeToNextTurn = data.TimeUntilNextTurn
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
        self.bank = bank:new(gameState.MyElevators)
    end

    -- figure out which floors people are going up/down on
    local peopleGoingUp = {}
    local peopleGoingDown = {}
    local maxFloor = 0
    local minFloor = math.huge

    print("TOTAL FLOORS", #gameState.Floors)
    for i, peopleOnFloor in ipairs(gameState.Floors) do
        local currentFloor = i - 1
        peopleGoingUp[currentFloor] = peopleOnFloor.GoingUp
        peopleGoingDown[currentFloor] = peopleOnFloor.GoingDown

        -- used when we want to switch elevator directions
        if currentFloor > maxFloor then
            maxFloor = currentFloor
        end

        if currentFloor < minFloor then
            minFloor = currentFloor
        end
    end

    for i, elevator in ipairs(gameState.MyElevators) do
        local currentId = elevator.Id
        local currentFloor = elevator.Floor 
            
            local DropOff = false
            local PickUp = false
            
            -- go through every person and see if they want to get off here
            for i, person in ipairs(elevator.Meeples) do
                if currentFloor == person.DesiredFloor then
                    DropOff = true
                end
            end

            -- See if we should pick up people on this floor heading up
            if self.bank.elevators[currentId].Function == "MoveUp" then
                if peopleGoingUp[currentFloor] > 0 then
                    PickUp = true
                end
            end

            -- See if we should pick up people on this floor heading down
            if self.bank.elevators[currentId].Function == "MoveDown" then
                if peopleGoingDown[currentFloor] > 0 then
                    PickUp = true
                end
            end

            -- ONE LAST THING. Figure out if we should swap this elevator's directions
            if not (PickUp or DropOff) then
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

            if DropOff then 
                self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "stop"}
            if PickUp and elevator.FreeSpace > 0 then
                self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "stop"}
            else

                if self.bank.elevators[currentId].Function == "MoveUp" then
                    self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "up"}

                elseif self.bank.elevators[currentId].Function == "MoveDown" then
                    self.pendingMoves [ #self.pendingMoves+1 ] = {ElevatorID = currentId, Function = "down"}

                else
                    error("uhhhhhh. This elevator doesn't have a Functions?! WHAT YOU DO?!?!?!")
                end
            end
    end


end

function client:update(gameState)
    -- LAGS BRO
    --self:getTurnInfo()

    -- figure out that bank!
    self:updateBanks(gameState)

    -- update turn info
    self:getGameInfo()
end

function client:sendUpdate()
    for i, move in ipairs(self.pendingMoves) do
        local update = { AuthToken = self.AuthToken, GameId = self.GameId, ElevatorId = move.ElevatorID, Direction = move.Function }
        local response = httpRequest(self.url .. "/api/game/move", "POST", self.headers, update)
    end

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

local agent = client:new("SampleLuaAgent", "http://localhost:3193")
agent:start()
