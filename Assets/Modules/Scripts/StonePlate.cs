using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Deployment.Internal;
using System.Linq;
using UnityEngine;


public class StonePlate : PlateBase{

    int[][] VoidPatterns = new int[4][]
    {
        new int[16]{ 1, 6, 8, 15, 18, 21, 27, 28, 35, 36, 42, 45, 48, 55, 57, 62 },
        new int[23]{ 9, 17, 18, 21, 22, 25, 26, 29, 30, 31, 37, 38, 39, 42, 43, 46, 47, 48, 49, 50, 51, 56, 57},
        new int[22]{ 4, 6, 12, 13, 14, 20, 22, 25, 26, 27, 30, 33, 36, 37, 38, 41, 43, 49, 50, 51, 57, 59 },
        new int[24]{ 0, 1, 3, 8, 9, 11, 12, 15, 20, 22, 23, 30, 33, 34, 35, 36, 47, 49, 50, 51, 55, 57, 62, 63 }
    };


    /// <summary> Starting from the top-left corner of the Treasure, add the top-left corner's index
    /// to the indices in this table to get the final indices of the treasure, in an 8x8 table.</summary>
    int[][] TreasurePositionOffsets = new int[10][]
    {
        new int[14]{ 1, 2, 3, 8, 9, 10, 11, 12, 16, 20, 27, 28, 34, 35 },
        new int[14]{ 0, 2, 8, 9, 10, 11, 12, 16, 17, 18, 19, 20, 25, 27 },
        new int[11]{ 0, 8, 9, 10, 12, 16, 18, 19, 20, 27, 28 },
        new int[18]{ 2, 9, 10, 11, 17, 19, 20, 24, 25, 27, 28, 32, 33, 34, 35, 41, 42, 43 },
        new int[16]{ 4, 10, 12, 16, 18, 19, 20, 24, 25, 26, 27, 28, 32, 33, 34, 35 },
        new int[14]{ 2, 4, 10, 11, 12, 16, 19, 24, 25, 26, 27, 33, 34, 35 },
        new int[14]{ 0, 8, 9, 16, 17, 19, 24, 25, 26, 27, 32, 35, 42, 43 },
        new int[12]{ 1, 8, 9, 16, 17, 18, 25, 26, 34, 35, 42, 43 },
        new int[12]{ 1, 9, 10, 16, 17, 18, 19, 24, 32, 33, 34, 41 },
        new int[13]{ 0, 8, 9, 17, 18, 24, 25, 26, 27, 34, 35, 41, 42 }
    };

    /// <summary> For each Treasure, gives the latest Column and Row allowed for the Treasure to be correctly placed. 0-indexed </summary>
    int[] furthestAllowedCoordinatesPerTreasure = new int[20]
    { 
        3, 3,
        3, 4,
        3, 4,
        3, 2,
        3, 3,
        3, 3,
        4, 2,
        4, 2,
        4, 2,
        4, 2
    };

    List<int> allTreasureTiles = new List<int>();
    List<int> remainingDrillableTreasureTiles = new List<int>();

    int selectedVoidPaternIndex;
    int selectedTreasureIndex;
    int startingCoordinate;

    [SerializeField] TextMesh startingCoordinateTextMesh;

    int currentCoordinate;
    int currentNumberOfDrillsDone;

    /// <summary> Boolean mainly used for Twitch Plays as the vibration could be obscured or lagged-out easily. This will print the vibration type in the chat. </summary>
    bool lastVibrationWasStrong;

    Coroutine drillVibrationCoroutine;

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
        platePressableButtons[4].OnInteract += delegate () { PressedDrillInput(); return false; };

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


    protected override void CasingTextButtonGetsPressed() 
    {
        if (summoningModule.isModuleSolved)
        { return; }

        summoningModule.ModuleLog(moduleId, "STONE word pressed, re-generating puzzle.");

        ReGeneratePuzzleAtRuntime();
    }


