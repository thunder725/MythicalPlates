using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IronPlate : PlateBase {
    enum CreatureMovementPattern { H_Horizontal, L_HorizontalBackAndForth, X_DiagonalUpRight, Y_DiagonalUpLeft, O_Circular, Z_ZigZag };

    struct Creature {
        public CreatureMovementPattern movementPattern;
        public int targetLocation;
        public int startingLocation;
        public int currentCreatureLocation;
        public MovementDirection currentMovementDirection;
        public int supplementaryMovementData;
        public bool zigZagShouldDoClockwise;
    };

    Creature[] creatures = new Creature[4];

    [SerializeField] TextMesh[] creaturesDataPlateTexts;

    /// <summary> Number of Timesteps used for generating the puzzle, and a forced solution. Sadly not necessary unique; especially with infinity seconds... </summary>
    int targetTimestopDuration;

    bool isPlayerSimulatingGame;
    Coroutine timeFlowingCoroutine;

    int previousSeenTimerSecond = 0;
    int numberOfMovementsDone = 0;

    // Universal Logging Data
    static int moduleIdCounter = 1;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;


        platePressableButtons[0].OnInteract += delegate () { PressedTimestopButton(); return false; };

    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        InitializePuzzle();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Input
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void PressedTimestopButton()
    {
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        // Flip the boolean!
        isPlayerSimulatingGame ^= true;
        
        if (isPlayerSimulatingGame)
        {
            summoningModule.ModuleLog(moduleId, "=-=-= Time now flows again =-=-=");

            previousSeenTimerSecond = Mathf.FloorToInt(bombInfo.GetTime());

            timeFlowingCoroutine = StartCoroutine(TimeFlowing());
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Time has been stopped after {0} seconds.", numberOfMovementsDone);

            if (timeFlowingCoroutine != null)
            {
                StopCoroutine(timeFlowingCoroutine);
            }

            VerifyFinalAnswer();
        }
    }

    void VerifyFinalAnswer()
    {
        // Verify that the Current Location of every creature is their Target
        for (int i = 0; i < 4; i ++)
        {
            if (creatures[i].currentCreatureLocation != creatures[i].targetLocation)
            {
                summoningModule.ModuleLog(moduleId, "Current Location of creature {0} is {1} while its target is {2}!!",
                    i,
                    GetCoordinateFromCellIndex(creatures[i].currentCreatureLocation, 9),
                    GetCoordinateFromCellIndex(creatures[i].targetLocation, 9));

                ResetCreatureData();
                summoningModule.ReceiveStrike();
                return;
            }
        }

        // Code arrives here only if all 4 were correct.

        summoningModule.ModuleLog(moduleId, "All four creatures arrived at their target locations!!");
        summoningModule.ReceiveSolve();
    }

    IEnumerator TimeFlowing()
    {
        while (true)
        {
            // Time is different?
            if (previousSeenTimerSecond != Mathf.FloorToInt(bombInfo.GetTime()))
            {
                // Update time
                previousSeenTimerSecond = Mathf.FloorToInt(bombInfo.GetTime());

                numberOfMovementsDone++;

                summoningModule.ModuleLog(moduleId, "Moving creatures for second {0}", numberOfMovementsDone);

                for (int i = 0; i < 4; i ++)
                {
                    MoveCreatureOneTimestep(i);
                }
            }

            // No need to check every single frame, we have a window of 1s, we can check less often than that to optimize a bit
            yield return new WaitForSeconds(0.1f);
        }
    }



    void ResetCreatureData()
    {
        for (int i = 0; i < 4; i ++)
        {
            // Current location is starting location
            creatures[i].currentCreatureLocation = creatures[i].startingLocation;
            creatures[i].supplementaryMovementData = 0;

            // Movement Direction depends on Movement Pattern
            switch (creatures[i].movementPattern)
            {
                case CreatureMovementPattern.H_Horizontal:
                    creatures[i].currentMovementDirection = MovementDirection.Right;
                    break;

                case CreatureMovementPattern.L_HorizontalBackAndForth:
                    creatures[i].currentMovementDirection = MovementDirection.Left;
                    break;

                case CreatureMovementPattern.X_DiagonalUpRight:
                    creatures[i].currentMovementDirection = MovementDirection.UpRight;
                    break;

                case CreatureMovementPattern.Y_DiagonalUpLeft:
                    creatures[i].currentMovementDirection = MovementDirection.UpLeft;
                    break;

                case CreatureMovementPattern.O_Circular:
                    creatures[i].currentMovementDirection = MovementDirection.Right;
                    break;

                case CreatureMovementPattern.Z_ZigZag:
                    creatures[i].currentMovementDirection = MovementDirection.UpLeft;
                    creatures[i].zigZagShouldDoClockwise = true;
                    break;
            }
        }
    }

    protected override void CasingTextButtonGetsPressed() { }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Hexagonal Grid Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    bool WillTileGoOffEdge(int tileIndex, MovementDirection edgeDirection)
    {
        // Edge linked to Movement Directions are combination of different Hexagon Edges
        // Moving to the left brings you out if you are in the TopLeft or BottomLeft edges
        // So every MovementDirection is the combination of two Hexagon Edges

        switch (edgeDirection)
        {
            case MovementDirection.Left:
                return isTileOnTopLeftEdge(tileIndex) || isTileOnBottomLeftEdge(tileIndex);

            case MovementDirection.Right:
                return isTileOnBottomRightEdge(tileIndex) || isTileOnTopRightEdge(tileIndex);

            case MovementDirection.UpLeft:
                return isTileOnTopLeftEdge(tileIndex) || isTileOnTopEdge(tileIndex);

            case MovementDirection.DownRight:
                return isTileOnBottomEdge(tileIndex) || isTileOnBottomRightEdge(tileIndex);

            case MovementDirection.UpRight:
                return isTileOnTopRightEdge(tileIndex) || isTileOnTopEdge(tileIndex);

            case MovementDirection.DownLeft:
                return isTileOnBottomEdge(tileIndex) || isTileOnBottomLeftEdge(tileIndex);
        }

        return false;
    }


    bool isTileOnTopEdge(int tileIndex)
    { // Is in line 1
        return tileIndex < 5; }

    bool isTileOnTopLeftEdge(int tileIndex)
    { // Is in column A
        return tileIndex % 9 == 0; }

    bool isTileOnBottomLeftEdge(int tileIndex)
    { // Is in A5, B6, C7, D8 or E9
      // Those are index 36, 46, 56, 66 and 76
        return (tileIndex > 35) && (tileIndex % 10 == 6); }

    bool isTileOnBottomEdge(int tileIndex)
    { // Is in line 9
        return tileIndex > 71; }

    bool isTileOnBottomRightEdge (int tileIndex)
    { // Is in column I
        return tileIndex % 9 == 8; }

    bool isTileOnTopRightEdge(int tileIndex)
    { // Is in E1 F2 G3 H4 I5
      // Those are index 4 14 24 34 44
        return (tileIndex < 45) && (tileIndex % 10 == 4); }


    /// <summary>
    /// Does NOT do any edge verification algorithm nor Void verification, it just returns an offset index.
    /// </summary>
    int GetTileIndexInDirection(int startingTile, MovementDirection movementDirection)
    {
        switch(movementDirection)
        {
            case MovementDirection.Left:
                return startingTile - 1;

            case MovementDirection.Right:
                return startingTile + 1;

            case MovementDirection.UpLeft:
                return startingTile - 10;

            case MovementDirection.DownRight: 
                return startingTile + 10;

            case MovementDirection.UpRight:
                return startingTile - 9;

            case MovementDirection.DownLeft:
                return startingTile + 9;
        }

        return 0;
    }

    MovementDirection GetRotatedMovementDirection(MovementDirection movementDirection, bool isClockwise)
    {
        switch (movementDirection)
        {
            case MovementDirection.Left:
                return isClockwise ? MovementDirection.UpLeft : MovementDirection.DownLeft;

            case MovementDirection.Right:
                return isClockwise ? MovementDirection.DownRight : MovementDirection.UpRight;

            case MovementDirection.UpLeft:
                return isClockwise ? MovementDirection.UpRight : MovementDirection.Left;

            case MovementDirection.DownRight:
                return isClockwise ? MovementDirection.DownLeft : MovementDirection.Right;

            case MovementDirection.UpRight:
                return isClockwise ? MovementDirection.Right : MovementDirection.UpLeft;

            case MovementDirection.DownLeft:
                return isClockwise ? MovementDirection.Left : MovementDirection.DownRight;
        }

        return MovementDirection.Right;
    }


    void MoveCreatureOneTimestep(int creatureId)
    {
        // Good thing is that Void is never:
        // a) 2 in a row
        // b) Next to an edge
        // So void handling is wayyyy easier, none of that while loop business, just move twice if needed ^^


        Creature _creatureToMove = creatures[creatureId];


        // First move once
        _creatureToMove.currentCreatureLocation = GetTileIndexInDirection(_creatureToMove.currentCreatureLocation, _creatureToMove.currentMovementDirection);

        // Then check for void
        if (voidedCellsIndices.Contains(_creatureToMove.currentCreatureLocation))
        {
            // Move a second time
            // Void can't be next to an edge so it's always safe to do that
            _creatureToMove.currentCreatureLocation = GetTileIndexInDirection(_creatureToMove.currentCreatureLocation, _creatureToMove.currentMovementDirection);
        }
        

        
        // Check for tile edge
        if (WillTileGoOffEdge(_creatureToMove.currentCreatureLocation, _creatureToMove.currentMovementDirection))
        {
            // If on edge, then turn 180°
            _creatureToMove.currentMovementDirection = GetOppositeMovementDirection(_creatureToMove.currentMovementDirection);

            // And reset all counters for the creatures 
            _creatureToMove.supplementaryMovementData = 0;

            summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1} and reached an edge. New Movement Direction is {2}",
                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString());

        }
        // Only apply MovementPattern-specific rotation if edge was not reached
        else
        {
            // Every single one increases tile by 1
            // And does something once it reaches 2

            summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1}.",
                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9));

            _creatureToMove.supplementaryMovementData++;
            if (_creatureToMove.supplementaryMovementData == 2)
            {
                // Reset Counter
                _creatureToMove.supplementaryMovementData = 0;


                // Then do something for some Movement Patterns
                switch (_creatureToMove.movementPattern)
                {
                    // (L) Horizontal Back & Forth
                    // Turns around
                    case CreatureMovementPattern.L_HorizontalBackAndForth:
                        // Reset counter and turn 180°
                        _creatureToMove.currentMovementDirection = GetOppositeMovementDirection(_creatureToMove.currentMovementDirection);
                        summoningModule.ModuleLog(moduleId, "Creature {0} has turned 180° to {1}.",
                            creatureId, _creatureToMove.currentMovementDirection.ToString());
                        break;


                    // (O) Circle
                    // Turns counter-clockwise
                    case CreatureMovementPattern.O_Circular:
                        // Reset counter and turn -60°
                        _creatureToMove.currentMovementDirection = GetRotatedMovementDirection(_creatureToMove.currentMovementDirection, isClockwise: false);
                        summoningModule.ModuleLog(moduleId, "Creature {0} has turned counter-clockwise once to {1}.",
                            creatureId, _creatureToMove.currentMovementDirection.ToString());
                        break;

                    // (Z) Zig-Zag
                    // Turns Clock then Counter then Clock then Counter
                    case CreatureMovementPattern.Z_ZigZag:
                        _creatureToMove.currentMovementDirection = GetRotatedMovementDirection(_creatureToMove.currentMovementDirection, _creatureToMove.zigZagShouldDoClockwise);
                        
                        // Flip the bit so that it alternates between Clock & Counter
                        _creatureToMove.zigZagShouldDoClockwise ^= true;

                        summoningModule.ModuleLog(moduleId, "Creature {0} has turned {2} to {1}.",
                            creatureId, _creatureToMove.currentMovementDirection.ToString(), _creatureToMove.zigZagShouldDoClockwise ? "clockwise" : "counter-clockwise");
                        break;
                }
            }

            
        }


        // After all that, apply the data to the Creature
        creatures[creatureId] = _creatureToMove;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        // Determine what the intended timestop duration is
        targetTimestopDuration = UnityEngine.Random.Range(3, 16);
        summoningModule.ModuleLog(moduleId, "The intended solution for this generation will be to resume time for {0} seconds.", targetTimestopDuration);


        DetermineVoidedTiles();
        GenerateFourCreatures();
        StartGenerationSimulation();

        // Reset data so that Generation Simulation data doesn't carry over
        ResetCreatureData();
    }

    void DetermineVoidedTiles()
    {
        // Is there any AA Battery? => Void E2
        // Happens if there are more Batteries than Holders
        if (bombInfo.GetBatteryCount() > bombInfo.GetBatteryHolderCount())
        {
            voidedCellsIndices.Add(13);
            summoningModule.ModuleLog(moduleId, "Found AA batteries. Voiding E2");
        }


        IEnumerable<string> _indicators = bombInfo.GetIndicators();
        IEnumerable<string> _ports = bombInfo.GetPorts();

        // Is there a CAR indicator? => Void C3
        if (_indicators.Contains("CAR"))
        {
            voidedCellsIndices.Add(20);
            summoningModule.ModuleLog(moduleId, "Found CAR Indicator. Voiding C3");
        }

        // Is there an empty port plate? => Void B4
        if (bombInfo.GetPortPlates().Any(x => x.Length == 0))
        {
            voidedCellsIndices.Add(28);
            summoningModule.ModuleLog(moduleId, "Found empty port plate. Voiding B4");
        }

        // Is there a DVI-D Port? => Void G5
        if (_ports.Contains("DVI"))
        {
            voidedCellsIndices.Add(42);
            summoningModule.ModuleLog(moduleId, "Found DVI-D port. Voiding G5");
        }

        // Is there SND Indicator? => Void E6
        if (_indicators.Contains("SND"))
        {
            voidedCellsIndices.Add(49);
            summoningModule.ModuleLog(moduleId, "Found SND Indicator. Voiding E6");
        }

        // Is there BOB Indicator? => Void D7
        if (_indicators.Contains("BOB"))
        {
            voidedCellsIndices.Add(57);
            summoningModule.ModuleLog(moduleId, "Found BOB Indicator. Voiding D7");
        }

        // Is there a Parallel Port? => Void G8
        if (_ports.Contains("Parallel"))
        {
            voidedCellsIndices.Add(69);
            summoningModule.ModuleLog(moduleId, "Found Parallel port. Voiding G8");
        }
    }

    void GenerateFourCreatures()
    {
        // Generate a list of allowed Starting Locations
        // All Hexagon Tiles, except Edges, except Void
        List<int> allowedStartingLocations = new List<int>()
        { 
                  10, 11, 12, 13,   
                19, 20, 21, 22, 23,   
              28, 29, 30, 31, 32, 33,  
            37, 38, 39, 40, 41, 42, 43,  
              47, 48, 49, 50, 51, 52,
                57, 58, 59, 60, 61,
                  67, 68, 69, 70
        }.Except(voidedCellsIndices).ToList();


        // Generate a list of allowed MovementPattern
        // All are allowed, but not twice
        List<CreatureMovementPattern> allowedMovementPatterns = new List<CreatureMovementPattern>() 
        {
            CreatureMovementPattern.H_Horizontal, CreatureMovementPattern.L_HorizontalBackAndForth,
            CreatureMovementPattern.X_DiagonalUpRight, CreatureMovementPattern.Y_DiagonalUpLeft,
            CreatureMovementPattern.O_Circular, CreatureMovementPattern.Z_ZigZag
        };


        // Generate 4 creatures
        for (int i = 0; i < 4; i ++)
        {
            // Generate Creature
            Creature _generatedCreature = new Creature();

            // Generate Movement Pattern
            _generatedCreature.movementPattern = allowedMovementPatterns.PickRandom();
            allowedMovementPatterns.Remove(_generatedCreature.movementPattern);


            // Generate Starting Location
            _generatedCreature.startingLocation = allowedStartingLocations.PickRandom();
            allowedStartingLocations.Remove(_generatedCreature.startingLocation);


            // Current location is starting location
            _generatedCreature.currentCreatureLocation = _generatedCreature.startingLocation;

            // Movement Direction depends on Movement Pattern
            switch(_generatedCreature.movementPattern)
            {
                case CreatureMovementPattern.H_Horizontal:
                    _generatedCreature.currentMovementDirection = MovementDirection.Right;
                    break;

                case CreatureMovementPattern.L_HorizontalBackAndForth:
                    _generatedCreature.currentMovementDirection = MovementDirection.Left;
                    break;

                case CreatureMovementPattern.X_DiagonalUpRight:
                    _generatedCreature.currentMovementDirection = MovementDirection.UpRight;
                    break;

                case CreatureMovementPattern.Y_DiagonalUpLeft:
                    _generatedCreature.currentMovementDirection = MovementDirection.UpLeft;
                    break;

                case CreatureMovementPattern.O_Circular:
                    _generatedCreature.currentMovementDirection = MovementDirection.Right;
                    break;

                case CreatureMovementPattern.Z_ZigZag:
                    _generatedCreature.currentMovementDirection = MovementDirection.UpLeft;
                    _generatedCreature.zigZagShouldDoClockwise = true;
                    break;
            }

            // Target Location will get set after simulation

            summoningModule.ModuleLog(moduleId, "Creature {0} starts in {1} and has Movement Pattern {2}.",
                i, GetCoordinateFromCellIndex(_generatedCreature.startingLocation, 9), _generatedCreature.movementPattern.ToString());

            creatures[i] = _generatedCreature;
        }
    }

    void StartGenerationSimulation()
    {
        summoningModule.ModuleLog(moduleId, "Starting Simulation for {0} turns", targetTimestopDuration);

        for (int _turn = 1; _turn <= targetTimestopDuration; _turn++)
        {
            summoningModule.ModuleLog(moduleId, "Starting turn {0}.", _turn);

            for (int _creatureId = 0; _creatureId < 4; _creatureId++)
            {
                MoveCreatureOneTimestep(_creatureId);
            }
        }



        // Keep only if the four creatures end up in different locations!
        int[] _endlocations = new int[4];

        for (int _creatureId = 0; _creatureId < 4; _creatureId++)
        {
            _endlocations[_creatureId] = creatures[_creatureId].currentCreatureLocation;
        }

        // All are different?
        if (_endlocations.Distinct().Count() == 4)
        {
            // Save them
            for (int _creatureId = 0; _creatureId < 4; _creatureId++)
            {
                creatures[_creatureId].targetLocation = _endlocations[_creatureId];

                summoningModule.ModuleLog(moduleId, "Creature {0} starts in {1} and has {2} as a target location",
                _creatureId,
                GetCoordinateFromCellIndex(creatures[_creatureId].startingLocation, 9),
                GetCoordinateFromCellIndex(creatures[_creatureId].targetLocation, 9));


                // Put on the Plate
                creaturesDataPlateTexts[_creatureId].text = string.Format("{0} {1} {2}",
                    GetCoordinateFromCellIndex(creatures[_creatureId].startingLocation, 9),
                    creatures[_creatureId].movementPattern.ToString().Remove(1),
                    GetCoordinateFromCellIndex(creatures[_creatureId].targetLocation, 9));
            }
        }
        else
        {
            // They aren't all different? Regenrate!
            summoningModule.ModuleLog(moduleId, "Some of the Creatures ended in the same spot. Regenerating new Simulation");
            GenerateFourCreatures();
            StartGenerationSimulation();
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
            yield return "sendtochaterror {0} Received empty command!";
            yield break;
        }

        if (commandParts.Length != 2)
        {
            yield return "sendtochaterror {0} Received command formatted incorrectly!";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Received unknown command! Please use 'submit' or 'press' to submit an answer.";
            yield break;
        }

        int _timeToKeepPressed = int.Parse(commandParts[1]);

        if (_timeToKeepPressed < 1)
        {
            yield return "sendtochaterror {0} Trying to keep time flowing for less than 1 timer tick!";
            yield break;
        }

        if (_timeToKeepPressed > 17)
        {
            yield return "sendtochaterror {0} Trying to keep time flowing for less more than 17 timer tick! While this can be valid, there is a shorter solution. Let's avoid clogging Twitch Plays!";
            yield break;
        }

        // Make time Flow
        PressedTimestopButton();

        // Wait
        while (numberOfMovementsDone < _timeToKeepPressed)
        {
            yield return null;
        }

        // Stop Time
        PressedTimestopButton();
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        summoningModule.ReceiveSolve();

        yield break;
    }
}
