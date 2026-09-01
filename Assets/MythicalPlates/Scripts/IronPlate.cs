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


    // Ruleseed data
    string[] readableRuleLog = new string[21] {"AA Batteries", "D Batteries", "Indicator SND", "Indicator CLR", "Indicator CAR",
            "Indicator IND", "Indicator FRQ", "Indicator SIG", "Indicator NSA", "Indicator MSA", "Indicator TRN",
            "Indicator BOB", "Indicator FRK", "Empty Port Plate", "Parallel Port", "Serial Port", "DVI-D Port",
            "PS/2 Port", "RJ-45 Port", "Stereo RCA Port", "Duplicate Port"};
    int[] selectedPotentialVoidRules = new int[7];
    int[] selectedPotentialVoidLocations = new int[7];


    /// <summary> Number of Timesteps used for generating the puzzle, and a forced solution. Sadly not necessary unique; especially with infinity seconds... </summary>
    int targetTimestopDuration;

    bool isPlayerSimulatingGame;
    Coroutine timeFlowingCoroutine;

    List<string> loggingData = new List<string>();

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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (hasPlateSolved)
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
        StartCoroutine(PlateShouldSolve());
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
        // Good thing is that Void is never next to an edge
        // So void handling is wayyyy easier; that would pause Game Design questions I do not want to answer


        // Logging is special because we might do multiple rounds of Puzzle Generation (and so of Creature Movements)
        // so to not flood the Log with discarded generation attempts, Logging will be separated using "isPlayerSimulatingGame"
        // If true, that's the player's attempt, so it gets Logged directly
        // If false, that's a puzzle generation attempt, and so it gets added to the loggingData List and kept to be logged only if this attempt's successful


        Creature _creatureToMove = creatures[creatureId];


        // Move while not in void
        do
        {
            // Void can't be next to an edge so it's always safe to move
            _creatureToMove.currentCreatureLocation = GetTileIndexInDirection(_creatureToMove.currentCreatureLocation, _creatureToMove.currentMovementDirection);
        }
        while (voidedCellsIndices.Contains(_creatureToMove.currentCreatureLocation));       

        
        // Check for tile edge
        if (WillTileGoOffEdge(_creatureToMove.currentCreatureLocation, _creatureToMove.currentMovementDirection))
        {
            // If on edge, then turn 180°
            _creatureToMove.currentMovementDirection = GetOppositeMovementDirection(_creatureToMove.currentMovementDirection);

            // And reset all counters for the creatures 
            _creatureToMove.supplementaryMovementData = 0;

            // Either log directly, or save data for later
            if (isPlayerSimulatingGame)
            {
                summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1} and reached an edge. It turned 180° to face {2}",
                    creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString());
            }
            else
            {
                loggingData.Add(string.Format("{0} moves to Edge {1}, turns to {2}",
                    creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString()));
            }

            

        }
        // Only apply MovementPattern-specific rotation if edge was not reached
        else
        {
            // Every single one increases tile by 1
            // And does something once it reaches 2


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

                        if (isPlayerSimulatingGame)
                        {
                            summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1} and then turned 180° to face {2}.",
                                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString());
                        }
                        else
                        {
                            loggingData.Add(string.Format("{0} moves to {1}, turns 180° to {2}",
                                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString()));
                        }
                            
                        break;


                    // (O) Circle
                    // Turns counter-clockwise
                    case CreatureMovementPattern.O_Circular:
                        // Reset counter and turn -60°
                        _creatureToMove.currentMovementDirection = GetRotatedMovementDirection(_creatureToMove.currentMovementDirection, isClockwise: false);

                        if (isPlayerSimulatingGame)
                        {
                            summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1} and then turned counter-clockwise once to face {2}.",
                                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString());
                        }
                        else
                        {
                            loggingData.Add(string.Format("{0} moves to {1}, turns counter-clockwise to {2}",
                                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9), _creatureToMove.currentMovementDirection.ToString()));
                        }
                            

                        break;

                    // (Z) Zig-Zag
                    // Turns Clock then Counter then Clock then Counter
                    case CreatureMovementPattern.Z_ZigZag:
                        _creatureToMove.currentMovementDirection = GetRotatedMovementDirection(_creatureToMove.currentMovementDirection, _creatureToMove.zigZagShouldDoClockwise);
                        
                        // Flip the bit so that it alternates between Clock & Counter
                        _creatureToMove.zigZagShouldDoClockwise ^= true;

                        if (isPlayerSimulatingGame)
                        {
                            summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1} and then turned {2} to face {3}.",
                                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9),
                                _creatureToMove.zigZagShouldDoClockwise ? "clockwise" : "counter-clockwise", _creatureToMove.currentMovementDirection.ToString());
                        }
                        else
                        {
                            loggingData.Add(string.Format("{0} moves to {1}, turns {2} to {3}", creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9),
                                _creatureToMove.zigZagShouldDoClockwise ? "clockwise" : "counter-clockwise", _creatureToMove.currentMovementDirection.ToString()));
                        }

                        break;



                    // Other Patterns that don't do anything
                    default:
                        if (isPlayerSimulatingGame)
                        {
                            summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1}.",
                                creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9));
                        }
                        else
                        {
                            loggingData.Add(string.Format("{0} moves to {1}", creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9)));
                        }
                        break;
                }
            }
            else // Not a special turn count
            {

                if (isPlayerSimulatingGame)
                {
                    summoningModule.ModuleLog(moduleId, "Creature {0} has moved to {1}.",
                        creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9));
                }
                else
                {
                    loggingData.Add(string.Format("{0} moves to {1}", creatureId, GetCoordinateFromCellIndex(_creatureToMove.currentCreatureLocation, 9)));
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

        ManageRuleseed();

        DetermineVoidedTiles();
        GenerateFourCreatures();
        StartGenerationSimulation();

        // Reset data so that Generation Simulation data doesn't carry over
        ResetCreatureData();
    }

    void ManageRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        {
            selectedPotentialVoidRules = new int[7] { 0, 4, 13, 16, 2, 11, 14 };

            // Not the same as the manual, because we directly use the tile indices in here!
            selectedPotentialVoidLocations = new int[7] { 13, 20, 28, 42, 49, 57, 69 };
            return;
        }

        int[] possibleRules = new int[21] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
        int[] possibleLocations = new int[37] {
                  10, 11, 12, 13,
                19, 20, 21, 22, 23,
              28, 29, 30, 31, 32, 33,
            37, 38, 39, 40, 41, 42, 43,
              47, 48, 49, 50, 51, 52,
                57, 58, 59, 60, 61,
                  67, 68, 69, 70 };

        FisherYatesShuffle(ref possibleRules, Rng);
        FisherYatesShuffle(ref possibleLocations, Rng);

        selectedPotentialVoidRules = possibleRules.Take(7).ToArray();
        selectedPotentialVoidLocations = possibleLocations.Take(7).ToArray();

        for (int i = 0; i < 7; i ++)
        {
            Debug.LogFormat("<Iron Plate #{0}> Potential Void has rule {1} and is in location {2}", moduleId, readableRuleLog[selectedPotentialVoidRules[i]],
                GetCoordinateFromCellIndex(selectedPotentialVoidLocations[i], 9));
        }
    }

    void DetermineVoidedTiles()
    {
        for (int i = 0; i < 7; i++)
        {
            CheckIndividualVoidTile(selectedPotentialVoidRules[i], selectedPotentialVoidLocations[i]);
        }
    }

    void CheckIndividualVoidTile(int ruleNumber, int locationId)
    {
        bool _isTrue = false;
        switch (ruleNumber)
        {
            case 0: // AA Batteries
                _isTrue = bombInfo.GetBatteryCount(Battery.AA) > 0;
                break;

            case 1: // D Batteries
                _isTrue = bombInfo.GetBatteryCount(Battery.D) > 0;
                break;

            case 2: // Indicator SND
                _isTrue = bombInfo.GetIndicators().Contains("SND");
                break;

            case 3: // Indicator CLR
                _isTrue = bombInfo.GetIndicators().Contains("CLR");
                break;

            case 4: // Indicator CAR
                _isTrue = bombInfo.GetIndicators().Contains("CAR");
                break;

            case 5: // Indicator IND
                _isTrue = bombInfo.GetIndicators().Contains("IND");
                break;

            case 6: // Indicator FRQ
                _isTrue = bombInfo.GetIndicators().Contains("FRQ");
                break;

            case 7: // Indicator SIG
                _isTrue = bombInfo.GetIndicators().Contains("SIG");
                break;

            case 8: // Indicator NSA
                _isTrue = bombInfo.GetIndicators().Contains("NSA");
                break;

            case 9: // Indicator MSA
                _isTrue = bombInfo.GetIndicators().Contains("MSA");
                break;

            case 10: // Indicator TRN
                _isTrue = bombInfo.GetIndicators().Contains("TRN");
                break;

            case 11: // Indicator BOB
                _isTrue = bombInfo.GetIndicators().Contains("BOB");
                break;

            case 12: // Indicator FRK
                _isTrue = bombInfo.GetIndicators().Contains("FRK");
                break;

            case 13: // Empty Port Plate
                _isTrue = bombInfo.GetPortPlates().Any(x => x.Length == 0);
                break;

            case 14: // Parallel Port
                _isTrue = bombInfo.GetPorts().Contains("Parallel");
                break;

            case 15: // Serial Port
                _isTrue = bombInfo.GetPorts().Contains("Serial");
                break;

            case 16: // DVI-D Port
                _isTrue = bombInfo.GetPorts().Contains("DVI");
                break;

            case 17: // PS/2 Port
                _isTrue = bombInfo.GetPorts().Contains("PS2");
                break;

            case 18: // RJ-45 Port
                _isTrue = bombInfo.GetPorts().Contains("RJ45");
                break;

            case 19: // Stereo RCA Port
                _isTrue = bombInfo.GetPorts().Contains("StereoRCA");
                break;

            case 20: // Duplicate Port
                _isTrue = bombInfo.GetPorts().Distinct().Count() != bombInfo.GetPortCount();
                break;
        }

        if (_isTrue)
        {
            voidedCellsIndices.Add(locationId);
            summoningModule.ModuleLog(moduleId, "Found {0}. Voiding {1}", readableRuleLog[ruleNumber], GetCoordinateFromCellIndex(locationId, 9));
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

            // summoningModule.ModuleLog(moduleId, "Creature {0} starts in {1} and has Movement Pattern {2}.",
            //     i, GetCoordinateFromCellIndex(_generatedCreature.startingLocation, 9), _generatedCreature.movementPattern.ToString());

            creatures[i] = _generatedCreature;
        }
    }

    void StartGenerationSimulation()
    {
        // Clear Logging Data as this is a new simulation
        loggingData.Clear();


        // summoningModule.ModuleLog(moduleId, "Starting Simulation for {0} turns", targetTimestopDuration);

        for (int _turn = 1; _turn <= targetTimestopDuration; _turn++)
        {
            // summoningModule.ModuleLog(moduleId, "Starting turn {0}.", _turn);

            loggingData.Add("Turn " + _turn);

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

                summoningModule.ModuleLog(moduleId, "Creature {0} with Movement Pattern {1} starts in {2} and has {3} as a target location",
                _creatureId, creatures[_creatureId].movementPattern.ToString(),
                GetCoordinateFromCellIndex(creatures[_creatureId].startingLocation, 9),
                GetCoordinateFromCellIndex(creatures[_creatureId].targetLocation, 9));


                // Put on the Plate
                creaturesDataPlateTexts[_creatureId].text = string.Format("{0} {1} {2}",
                    GetCoordinateFromCellIndex(creatures[_creatureId].startingLocation, 9),
                    creatures[_creatureId].movementPattern.ToString().Remove(1),
                    GetCoordinateFromCellIndex(creatures[_creatureId].targetLocation, 9));
            }


            // Log the correct Information
            LogPuzzleGenerationData();
        }
        else
        {
            // They aren't all different? Regenrate!
            // summoningModule.ModuleLog(moduleId, "Some of the Creatures ended in the same spot. Regenerating new Simulation");
            GenerateFourCreatures();
            StartGenerationSimulation();
        }
    }

    void LogPuzzleGenerationData()
    {
        if (loggingData.Count%5 != 0)
        {
            summoningModule.ModuleLogError(moduleId, "Logging Data List is not a multiple of 5. That is very wrong! Please contact thunder725");
            summoningModule.ModuleLog(moduleId, "Logging Data List: {0}", loggingData.Join(" // "));
            return;
        }

        summoningModule.ModuleLog(moduleId, "Puzzle Generation summary:");

        for (int i = 0; i < loggingData.Count / 5; i ++)
        {
            summoningModule.ModuleLog(moduleId, "{0}: {1} // {2} // {3} // {4}", loggingData[i * 5],
                loggingData[1 + i * 5], loggingData[2 + i * 5], loggingData[3 + i * 5], loggingData[4 + i * 5]);
        }

        summoningModule.ModuleLog(moduleId, "Resume time for {0} seconds to solve this Module.", targetTimestopDuration);
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { yield break; }

        Debug.LogFormat("<Iron Plate #{0}> Received Command ''{1}''", moduleId, command);
        if (hasPlateSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);


        if (commandParts.Length == 0)
        {
            Debug.LogFormat("<Iron Plate #{0}> Received empty command!", moduleId);
            yield return "sendtochaterror {0} Received empty command!";
            yield break;
        }

        if (commandParts.Length != 2)
        {
            Debug.LogFormat("<Iron Plate #{0}> Received command formatted incorrectly!", moduleId);
            yield return "sendtochaterror {0} Received command formatted incorrectly!";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            Debug.LogFormat("<Iron Plate #{0}> Received unknown command! Please use 'submit' or 'press' to submit an answer.", moduleId);
            yield return "sendtochaterror {0} Received unknown command! Please use 'submit' or 'press' to submit an answer.";
            yield break;
        }

        int _timeToKeepPressed = int.Parse(commandParts[1]);

        if (_timeToKeepPressed < 1)
        {
            Debug.LogFormat("<Iron Plate #{0}> You can't keep time flowing for less than 1 timer tick!", moduleId);
            yield return "sendtochaterror {0} You can't keep time flowing for less than 1 timer tick!";
            yield break;
        }

        if (_timeToKeepPressed > 17)
        {
            Debug.LogFormat("<Iron Plate #{0}> Trying to keep time flowing for less more than 17 timer tick! While this can be valid, there is a shorter solution. Let's avoid stalling in Twitch Plays!!", moduleId);
            yield return "sendtochaterror {0} Trying to keep time flowing for less more than 17 timer tick! While this can be valid, there is a shorter solution. Let's avoid stalling in Twitch Plays!";
            yield break;
        }

        yield return null;
        // Make time Flow
        platePressableButtons[0].OnInteract();

        // Wait
        while (numberOfMovementsDone < _timeToKeepPressed)
        {
            yield return null;
        }

        // Stop Time
        platePressableButtons[0].OnInteract();
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        StartCoroutine(PlateShouldSolve());

        yield break;
    }
}
