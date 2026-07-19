using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ToxicPlate : PlateBase {

    /// <summary> Representation, for every starting room of Void, of the path it will take around the grid.
    /// Those paths loop. </summary>
    readonly int[][] VoidPathsFromStartingLocation = new int[23][]
    {
        new int[]{0, 1, 6, 8, 12},
        new int[]{1, 2, 3, 5},
        new int[]{2, 3, 5, 1},
        new int[]{3, 5, 1, 2},
        new int[]{4, 11, 14, 10, 5},
        new int[]{5, 3, 2, 1, 0, 12, 8, 16, 21, 17, 22, 19, 15, 11, 4},
        new int[]{6, 1, 5, 10, 9, 7},
        new int[]{7, 9, 13, 20, 17, 16, 8, 6},
        new int[]{8, 12, 0, 1, 6},
        new int[]{9, 7, 6, 1, 5, 10},
        new int[]{10, 5, 4, 11, 14},
        new int[]{11, 4, 5, 3, 2, 1, 0, 12, 8, 16, 21, 17, 22, 19, 15},
        new int[]{12, 0, 1, 6, 8},
        new int[]{13, 9, 10, 14, 11, 18},
        new int[]{14, 10, 5, 4, 11},
        new int[]{15, 19, 22, 13, 18, 11},
        new int[]{16, 8, 6, 7, 9, 13, 20, 17},
        new int[]{17, 22, 19, 15, 11, 4, 5, 3, 2, 1, 0, 12, 8, 16, 21},
        new int[]{18, 11, 15, 19, 22, 13},
        new int[]{19, 15, 11, 4, 5, 3, 2, 1, 0, 12, 8, 16, 21, 17, 22},
        new int[]{20, 13, 22, 17},
        new int[]{21, 17, 22, 19, 15, 11, 4, 5, 3, 2, 1, 0, 12, 8, 16},
        new int[]{22, 13, 18, 11, 15, 19}
    };

    /// <summary> Array containing, for each room, its connections in order North East South West. -1 represents no door. </summary>
    readonly int[][] connectedRoomsFromIndices = new int[23][]
    {
        new int[4]{ -1, 1, 12, -1 },
        new int[4]{ 2, 5, 6, 0 },
        new int[4]{ -1, 3, 1, -1 },
        new int[4]{ -1, -1, 5, 2 },
        new int[4]{ -1, -1, 11, 5 },
        new int[4]{ 3, 4, 10, 1 },
        new int[4]{ 1, 7, -1, 8 },
        new int[4]{ -1, -1, 9, 6 },
        new int[4]{ 12, 6, 16, -1 },
        new int[4]{ 7, 10, 13, -1 },
        new int[4]{ 5, -1, 14, 9 },
        new int[4]{ 4, 15, 18, 14 },
        new int[4]{ 0, -1, 8, -1 },
        new int[4]{ 9, 18, 22, 20 },
        new int[4]{ 10, 11, -1, -1 },
        new int[4]{ -1, -1, 19, 11 },
        new int[4]{ 8, 17, -1, 21 },
        new int[4]{ 22, 20, 21, 16 },
        new int[4]{ 11, -1, -1, 13 },
        new int[4]{ 15, -1, -1, 22 },
        new int[4]{ -1, 13, -1, 17 },
        new int[4]{ 17, 16, -1, -1 },
        new int[4]{ 13, 19, 17, -1 }
    };

    /// <summary> Starting locations of Voids </summary>
    List<int> startingVoidLocationsIndices = new List<int>();
    /// <summary> Starting locations of the Toxic Crystals </summary>
    List<int> startingCrystalsLocationIndices = new List<int>();
    /// <summary> Starting location of the Player </summary>
    int startingPlayerLocationIndex;

    /// <summary> Current location of the Player </summary>
    int currentPlayerLocationIndex;
    /// <summary> Current locations of the Toxic Crystals. They do not move around, but this stores which ones are left to collect. </summary>
    List<int> currentCrystalsLocationIndices = new List<int>();

    /// <summary> As Voids move around, how far into their movement loop are they?
    /// Used as an index for lookup in VoidPathsFromStartingLocation.</summary>
    int[] currentIndexInPathForVoids;

    [SerializeField] TextMesh topStartingVoidRoomsTextMesh;
    [SerializeField] TextMesh middleToxicCrystalsRoomsTextMesh;
    [SerializeField] TextMesh bottomStartingLocationTextMesh;


    Coroutine movementVibrationCoroutine;

    // Universal Logging Data
    static int moduleIdCounter = 1;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PressedMovementInput(MovementDirection.Up); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressedMovementInput(MovementDirection.Down); return false; };
        platePressableButtons[2].OnInteract += delegate () { PressedMovementInput(MovementDirection.Left); return false; };
        platePressableButtons[3].OnInteract += delegate () { PressedMovementInput(MovementDirection.Right); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        InitializePuzzle();
    }

    



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void PressedMovementInput(MovementDirection direction)
    {
        platePressableButtons[0].AddInteractionPunch(0.5f);
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        summoningModule.ModuleLog(moduleId, "Pressed button {0}.", direction.ToString());

        // Movement as index is Up Down Left Right since that's what's used in other Plate modules
        // However linked rooms in the Rooms Connected is Up Right Down Left because that is clockwise and makes it easier "turning" clockwise when needed
        // So some slight conversion at the start is needed
        int _roomDirectionIndexInitiallyTaken = -1;
        switch (direction)
        {
            case MovementDirection.Up:
                _roomDirectionIndexInitiallyTaken = 0;
                break;

            case MovementDirection.Down:
                _roomDirectionIndexInitiallyTaken = 2;
                break;

            case MovementDirection.Left:
                _roomDirectionIndexInitiallyTaken = 3;
                break;

            case MovementDirection.Right:
                _roomDirectionIndexInitiallyTaken = 1;
                break;
        }


        // Determine which room to move to
        int _roomIndexToMoveTo = connectedRoomsFromIndices[currentPlayerLocationIndex][_roomDirectionIndexInitiallyTaken];


        // Trying to move in a wall, shake the plate and do nothing
        if (_roomIndexToMoveTo == -1)
        {
            // Memory management & Coroutine cancellation just in case
            if (movementVibrationCoroutine != null)
            { StopCoroutine(movementVibrationCoroutine); }

            // Shake the plate
            movementVibrationCoroutine = StartCoroutine(VibratePlate(2f));

            summoningModule.ModuleLog(moduleId, "Trying to move {0} from room {1}, but there is no opening!",
                GetPlayerFacingDirectionName(_roomDirectionIndexInitiallyTaken), currentPlayerLocationIndex);

            return;
        }



        // We know where to move
        // If not Voided, just move there
        if (voidedCellsIndices.Contains(_roomIndexToMoveTo) == false)
        {
            MovePlayerToNewRoom(_roomIndexToMoveTo);
            return;
        }

        // Else, it is Voided, so we need to loop until we find a non-Voided room room.
        MovePlayerToNewRoom(DeterminePlayerMovementThroughVoid(currentPlayerLocationIndex, _roomDirectionIndexInitiallyTaken, _roomIndexToMoveTo));
    }

    protected override void CasingTextButtonGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        summoningModule.ModuleLog(moduleId, "TOXIC text button pressed. Resetting the module to its initial state.");
        ResetValuesToStart();
    }

    int DeterminePlayerMovementThroughVoid(int startingRoom, int desiredMovementDirection, int initialRoomToMoveTo)
    {
        // Failsafe Iteration Count to avoid infinite loops because I do NOT trust while loops
        int _failsafeIterationCount = 0;

        // What room we currently are in. Starts Voided.
        // Is the one that we move through as we step through potentially multiple Voids in a row.
        int _currentTargetRoom = initialRoomToMoveTo;

        // What room we moved FROM, to know the previous step as well
        int _previousRoom = startingRoom;

        // What direction we travelled TO, from the previous room.
        int _previousMovementDirectionTaken = desiredMovementDirection;


        // Loop until the room we want to move to is not Voided
        while (voidedCellsIndices.Contains(_currentTargetRoom))
        {
            // Failsafe Iteration Count
            _failsafeIterationCount++;
            if (_failsafeIterationCount > 25)
            {
                summoningModule.ModuleLogError(moduleId, "HELP! GOT OVER 25 ITERATIONS TRYING TO FIND NON-VOID IN DIRECTION {0} FROM {1}!!!! Cancelling Movement.",
                    GetPlayerFacingDirectionName(desiredMovementDirection), startingRoom);
                return startingRoom;
            }

            summoningModule.ModuleLog(moduleId, "Trying to move to room {0} which is Voided.", _currentTargetRoom);


            // If only one other door exists, exit through it.

            // This line checks if there are two "-1" outputs on the current room
            if (connectedRoomsFromIndices[_currentTargetRoom].Where(x => x == -1).Count() == 2)
            {
                // If true, we only have one other possible exit, so take it.

                // save where we came from
                int _cachedPrevious = _currentTargetRoom;

                // Get the one value that is not -1 nor the room we enter FROM
                _currentTargetRoom = connectedRoomsFromIndices[_currentTargetRoom].Where(x => x != -1 && x != _previousRoom).First();

                // Update previous room
                _previousRoom = _cachedPrevious;

                // Updating the Direction we moved to
                _previousMovementDirectionTaken = Array.IndexOf(connectedRoomsFromIndices[_previousRoom], _currentTargetRoom);

                summoningModule.ModuleLog(moduleId, "Only one other exit was found, to room {0}", _currentTargetRoom);
                continue;
            }



            // Else, exit through the door cardinally opposite the one you entered from

            // If we leave room A through the West, we try to leave using voided room B's West exit, so it's actually the same direction.
            // It's cardinally opposite from the direction *we entered from* which is already cardinally opposite from the direction we took
            // (leaving A from its West enters B from its East)
            if (connectedRoomsFromIndices[_currentTargetRoom][_previousMovementDirectionTaken] != -1)
            {
                // save where we came from
                int _cachedPrevious = _currentTargetRoom;

                _currentTargetRoom = connectedRoomsFromIndices[_currentTargetRoom][_previousMovementDirectionTaken];

                // Update previous room
                _previousRoom = _cachedPrevious;

                // No need to update the _previousMovementDirectionTaken since it's the same

                summoningModule.ModuleLog(moduleId, "A straight forward exit was found, to room {0}", _currentTargetRoom);
                continue;
            }


            // Else, exit through the door that is cardinally clockwise.
            // We entered from a direction. We know that out of the 3 other possibles, we have at least 2 opened (if only 1, we would've taken it)
            // That we cannot have the one in the same direction opened either (otherwise we would've taken it)
            // The only way we get in this situation is if the Voided Room we are in has a split path, a T-junction
            // In this case, take the one clockwise relative to the direction taken (North takes East, East takes South, South takes West, West takes North)
            if (connectedRoomsFromIndices[_currentTargetRoom][(_previousMovementDirectionTaken + 1)%4] != -1)
            {

                // save where we came from
                int _cachedPrevious = _currentTargetRoom;

                // Get the one value that is not -1 nor the room we enter FROM
                _currentTargetRoom = connectedRoomsFromIndices[_currentTargetRoom][(_previousMovementDirectionTaken + 1) % 4];

                // Update previous room
                _previousRoom = _cachedPrevious;

                // Updating the Direction we moved to
                _previousMovementDirectionTaken = (_previousMovementDirectionTaken + 1) % 4;

                summoningModule.ModuleLog(moduleId, "Only a cardinally clockwise exit was found, to room {0}", _currentTargetRoom);
                continue;
            }


            // If we are here, it means that somehow:
            // We landed in a Voided Room
            // This Voided room had more than 1 other opening (so 3 or 4 openings total)
            // This Voided room had the opening in the same direction closed (so not 4 openings, so 3 max)
            // And this Voided room had the clockwise opening closed (so less than 3 opened)
            // So while logically impossible, I still want to handle this case
            summoningModule.ModuleLogError(moduleId, "Something went terribly wrong. We somehow ended in a room with 3 openings but that has 2 openings. Room is {0}, Movement Direction is {1}. Returning starting room.",
                _currentTargetRoom, _previousMovementDirectionTaken);
            return startingRoom;
        }



        return _currentTargetRoom;
    }

    void MovePlayerToNewRoom(int newRoomIndex)
    {
        // Move Player
        summoningModule.ModuleLog(moduleId, "Moving to Room {0}", newRoomIndex);

        currentPlayerLocationIndex = newRoomIndex;

        // Verify Toxic Crystal Locations
        if (currentCrystalsLocationIndices.Contains(currentPlayerLocationIndex))
        {
            // Collect Toxic Crystals
            currentCrystalsLocationIndices.Remove(currentPlayerLocationIndex);

            // Check for module solving
            if (currentCrystalsLocationIndices.Count == 0)
            {
                summoningModule.ModuleLog(moduleId, "Collected the Toxic Crystal in room {0}. That was all of them!!", currentPlayerLocationIndex);
                summoningModule.ReceiveSolve();

                // Return because moving Void is useless
                return;
            }
            else
            {
                summoningModule.ModuleLog(moduleId, "Collected the Toxic Crystal in room {0}. Remaining ones are in Room(s) {1}",
                    currentPlayerLocationIndex, currentCrystalsLocationIndices.Join());
            }
        }

        // Moving Void
        MoveVoidToNewRooms();
    }

    string GetPlayerFacingDirectionName(int movementDirection)
    {
        switch (movementDirection)
        {
            case 0:
                return "North";
            case 1:
                return "East";
            case 2:
                return "South";
            case 3:
                return "West";
            default:
                return "NULL";
        }
    }
    
    void MoveVoidToNewRooms()
    {
        for (int i = 0; i < 4; i++)
        {
            int _voidStartingLocation = startingVoidLocationsIndices[i];

            // Increment to get to the next room in the Paths.
            // Starting room is index 0 in the Path, so the first movement brings us to index 1, as wanted
            currentIndexInPathForVoids[i] = (currentIndexInPathForVoids[i] + 1) % (VoidPathsFromStartingLocation[_voidStartingLocation].Length);

            // Update the rooms for all Voids.
            voidedCellsIndices[i] = VoidPathsFromStartingLocation[_voidStartingLocation][currentIndexInPathForVoids[i]];

            // Check player location to send strike
           if (voidedCellsIndices[i] == currentPlayerLocationIndex)
           {
               summoningModule.ModuleLog(moduleId, "The Void that started in room {0} moved to cell {1}, which is where you currently stand. Strike given. Module reset.",
                   _voidStartingLocation, currentPlayerLocationIndex);
           
               summoningModule.ReceiveStrike();
           
               ResetValuesToStart();
               return;
           }


        }

        summoningModule.ModuleLog(moduleId, "Voids that started in rooms {0} moved to rooms {1} respectively.", startingVoidLocationsIndices.Join("-"), voidedCellsIndices.Join("-"));
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        // Verify integrity of the puzzle data
        // VerifyData();


        // Generate Void in faraway-ish rooms so you're not instantly locked turn 1
        GenerateStartingLocations();


        // Initialize the values by "resetting" them
        ResetValuesToStart();

    }

    void GenerateStartingLocations()
    {
        // Start with Voided Rooms
        // We want all 4 Voids to be far-ish away from each other so they're not clustered everywhere
        // Since the indices are in reading order and not geographically, we can't just take 4 indices far away
        // Instead, we pick a room randomly for the first one
        // Then for the next ones, we loop until we find a room that is not connected
        // Repeat, preventing for every room, and we should have a simple cluster prevention

        // The worst-case scenario is picking 1, 9, 11 and 17 since they disable every room except 4
        // But even in this case, we still have 4 rooms remaining, 3 get crystals and 1 the player.


        // Create a list of possible indices, shuffled to act as a Randomizer
        List<int> _possibleRemainingRoomIndices = new List<int>() { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 }.Shuffle();


        // Reverse loop, with safety check as we might preemptively remove room indices that we know are unsafe when we select a new room to Void
        // This a allows to remove items from the _possibleRemainingRoomIndices and use the same loop for Crystals and Player room selection

        for (int i = _possibleRemainingRoomIndices.Count - 1; i >= 0; i--)
        {
            // Safety check to make sure we aren't reading outside of the bounds, in case we remove a lot of rooms at once
            if (_possibleRemainingRoomIndices.Count <= i) { continue; }

            int _currentTestedRoom = _possibleRemainingRoomIndices[i];

            if (startingVoidLocationsIndices.Count != 4)
            { goto VoidedRooms; }
            else if (startingCrystalsLocationIndices.Count != 3)
            { goto ToxicCrystalsLocation; }
            else { goto PlayerLocation; }



        VoidedRooms:
            // We should already just have valid rooms remaining, so we just pick one
            startingVoidLocationsIndices.Add(_currentTestedRoom);

            // Remove it from the possibilities
            _possibleRemainingRoomIndices.Remove(_currentTestedRoom);

            // Remove its connections from the possibilities too
            _possibleRemainingRoomIndices = _possibleRemainingRoomIndices.Where(x => connectedRoomsFromIndices[_currentTestedRoom].Contains(x) == false).ToList();

            // summoningModule.ModuleLog(moduleId, "Just added room {0}, next possible rooms are {1}.", _currentTestedRoom, _possibleRemainingRoomIndices.Join());
            continue;

        ToxicCrystalsLocation:
            // Crystals are the same, but we don't need to remove their connections

            startingCrystalsLocationIndices.Add(_currentTestedRoom);
            _possibleRemainingRoomIndices.Remove(_currentTestedRoom);

            continue;

        PlayerLocation:
            // Player Location should also be fine
            
            startingPlayerLocationIndex = _currentTestedRoom;
            _possibleRemainingRoomIndices.Remove(_currentTestedRoom);

            // We are done with the initial room finding for everything. Exit the loop
            break;
        }

        // summoningModule.ModuleLog(moduleId, "For info, the remaining available rooms were {0}.", _possibleRemainingRoomIndices.Join());

        ShowInitialInfoOnPlate();

    }

    void ShowInitialInfoOnPlate()
    {
        // Void
        string _allVoidRooms = startingVoidLocationsIndices.Join();
        topStartingVoidRoomsTextMesh.text = _allVoidRooms;
        summoningModule.ModuleLog(moduleId, "Initialization complete. Starting Void Rooms are: {0}.", _allVoidRooms);


        // Crystals
        string _allCrystals = startingCrystalsLocationIndices.Join();
        middleToxicCrystalsRoomsTextMesh.text = _allCrystals;
        summoningModule.ModuleLog(moduleId, "Starting Toxic Crystal Rooms are: {0}.", _allCrystals);


        // Player
        bottomStartingLocationTextMesh.text = startingPlayerLocationIndex.ToString();
        summoningModule.ModuleLog(moduleId, "Starting Player Room is {0}.", startingPlayerLocationIndex);

    }

    void ResetValuesToStart()
    {
        // doing ListA = ListB actually makes both Lists behave the exact same way, as in they both become one and the same
        // So doing ListA = new List<T>(ListB) is necessary to make a copy


        // Reset Void
        voidedCellsIndices = new List<int>(startingVoidLocationsIndices);
        // Initialize at index 0, and we increment before every movement so it ends up correct
        currentIndexInPathForVoids = new int[4] { 0, 0, 0, 0 };

        // Reset Crystals
        currentCrystalsLocationIndices = new List<int>(startingCrystalsLocationIndices);

        // Reset Player
        currentPlayerLocationIndex = startingPlayerLocationIndex;
    }


    void VerifyData()
    {
        summoningModule.ModuleLog(moduleId, "Verifying Data");

        foreach (int[] _roomExits in connectedRoomsFromIndices)
        {
            for (int i = 0; i < 4; i ++)
            {
                if (_roomExits[i] == -1)
                {
                    continue;
                }
                
                if (Array.IndexOf(connectedRoomsFromIndices, _roomExits) == connectedRoomsFromIndices[_roomExits[i]][(i+2)%4])
                {
                    continue;
                }

                summoningModule.ModuleLogError(moduleId, "Found incorrect room. Exit {0} of room {1} leads to {2}, but exit {3} of {2} leads to {4}!!",
                    i, Array.IndexOf(connectedRoomsFromIndices, _roomExits), _roomExits[i], (i + 2) % 4, connectedRoomsFromIndices[_roomExits[i]][(i + 2) % 4]);

            }
        }
    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            yield return "sendtochaterror {0} Received an empty command.";
            yield break;
        }

        // Either TOXIC
        if (commandParts[0] == "toxic")
        {
            yield return "sendtochat {0} Successfully pressed TOXIC and reset the module.";
            CasingTextButtonGetsPressed();
            yield break;
        }

        // Accept the words "submit", "move", "press", or their initials
        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "move" && commandParts[0] != "m" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Unrecognized command. Please use 'toxic' to reset module, or 'move', 'submit' or 'press' to move around.";
            yield break;
        }


        // or Submit / Move + directions
        if (commandParts.Length == 1)
        {
            yield return "sendtochaterror {0} Please send a movement when submitting.";
            yield break;
        }
        else if (commandParts.Length > 2)
        {
            yield return "sendtochat {0} More than one movement payload was found. Only '" + commandParts[1] + "' will be taken into account." ;
        }


        foreach (char _movementDirection in commandParts[1])
        {
            yield return new WaitForSeconds(0.15f);

            switch (_movementDirection)
            {
                case 'u': case 'n':
                    platePressableButtons[0].OnInteract();
                    break;

                case 'r': case 'e':
                    platePressableButtons[3].OnInteract();
                    break;

                case 'd': case 's':
                    platePressableButtons[1].OnInteract();
                    break;

                case 'l': case 'w':
                    platePressableButtons[2].OnInteract();
                    break;
            }
        }



        yield return null;
    }

    public override IEnumerator TwitchHandleForcedSolve()
    {
        // An Auto-solver could be done, but it's a complex non-grid pathfinding
        // and I don't feel like spending days to find a solution, for now.
        summoningModule.ReceiveSolve();

        yield break;
    }
}