    void PressedMovementInput(MovementDirection direction)
    {
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        VoidMovementData _data = MoveAroundGridWithVoid(direction, 64, ref currentCoordinate, 8, false);
        if (_data.ranIntoGridEdges)
        {
            summoningModule.ModuleLog(moduleId, "Moved {0} into the edges of the Underground. Strike! Re-generating puzzle", direction.ToString());
            summoningModule.ReceiveStrike();
            ReGeneratePuzzleAtRuntime();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Moved {0} into {1}", direction.ToString(), GetCoordinateFromCellIndex(currentCoordinate, 8));
        }
    }


    void PressedDrillInput()
    {
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        currentNumberOfDrillsDone++;

        if (currentNumberOfDrillsDone == 25)
        {
            summoningModule.ModuleLog(moduleId, "25th drill done, Striking and resetting the puzzle");
            summoningModule.ReceiveStrike();
            ReGeneratePuzzleAtRuntime();
            return;
        }
        
        // Are we on a Treasure Tile that we haven't uncovered before?
        if (remainingDrillableTreasureTiles.Contains(currentCoordinate))
        {
            // Vibrate greatly
            lastVibrationWasStrong = true;

            if (drillVibrationCoroutine != null) { StopCoroutine(drillVibrationCoroutine); }
            drillVibrationCoroutine = StartCoroutine(VibratePlate(5f));

            // Mark the tile as unnecessary to drill anymore
            remainingDrillableTreasureTiles.Remove(currentCoordinate);

            summoningModule.ModuleLog(moduleId, "Drilling at {0} returned a {1} vibration. This is drill number {2}.",
                GetCoordinateFromCellIndex(currentCoordinate, 8), lastVibrationWasStrong ? "strong" : "weak", currentNumberOfDrillsDone);


            // If all are excavated
            if (remainingDrillableTreasureTiles.Count == 0)
            {
                summoningModule.ModuleLog(moduleId, "All tiles of the Treasure have been drilled out! Solving module!");
                summoningModule.ReceiveSolve();
            }

        }
        // Otherwise
        else
        {
            // Vibrate slightly
            lastVibrationWasStrong = false;

            if (drillVibrationCoroutine != null) { StopCoroutine(drillVibrationCoroutine); }
            drillVibrationCoroutine = StartCoroutine(VibratePlate(1f));
        }
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        DetermineVoidPattern();

        GenerateStartingCoordinate();

        GenerateAndPlaceNewTreasure();

        currentCoordinate = startingCoordinate;
        startingCoordinateTextMesh.text = GetCoordinateFromCellIndex(startingCoordinate, 8);
    }


    void DetermineVoidPattern()
    {
        int _numberOfIndicators = bombInfo.GetIndicators().Count();

        selectedVoidPaternIndex = Mathf.Clamp(_numberOfIndicators, 0, 3);

        summoningModule.ModuleLog(moduleId, "Found {0} indicators, so Void Pattern used will be number {1}", _numberOfIndicators, selectedVoidPaternIndex);

        voidedCellsIndices.Clear();
        voidedCellsIndices = VoidPatterns[selectedVoidPaternIndex].ToList();
    }

    void GenerateStartingCoordinate()
    {
        bool isSearching = true;

        while (isSearching)
        {
            startingCoordinate = UnityEngine.Random.Range(0, 64);

            if (voidedCellsIndices.Contains(startingCoordinate) == false)
            {
                isSearching = false;
            }
        }
    }

