using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class StonePlate : PlateBase{

    readonly int[][] VoidPatterns = new int[11][]
    {
        new int[16]{ 1, 6, 8, 15, 18, 21, 27, 28, 35, 36, 42, 45, 48, 55, 57, 62 }, // Corners and X
        new int[23]{ 9, 17, 18, 21, 22, 25, 26, 29, 30, 31, 37, 38, 39, 42, 43, 46, 47, 48, 49, 50, 51, 56, 57}, // Big Blobs
        new int[22]{ 4, 6, 12, 13, 14, 20, 22, 25, 26, 27, 30, 33, 36, 37, 38, 41, 43, 49, 50, 51, 57, 59 }, // Underground tunnels
        new int[24]{ 0, 1, 3, 8, 9, 11, 12, 15, 20, 22, 23, 30, 33, 34, 35, 36, 47, 49, 50, 51, 55, 57, 62, 63 }, // Tetris Pieces
        new int[20]{ 1, 4, 5, 10, 11, 16, 17, 19, 28, 31, 32, 37, 39, 41, 46, 49, 54, 55, 58, 61}, // Cracks in reality
        new int[20]{ 1, 2, 8, 11, 16, 19, 21, 22, 23, 25, 26, 29, 31, 37, 38, 39, 43, 50, 52, 59 }, // Bubbles!
        new int[17]{ 5, 12, 13, 20, 27, 28, 33, 34, 35, 36, 40, 41, 44, 45, 54, 55, 63 }, // Windmill
        new int[20]{ 3, 4, 10, 13, 14, 18, 23, 27, 28, 30, 37, 44, 45, 50, 51, 52, 54, 57, 60, 63 }, // Torterra Tree
        new int[20]{ 2, 5, 7, 9, 10, 11, 12, 14, 17, 19, 30, 36, 37, 39, 41, 46, 48, 50, 51, 57 }, // Infernape Flame + Shoulder
        new int[19]{ 1, 8, 9, 10, 12, 17, 18, 21, 27, 30, 33, 36, 38, 42, 45, 46, 51, 52, 53 }, // Empoleon Trident
        new int[13]{ 2, 7, 12, 17, 22, 27, 32, 37, 42, 47, 52, 57, 62 } // Dots all around
    };


    /// <summary> Starting from the top-left corner of the Treasure, add the top-left corner's index
    /// to the indices in this table to get the final indices of the treasure, in an 8x8 table.</summary>
    readonly int[][] TreasurePositionOffsets = new int[17][]
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
        new int[13]{ 0, 8, 9, 17, 18, 24, 25, 26, 27, 34, 35, 41, 42 },
        new int[13]{ 0, 1, 5, 6, 9, 10, 11, 12, 13, 16, 17, 21, 22 },
        new int[15]{ 0, 2, 4, 9, 10, 11, 12, 16, 17, 18, 19, 26, 27, 33, 36 },
        new int[14]{ 0, 3, 8, 9, 10, 11, 17, 19, 20, 24, 25, 26, 27, 35 },
        new int[13]{ 2, 4, 11, 12, 16, 17, 18, 19, 24, 25, 26, 32, 34 },
        new int[13]{ 2, 3, 10, 11, 12, 17, 18, 20, 24, 25, 26, 33, 34 },
        new int[10]{ 1, 8, 10, 13, 16, 17, 20, 22, 29, 30 },
        new int[14]{ 1, 2, 3, 8, 12, 16, 20, 25, 27, 34, 35, 36, 43, 44 }
    };

    /// <summary> For each Treasure, gives the latest Column and Row allowed for the Treasure to be correctly placed.
    /// 0-indexed. This is equal to "8 - size" for width and height. </summary>
    readonly int[] furthestAllowedCoordinatesPerTreasure = new int[34]
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
        4, 2,
        1, 5,
        3, 3,
        3, 3, 
        3, 3,
        3, 3,
        1, 4,
        3, 2
    };

    /// <summary> Description of the items that can be found in the underground </summary>
    readonly string[] itemDescriptions = new string[17]
    {
        "a croissant",
        "a horizontal zig-zag line",
        "a distorted dumbbell",
        "an elongated donut",
        "a distorted W",
        "a whale with a big tail",
        "a castle with two towers",
        "a backslash",
        "a cent symbol",
        "a crooked 3",
        "an open spanner/wrench",
        "a spiky ball",
        "a turtle pointing right",
        "a raindeer looking to the right",
        "a music note",
        "a pair of rings",
        "a necklace"
    };

    int[] ruleseedPossibleVoids = new int[4];
    int[] ruleseedPossibleTreasures = new int[10];

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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch();

        if (hasPlateSolved)
        { return; }

        summoningModule.ModuleLog(moduleId, "STONE word pressed, re-generating a new puzzle.");

        ReGeneratePuzzleAtRuntime();
    }


    void PressedMovementInput(MovementDirection direction)
    {
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch(0.3f);
        PlayPlatePressSound();

        if (hasPlateSolved)
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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch(0.5f);
        PlayPlatePressSound();

        if (hasPlateSolved) { return; }

        currentNumberOfDrillsDone++;

        if (currentNumberOfDrillsDone == 20)
        {
            summoningModule.ModuleLog(moduleId, "20th drill done, Striking and resetting the puzzle");
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
        }
        // Otherwise
        else
        {
            // Vibrate slightly
            lastVibrationWasStrong = false;

            if (drillVibrationCoroutine != null) { StopCoroutine(drillVibrationCoroutine); }
            drillVibrationCoroutine = StartCoroutine(VibratePlate(1f));
        }


        summoningModule.ModuleLog(moduleId, "Drilling at {0} returned a {1} vibration. This is drill number {2}.",
                GetCoordinateFromCellIndex(currentCoordinate, 8), lastVibrationWasStrong ? "strong" : "weak", currentNumberOfDrillsDone);

        // Check if the Plate is solved
        if (remainingDrillableTreasureTiles.Count == 0)
        {
            summoningModule.ModuleLog(moduleId, "All tiles of the Treasure have been drilled out! Solving module!");
            StartCoroutine(PlateShouldSolve());
        }
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        ManageRuleseed();

        DetermineVoidPattern();

        GenerateStartingCoordinate();

        GenerateAndPlaceNewTreasure();

        currentCoordinate = startingCoordinate;
        startingCoordinateTextMesh.text = GetCoordinateFromCellIndex(startingCoordinate, 8);
    }

    void ManageRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        {
            ruleseedPossibleVoids = new int[4] { 0, 1, 2, 3 };
            ruleseedPossibleTreasures = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            return;
        }

        int[] allowedVoids = new int[11] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int[] allowedTreasures = new int[17] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };

        FisherYatesShuffle(ref allowedVoids, Rng);
        FisherYatesShuffle(ref allowedTreasures, Rng);

        Array.Copy(allowedVoids, ruleseedPossibleVoids, 4);
        Array.Copy(allowedTreasures, ruleseedPossibleTreasures, 10);

        Debug.LogFormat("<Stone Plate #{0}> Allowed Void Patterns are numbers {1}", moduleId, ruleseedPossibleVoids.Join(", "));
        Debug.LogFormat("<Stone Plate #{0}> Allowed Treasures are {1}", moduleId, ruleseedPossibleTreasures.Select(x => itemDescriptions[x]).Join(", "));
    }


    void DetermineVoidPattern()
    {
        // Void Pattern is determined by the number of indicators, from 0 to 3 they have their own pattern
        int _numberOfIndicators = bombInfo.GetIndicators().Count();
        selectedVoidPaternIndex = Mathf.Clamp(_numberOfIndicators, 0, 3);

        voidedCellsIndices.Clear();
        voidedCellsIndices = VoidPatterns[ruleseedPossibleVoids[selectedVoidPaternIndex]].ToList();

        summoningModule.ModuleLog(moduleId, "Found {0} indicators, so Void Pattern used will be number {1}", _numberOfIndicators, selectedVoidPaternIndex);
        summoningModule.ModuleLog(moduleId, "Void locations are {0}", voidedCellsIndices.Select(x => GetCoordinateFromCellIndex(x, 8)).Join());
    }

    /// <summary> Randomly select a Starting coordinate that is not Voided </summary>
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


    /// <summary> Generate a new puzzle, with a new Treasure somewhere in the map </summary>
    void GenerateAndPlaceNewTreasure()
    {
        // Determine the Treasure shape
        int _treasureIndexOnPage = UnityEngine.Random.Range(0, 9);
        selectedTreasureIndex = ruleseedPossibleTreasures[_treasureIndexOnPage];
        summoningModule.ModuleLog(moduleId, "Selected Treasure with index {0} (first in reading order is 0). It looks like {1}.",
            _treasureIndexOnPage, itemDescriptions[selectedTreasureIndex]);


        int _treasureColumn = UnityEngine.Random.Range(0, furthestAllowedCoordinatesPerTreasure[2 * selectedTreasureIndex]);
        int _treasureRow = UnityEngine.Random.Range(0, furthestAllowedCoordinatesPerTreasure[1 + 2 * selectedTreasureIndex]);

        // Generate where it'll be placed
        int _topLeftTreasureCorner = 8 * _treasureRow + _treasureColumn;
        allTreasureTiles = TreasurePositionOffsets[selectedTreasureIndex].Select(x => x + _topLeftTreasureCorner).ToList();

        // Log its placement
        summoningModule.ModuleLog(moduleId, "With top-left corner at {0} (column {1} row {2}), all of its tiles are at index {3}",
            GetCoordinateFromCellIndex(_topLeftTreasureCorner, 8), _treasureColumn, _treasureRow,
            allTreasureTiles.Select(x => GetCoordinateFromCellIndex(x, 8)).Join(" "));


        // Filter Treasure tiles to only keep the ones that aren't Voided
        remainingDrillableTreasureTiles = allTreasureTiles.Where(x => voidedCellsIndices.Contains(x) == false).ToList();
        summoningModule.ModuleLog(moduleId, "With the selected Void Pattern, all remaining non-voided Treasure tile indices are {0}",
            remainingDrillableTreasureTiles.Select(x => GetCoordinateFromCellIndex(x, 8)).Join(" "));
    }

    /// <summary> Does a Puzzle Generation while also resetting the Player Data </summary>
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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { yield break; }

        Debug.LogFormat("<Stone Plate #{0}> Received Command ''{1}''", moduleId, command);
        if (hasPlateSolved) { yield break; }

        summoningModule.ModuleLog(moduleId, "Received the Twitch Plays command “{0}”", command);

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            Debug.LogFormat("<Stone Plate #{0}> Received empty command.", moduleId);
            yield return "sendtochaterror {0} Received an empty command.";
            yield break;
        }

        // Pressing STONE text on the center of the plate
        if (commandParts[0] == "stone")
        {
            yield return null;
            casingPressableButton.OnInteract();
            yield return "sendtochat {0} Successfully re-generated a puzzle.";
            yield break;
        }

        // Failing a submit
        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            Debug.LogFormat("<Stone Plate #{0}> Received unknown command! Please use 'submit' or 'press' to submit an answer.", moduleId);
            yield return "sendtochaterror {0} Received unknown command! Please use 'submit' or 'press' to submit an answer.";
            yield break;
        }

        if (commandParts.Length > 2)
        {
            yield return null;
            Debug.LogFormat("<Stone Plate #{0}> More than one movement payload was found. Only '{1}' will be taken into account.", moduleId, commandParts[1]);
            yield return "sendtochat {0} More than one movement payload was found. Only '" + commandParts[1] + "' will be taken into account.";
        }

        yield return null;
        Debug.LogFormat("<Stone Plate #{0}> Submitting {1}", moduleId, commandParts[1]);

        foreach (char _individualCommand in commandParts[1])
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
                    Debug.LogFormat("<Stone Plate #{0}> Received unknown character: “{1}”. To reset use command “stone”. You currently are in {2}.",
                        moduleId, _individualCommand, GetCoordinateFromCellIndex(currentCoordinate, 8));
                    string _stringToSend = string.Format("sendtochaterror {0} Received unknown character: “{1}”. To reset use command “stone”. You currently are in {2}.",
                        "{0}", _individualCommand, GetCoordinateFromCellIndex(currentCoordinate, 8));
                    yield return _stringToSend;
                    yield break;
            }
        }
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {

        // An Auto-solver could be done by forcing a puzzle generation,
        // moving around and drilling everything...
        // But with void, computing the path needed to go somewhere can be a nightmare
        StartCoroutine(PlateShouldSolve());

        yield break;
    }
}
