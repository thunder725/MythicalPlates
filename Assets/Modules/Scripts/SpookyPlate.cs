using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpookyPlate : PlateBase {

    Dictionary<string, int> PlatinumTablePortToNumber = new Dictionary<string, int>() {
    { "StereoRCA", 1}, { "PS2", 7}, { "Serial", 14}, { "RJ45", 21}, { "DVI", 28}, {"Parallel", 35} };

    /// <summary>
    /// Width of each floor, to use in conjuction with MoveAroundGridWithVoid() to get the correct floor shape
    /// </summary>
    int[] floorsWidths = new int[9] { 9, 5, 7, 6, 6, 7, 6, 7, 5};

    /// <summary>
    /// Starting Cell index (blue in the manual) for all 9 floors.
    /// </summary>
    int[] floorsStartingCells = new int[9] { 35, 4, 13, 21, 1, 14, 18, 17, 31 };


    /// <summary>
    /// All 9 floors. Walkable cells are marked with a 1, empty cells with a 0. Starting cell is a 2 and ending is a 3.
    /// </summary>
    int[][] allFloors = new int[9][] { 
    
        // Floor 1 - "Hole Field 1"
        new int[45]
        {
            0, 0, 1, 0, 1, 0, 0, 0, 0,
            3, 0, 1, 1, 1, 0, 1, 1, 0,
            1, 1, 1, 1, 0, 1, 1, 1, 1,
            0, 1, 1, 0, 1, 1, 0, 1, 2,
            0, 1, 1, 1, 1, 0, 1, 1, 0
        },

        // Floor 2 - "Easy"
        new int[35]
        {
            0, 0, 1, 1, 2,
            0, 0, 1, 1, 0,
            0, 0, 1, 1, 1, 
            0, 1, 1, 1, 0,
            0, 1, 1, 0, 0,
            1, 1, 0, 0, 0,
            1, 3, 0, 0, 0
        },

        // Floor 3 - "Large Holes"
        new int[28]
        {
            1, 3, 1, 1, 1, 1, 0,
            0, 1, 0, 1, 0, 1, 2,
            1, 1, 0, 1, 0, 1, 1,
            0, 0, 0, 1, 1, 1, 1
        },

        // Floor 4 - "Double Way 1"
        new int[36]
        {
            0, 0, 1, 0, 1, 1,
            1, 1, 1, 1, 1, 1,
            1, 3, 0, 0, 0, 1,
            1, 0, 0, 2, 1, 1,
            1, 0, 1, 1, 0, 0,
            1, 1, 1, 0, 0, 0
        }, 

        // Floor 5 - "Hole Field 2"
        new int[42]
        {
            1, 2, 1, 0, 0, 0,
            1, 0, 1, 1, 1, 0,
            1, 1, 1, 0, 1, 1,
            1, 1, 0, 1, 0, 1, 
            1, 0, 1, 1, 1, 1, 
            1, 1, 1, 0, 1, 0, 
            1, 0, 1, 1, 3, 0
        },

        // Floor 6 - "Double Way 2"
        new int[42]
        {
            1, 1, 1, 1, 0, 0, 0,
            1, 1, 0, 1, 1, 1, 0,
            2, 1, 0, 0, 1, 1, 3,
            0, 1, 0, 1, 1, 0, 0,
            0, 1, 1, 1, 1, 0, 0,
            0, 0, 1, 1, 0, 0, 0
        },

        // Floor 7 - "Hook"
        new int[42]
        {
            0, 0, 3, 1, 0, 0,
            0, 0, 1, 1, 0, 1,
            0, 0, 1, 1, 1, 0, 
            2, 0, 0, 1, 1, 0,
            1, 1, 1, 0, 1, 1,
            1, 1, 0, 1, 1, 1,
            1, 1, 1, 1, 1, 0
        },

        // Floor 8 - "Inside Out"
        new int[49]
        {
            0, 1, 1, 1, 0, 1, 0,
            0, 1, 1, 1, 1, 1, 1,
            1, 1, 0, 2, 1, 0, 1,
            1, 0, 1, 0, 1, 0, 1,
            1, 1, 0, 0, 0, 0, 1,
            0, 1, 1, 1, 0, 1, 1,
            0, 1, 1, 3, 1, 1, 0
        },

        // Floor 9 - "Minimalist"
        new int[35]
        {
            0, 1, 1, 1, 1, 
            1, 1, 0, 1, 3,
            1, 1, 0, 0, 0,
            0, 1, 1, 1, 0,
            1, 0, 1, 1, 0,
            1, 1, 1, 1, 0,
            0, 2, 1, 0, 0
        }
    };


    /// <summary>
    /// Array containing all individual Cells that should be voided for every Line in every Floor
    /// This is a bit messy, but trying to figure it out mathematically too would be messy since information about
    /// whether it's a row or column + the correct index should be stored, to then compute how many to void and then which ones...
    /// </summary>
    int[][] allVoidedLines = new int[63][] { 

        // Floor 1 - "Hole Field 1"
        new int[5]{ 1, 10, 19, 28, 37},
        new int[5]{ 2, 11, 20, 29, 38},
        new int[5]{ 3, 12, 21, 30, 39},
        new int[5]{ 4, 13, 22, 31, 40},
        new int[5]{ 5, 14, 23, 32, 41},
        new int[5]{ 6, 15, 24, 33, 42},
        new int[5]{ 7, 16, 25, 34, 43},

        // Floor 2 - "Easy"
        new int[5]{ 5, 6, 7, 8, 9},
        new int[5]{ 10, 11, 12, 13, 14},
        new int[5]{ 15, 16, 17, 18, 19},
        new int[5]{ 20, 21, 22, 23, 24},
        new int[5]{ 25, 26, 27, 28, 29},
        new int[7]{ 2, 7, 12, 17, 22, 27, 32},
        new int[7]{ 3, 8, 13, 18, 23, 28, 33},

        // Floor 3 - "Large Holes"
        new int[7]{ 14, 15, 16, 17, 18, 19, 20 },
        new int[7]{ 21, 22, 23, 24, 25, 26, 27 },
        new int[4]{ 0, 7, 14, 21 },
        new int[4]{ 2, 9, 16, 23 },
        new int[4]{ 3, 10, 17, 24 },
        new int[4]{ 4, 11, 18, 25 },
        new int[4]{ 5, 12, 19, 26 },

        // Floor 4 - "Double Way 1"
        new int[6]{ 0, 6, 12, 18, 24, 30 },
        new int[6]{ 2, 8, 14, 20, 26, 32 },
        new int[6]{ 4, 10, 16, 22, 28, 34 },
        new int[6]{ 5, 11, 17, 23, 29, 35 },
        new int[6]{ 6, 7, 8, 9, 10, 11 },
        new int[6]{ 24, 25, 26, 27, 28, 29 },
        new int[6]{ 30, 31, 32, 33, 34, 35 },

        // Floor 5 - "Hole Field 2"
        new int[7]{ 2, 8, 14, 20, 26, 32, 38 },
        new int[7]{ 3, 9, 15, 21, 27, 33, 39 },
        new int[6]{ 6, 7, 8, 9, 10, 11 },
        new int[6]{ 12, 13, 14, 15, 16, 17 },
        new int[6]{ 18, 19, 20, 21, 22, 23 },
        new int[6]{ 24, 25, 26, 27, 28, 29 },
        new int[6]{ 30, 31, 32, 33, 34, 35 },

        // Floor 6 - "Double Way 2"
        new int[7]{ 0, 1, 2, 3, 4, 5, 6 },
        new int[7]{ 7, 8, 9, 10, 11, 12, 13 },
        new int[7]{ 21, 22, 23, 24, 25, 26, 27 },
        new int[6]{ 1, 8, 15, 22, 29, 36 },
        new int[6]{ 2, 9, 16, 23, 30, 37 },
        new int[6]{ 3, 10, 17, 24, 31, 38 },
        new int[6]{ 4, 11, 18, 25, 32, 39 },

        // Floor 7 - "Hook"
        new int[6]{ 6, 7, 8, 9, 10, 11 },
        new int[6]{ 12, 13, 14, 15, 16, 17 },
        new int[6]{ 24, 25, 26, 27, 28, 29 },
        new int[6]{ 30, 31, 32, 33, 34, 35 },
        new int[7]{ 1, 7, 13, 19, 25, 31, 37 },
        new int[7]{ 3, 9, 15, 21, 27, 33, 39 },
        new int[7]{ 5, 11, 17, 23, 29, 35, 41 },

        // Floor 8 - "Inside Out"
        new int[7]{ 1, 8, 15, 22, 29, 36, 43 },
        new int[7]{ 2, 9, 16, 23, 30, 37, 44 },
        new int[7]{ 5, 12, 19, 26, 33, 40, 47 },
        new int[7]{ 6, 13, 20, 27, 34, 41, 48 },
        new int[7]{ 7, 8, 9, 10, 11, 12, 13 },
        new int[7]{ 28, 29, 30, 31, 32, 33, 34 },
        new int[7]{ 35, 36, 37, 38, 39, 40, 41 },

        // Floor 9 - "Minimalist"
        new int[7]{ 0, 5, 10, 15, 20, 25, 30 },
        new int[7]{ 2, 7, 12, 17, 22, 27, 32 },
        new int[7]{ 3, 8, 13, 18, 23, 28, 33 },
        new int[5]{ 10, 11, 12, 13, 14 },
        new int[5]{ 15, 16, 17, 18, 19 },
        new int[5]{ 20, 21, 22, 23, 24 },
        new int[5]{ 25, 26, 27, 28, 29 }
    };




    int floorOneNumber, floorTwoNumber, floorThreeNumber;
    int voidedLineOne, voidedLineTwo, voidedLineThree;

    int currentFloorNumber, currentFloorIndex;
    int currentPlayerCellIndex;

    [SerializeField] TextMesh floorNumbersText;

    [SerializeField] AudioClip newFloorSoundClip;
    Coroutine FloorResetSoundPlayingCoroutine;

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

    void PressedMovementInput(MovementDirection pressedDirection)
    {
        if (summoningModule.isModuleSolved) { return; }

        // Feedback
        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch(.3f);


        // Save the pre-movement location in case we need to cancel movement.
        int _preMovementLocation = currentPlayerCellIndex;

        VoidMovementData _movementData = MoveAroundGridWithVoid(pressedDirection, GetCurrentFloor().Length, ref currentPlayerCellIndex, GetCurrentFloorWidth(), false);

        // If ran outside of the edges
        if (_movementData.ranIntoGridEdges)
        {
            summoningModule.ModuleLog(moduleId, "Movement {0} from {1} landed outside of the bounds of the floor. Movement has been cancelled and Strike has been given.",
                pressedDirection, GetCoordinateFromCellIndex(_preMovementLocation, GetCurrentFloorWidth()));

            currentPlayerCellIndex = _preMovementLocation;
            summoningModule.ReceiveStrike();

            return;
        }
        // Ditto, for landing in a 0-Tile
        // We only check after the movement, players can go through empty cells if they are Voided, on purpose.
        else if (GetCurrentCellContent() == 0)
        {
            summoningModule.ModuleLog(moduleId, "Movement {0} from {1} landed in {2}, which is an invalid cell. Movement has been cancelled and Strike has been given.",
                            pressedDirection, GetCoordinateFromCellIndex(_preMovementLocation, GetCurrentFloorWidth()), GetCoordinateFromCellIndex(currentPlayerCellIndex, GetCurrentFloorWidth()));

            currentPlayerCellIndex = _preMovementLocation;
            summoningModule.ReceiveStrike();

            return;
        }
        // If we land on the final cell of the floor
        else if (GetCurrentCellContent() == 3)
        {
            // Go to the next floor
            currentFloorIndex++;

            // Congrats, you passed all 3 floors!
            if (currentFloorIndex == 3)
            {
                summoningModule.ModuleLog(moduleId, "Movement {0} from {1} arrived at the end of the current floor, which was the last one. Module solved",
                    pressedDirection, GetCoordinateFromCellIndex(_preMovementLocation, GetCurrentFloorWidth()));

                summoningModule.ReceiveSolve();
                return;
            }

            // Otherwise, you still passed a floor, that's coolio!
            summoningModule.ModuleLog(moduleId, "Movement {0} from {1} arrived at the end of the current floor.",
                    pressedDirection, GetCoordinateFromCellIndex(_preMovementLocation, GetCurrentFloorWidth()));

            // Go to the next floor
            LandInNewFloor(currentFloorIndex);
            summoningModule.PlaySound(newFloorSoundClip);
            return;
        }

        // Didn't escape current floor, but we still moved

        summoningModule.ModuleLog(moduleId, "Movement {0} from {1} landed in {2}.",
                    pressedDirection, GetCoordinateFromCellIndex(_preMovementLocation, GetCurrentFloorWidth()), GetCoordinateFromCellIndex(currentPlayerCellIndex, GetCurrentFloorWidth()));
    }


    protected override void CasingTextButtonGetsPressed()
    {
        if (summoningModule.isModuleSolved) { return; }

        // Feedback
        PlayPlatePressSound();
        platePressableButtons[4].AddInteractionPunch(.3f);


        LandInNewFloor(currentFloorIndex);


        // Start the sound-playing Coroutine to indicate the current floor

        // Be careful to not overlap sound playing
        if (FloorResetSoundPlayingCoroutine != null)
        {
            StopCoroutine(FloorResetSoundPlayingCoroutine);
        }

        FloorResetSoundPlayingCoroutine = StartCoroutine(PlayFloorResetSounds(currentFloorIndex + 1));
    }


    IEnumerator PlayFloorResetSounds(int numberOfSoundsToPlay)
    {
        // Play the sounds one after another
        for (int i = 0; i < numberOfSoundsToPlay; i ++)
        {
            summoningModule.PlaySound(newFloorSoundClip);

            yield return new WaitForSeconds(newFloorSoundClip.length + 0.1f);
        }

        yield return null;
    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        GenerateFloorOrder();

        DetermineVoidedLines();

        // First Floor
        currentFloorIndex = 0;
        LandInNewFloor(currentFloorIndex);
    }


    void GenerateFloorOrder()
    {
        // Generate the 3 possible Floors, without repeats
        int[] possibleFloors = new int[9] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }.Shuffle();

        floorOneNumber = possibleFloors[0];
        floorTwoNumber = possibleFloors[1];
        floorThreeNumber = possibleFloors[2];

        summoningModule.ModuleLog(moduleId, "The floors you will traverse will be Floors {0}, then {1}, then {2}.", floorOneNumber, floorTwoNumber, floorThreeNumber);

        floorNumbersText.text = floorOneNumber + " " + floorTwoNumber + " " + floorThreeNumber;
    }


    void DetermineVoidedLines()
    {
        // Concatenate all Serial Number digits as well as 487.
        string _numbersAsString = bombInfo.GetSerialNumberNumbers().Join("") + "487";

        // Get Digital Root
        int _resultingNumber = DigitalRoot(int.Parse(_numbersAsString));

        // There is however a specific rule of "if 9, use 7"
        // Also, log
        if (_resultingNumber == 9)
        {
            summoningModule.ModuleLog(moduleId, "Digital Root of the concatenated {0} is {1}. A 7 will be used instead.", _numbersAsString, _resultingNumber);
            _resultingNumber = 7;
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Digital Root of the concatenated {0} is {1}.", _numbersAsString, _resultingNumber);
        }



        // Then, for each unique Port Type, add a number.
        var _allPorts = bombInfo.GetPorts();
        if (_allPorts.Count() == 0)
        {
            summoningModule.ModuleLog(moduleId, "Found no ports, so no value has been added to the number.");
        }
        else
        {
            // Get all unique Port Types
            string[] _allPortTypes = _allPorts.Distinct().ToArray();

            // Log
            summoningModule.ModuleLog(moduleId, "Found a total of {0} distinct Port Types:", _allPortTypes.Length);

            // For each Port Type, add the number
            int _numberToAdd = 0;
            foreach (string _portType in _allPortTypes)
            {
                // Get the associated value with the Port
                // It looks like they all are multiples of 7... How strange :D
                PlatinumTablePortToNumber.TryGetValue(_portType, out _numberToAdd);

                // Add the value
                _resultingNumber += _numberToAdd;

                // Log
                summoningModule.ModuleLog(moduleId, "Found port type {0}, which adds {1}. Current number is {2}.", _portType, _numberToAdd, _resultingNumber);
            }
        }
        


        // Get the number of Lit and Unlit indicators
        int _numberOfLits = bombInfo.GetOnIndicators().Count();
        int _numberOfUnlits = bombInfo.GetOffIndicators().Count();

        // More Unlits, multiply by 3
        if (_numberOfUnlits > _numberOfLits)
        {
            _resultingNumber *= 3;

            summoningModule.ModuleLog(moduleId, "Found a total of {0} Unlit indicaotrs and {1} Lits. There are more Unlits, so the number is multiplied by 3 to get {2}",
                _numberOfUnlits, _numberOfLits, _resultingNumber);
        }
        // Otherwise, if equal, multiply by 2
        else if (_numberOfUnlits == _numberOfLits)
        {
            _resultingNumber *= 2;

            summoningModule.ModuleLog(moduleId, "Found a total of {0} Unlit indicaotrs and {1} Lits. There are as many Unlits as Lits, so the number is multiplied by 2 to get {2}",
                _numberOfUnlits, _numberOfLits, _resultingNumber);
        }
        // Otherwise, don't do anything
        else
        {
            summoningModule.ModuleLog(moduleId, "Found a total of {0} Unlit indicaotrs and {1} Lits. There are more Lits, so the number is unchanged and stays {2}",
                _numberOfUnlits, _numberOfLits, _resultingNumber);
        }



        // Convret to base 7 and get a number between 000 and 666 with all 3 digits kept
        string _resultingBaseSeven = "";

        // Hundreds digit
        voidedLineOne += Mathf.FloorToInt(_resultingNumber / 49);
        _resultingBaseSeven += voidedLineOne;

        // Tens digit
        voidedLineTwo += Mathf.FloorToInt((_resultingNumber % 49) / 7);
        _resultingBaseSeven += voidedLineTwo;

        // Digit
        voidedLineThree += _resultingNumber % 7;
        _resultingBaseSeven += voidedLineThree;

        summoningModule.ModuleLog(moduleId, "Final number is {0} in base-10, which is {1} in base-7 (keeping all 3 digits).", _resultingNumber, _resultingBaseSeven);
        summoningModule.ModuleLog(moduleId, "You'll first traverse Floor {0} with Line {1} voided, then Floor {2} with Line {3} voided, then finally Floor {4} with Line {5} voided.",
            floorOneNumber, voidedLineOne, floorTwoNumber, voidedLineTwo, floorThreeNumber, voidedLineThree);
    }


    void LandInNewFloor(int floorIndex)
    {

        // Register floor number
        currentFloorNumber = GetFloorNumberFromIndex(floorIndex);

        // Offset by 1 because Number and Index
        currentPlayerCellIndex = floorsStartingCells[currentFloorNumber - 1];

        summoningModule.ModuleLog(moduleId, "Landing in floor number {0} at Starting Location {1}.",
            currentFloorNumber, GetCoordinateFromCellIndex(GetCurrentFloor()[currentPlayerCellIndex], GetCurrentFloorWidth()));


        // This also manages the Voiding of lines, since they must change every time we land in a new floor
        voidedCellsIndices.Clear();
        
        // All Voided Lines are indexed at FloorIndex*7 + VoidexLineIndex
        // Remember that we currently get the Floor Number [1-9] not the Floor Index [0-8], so offset that before multiplying
        voidedCellsIndices = allVoidedLines[(currentFloorNumber - 1) * 7 + GetVoidedLineIndexFromFloorIndex(floorIndex)].ToList();
    }

    int[] GetCurrentFloor()
    {
        return allFloors[currentFloorNumber - 1];
    }

    int GetCurrentCellContent()
    {
        return GetCurrentFloor()[currentPlayerCellIndex];
    }

    int GetCurrentFloorWidth()
    {
        return floorsWidths[currentFloorNumber - 1];
    }

    int GetFloorNumberFromIndex(int floorIndex)
    {
        switch (currentFloorIndex)
        {
            case 0: return floorOneNumber;
            case 1: return floorTwoNumber;
            case 2: return floorThreeNumber;
        }

        return 0;
    }

    int GetVoidedLineIndexFromFloorIndex(int floorIndex)
    {
        switch (currentFloorIndex)
        {
            case 0: return voidedLineOne;
            case 1: return voidedLineTwo;
            case 2: return voidedLineThree;
        }

        return 0;
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

        // Either SPOOKY
        if (commandParts[0] == "spooky")
        {
            yield return "sendtochat {0} Successfully pressed SPOOKY and reset the module.";
            CasingTextButtonGetsPressed();
            yield break;
        }

        // Accept the words "submit", "move", "press", or their initials
        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "move" && commandParts[0] != "m" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Unrecognized command. Please use 'spooky' to reset module, or 'move', 'submit' or 'press' to move around.";
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
            yield return "sendtochat {0} More than one movement payload was found. Only '" + commandParts[1] + "' will be taken into account.";
        }


        foreach (char _movementDirection in commandParts[1])
        {
            yield return new WaitForSeconds(0.15f);

            switch (_movementDirection)
            {
                case 'u':
                    platePressableButtons[0].OnInteract();
                    break;

                case 'r':
                    platePressableButtons[3].OnInteract();
                    break;

                case 'd':
                    platePressableButtons[1].OnInteract();
                    break;

                case 'l':
                    platePressableButtons[2].OnInteract();
                    break;
            }
        }




        yield return null;
    }

    public override IEnumerator TwitchHandleForcedSolve()
    {
        // 
        summoningModule.ReceiveSolve();

        yield break;
    }
}