    void GenerateAndPlaceNewTreasure()
    {
        // Determine the Treasure shape
        selectedTreasureIndex = UnityEngine.Random.Range(0, 9);
        summoningModule.ModuleLog(moduleId, "Selected Treasure with index {0} (first in reading order is 0). It looks like {1}",
            selectedTreasureIndex, GetLogDescriptionFromTreasureIndex(selectedTreasureIndex));


        int _treasureColumn = UnityEngine.Random.Range(0, furthestAllowedCoordinatesPerTreasure[2 * selectedTreasureIndex]);
        int _treasureRow = UnityEngine.Random.Range(0, furthestAllowedCoordinatesPerTreasure[1 + 2 * selectedTreasureIndex]);

        // Generate where it'll be placed
        int _topLeftTreasureCorner = 8 * _treasureRow + _treasureColumn;
        allTreasureTiles = TreasurePositionOffsets[selectedTreasureIndex].Select(x => x + _topLeftTreasureCorner).ToList();

        // Log its placement
        summoningModule.ModuleLog(moduleId, "With top-left corner at {0} (column {1} row {2}), all of its tiles are at index {3}.",
            GetCoordinateFromCellIndex(_topLeftTreasureCorner, 8), _treasureColumn, _treasureRow,
            allTreasureTiles.Select(x => GetCoordinateFromCellIndex(x, 8)).Join(" "));


        // Filter Treasure tiles to only keep the ones that aren't Voided
        remainingDrillableTreasureTiles = allTreasureTiles.Where(x => voidedCellsIndices.Contains(x) == false).ToList();
        summoningModule.ModuleLog(moduleId, "With the selected Void Pattern, all remaining non-voided Treasure tile indices are {0}",
            remainingDrillableTreasureTiles.Select(x => GetCoordinateFromCellIndex(x, 8)).Join(" "));
    }

    string GetLogDescriptionFromTreasureIndex(int treasureIndex)
    {
        switch (treasureIndex)
        {
            case 0: return "a croissant.";
            case 1: return "a horizontal zig-zag line.";
            case 2: return "a distorted dumbbell.";
            case 3: return "an elongated donut.";
            case 4: return "a distorted W.";
            case 5: return "a whale with a big tail.";
            case 6: return "a castle with two towers.";
            case 7: return "a backslash";
            case 8: return "a cent symbol.";
            case 9: return "a crooked 3";
        }

        return "";
    }

    void ReGeneratePuzzleAtRuntime()
    {
        GenerateAndPlaceNewTreasure();
        currentCoordinate = startingCoordinate;
        currentNumberOfDrillsDone = 0;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        summoningModule.ModuleLog(moduleId, "Received the Twitch Plays command “{0}”", command);

        // Put to lowercase and remove spaces
        command = command.ToLowerInvariant().Replace(" ", "").Replace(",", "");

        // Pressing STONE text on the center of the plate
        if (command == "s")
        {
            casingPressableButton.OnInteract();
            yield return "sendtochat {0} Successfully re-generated a puzzle.";
            yield break;
        }


        foreach (char _individualCommand in command)
        {
            yield return new WaitForSeconds(0.1f);

            switch (_individualCommand)
            {
                // Button Type Indicator:
                // 0123 is Up Down Left Right Movement to coincide with MovementDirection enum
                // 4 is Center (submit)

                case 'u':
                    platePressableButtons[0].OnInteract();
                    break;

                case 'd':
                    platePressableButtons[1].OnInteract();
                    break;

                case 'l':
                    platePressableButtons[2].OnInteract();
                    break;

                case 'r':
                    platePressableButtons[3].OnInteract();
                    break;


                // Accept "c" as Center to drill
                case 'c':
                    platePressableButtons[4].OnInteract();
                    yield return string.Format("sendtochat {0} drilling at the current position returned a {1} vibration.", "{0}", lastVibrationWasStrong ? "strong": "weak");
                    break;

                default:
                    string _stringToSend = string.Format("sendtochat {0} Received unknown character: “{1}”. To reset send a singular “s” for STONE. You currently are in {2}.",
                        "{0}", _individualCommand, GetCoordinateFromCellIndex(currentCoordinate, 8));
                    yield return _stringToSend;
                    yield break;
            }
        }

        yield break;
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {

        // An Auto-solver could be done by forcing a puzzle generation,
        // moving around and drilling everything...
        // But with void, computing the path needed to go somewhere can be a nightmare
        summoningModule.ReceiveSolve();

        yield break;
    }
}
