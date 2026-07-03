using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MindPlate : PlateBase
{
    /// <summary> Representation of the Rubik's Cube that will get scrambled. Its numbers are 11 through 69, ignoring multiples of 10.
    /// Face I has numbers 11 12 13 14 15 16 17 18 19 in reading order, so when it gets scrambled we instantly know where it came from.
    /// This is just a representation of its original placement. The new location is represented in the number's index in the array.
    /// The format III-B1 is for the new location only, and is player-facing.</summary>
    readonly int[] rubiksCube = new int[54] {
        11, 12, 13, 14, 15, 16, 17, 18, 19,
        21, 22, 23, 24, 25, 26, 27, 28, 29,
        31, 32, 33, 34, 35, 36, 37, 38, 39,
        41, 42, 43, 44, 45, 46, 47, 48, 49,
        51, 52, 53, 54, 55, 56, 57, 58, 59,
        61, 62, 63, 64, 65, 66, 67, 68, 69 };

    /// <summary> Due to the weird nature of walking on a Cube, and the poor mapping decision in how to number indices,
    /// moving to the Left can bring you to index X-1, X-7, X-34 or X+7 depending on where you started.
    /// To avoid many, many headaches and stupid code with dozens of switches, a lookup table will be used instead.
    /// Input the movement Direction (U D L R) times 54, + starting index (0-53), and you get the index where you land.
    /// But to make my life even simpler, I added the info of the new Rotation (relative to the previous direction at least)!
    /// It is the Hundreds digit, using the same order as all MovementDirections have used from the start (UDLR).</summary>
    readonly int[] postMovementInformationLookupTable = new int[216]
    {
        // Indices 0-53 => Moving Up in the Net
            // Face UP
        138, 137, 136, 000, 001, 002, 003, 004, 005,
            // Face LEFT
        300, 303, 306, 009, 010, 011, 012, 013, 014,
            // Face FRONT
        006, 007, 008, 018, 019, 020, 021, 022, 023,
            // Face RIGHT
        208, 205, 202, 027, 028, 029, 030, 031, 032,
            // Face BACK
        102, 101, 100, 036, 037, 038, 039, 040, 041,
            // Face BOTTOM
        024, 025, 026, 045, 046, 047, 048, 049, 050,

        // Indices 54-107 => Moving Down in the Net
             // Face UP
        003, 004, 005, 006, 007, 008, 018, 019, 020,
            // Face LEFT
        012, 013, 014, 015, 016, 017, 251, 248, 245,
            // Face FRONT
        021, 022, 023, 024, 025, 026, 045, 046, 047,
            // Face RIGHT
        030, 031, 032, 033, 034, 035, 347, 350, 353,
            // Face BACK
        039, 040, 041, 042, 043, 044, 153, 152, 151,
            // Face BOTTOM
        048, 049, 050, 051, 052, 053, 144, 143, 142,

        // Indices 108-161 => Moving Left in the Net
             // Face UP
        209, 000, 001, 210, 003, 004, 211, 006, 007,
            // Face LEFT
        038, 009, 010, 041, 012, 013, 044, 015, 016,
            // Face FRONT
        011, 018, 019, 014, 021, 022, 017, 024, 025,
            // Face RIGHT
        020, 027, 028, 023, 030, 031, 026, 033, 034,
            // Face BACK
        029, 036, 037, 032, 039, 040, 035, 042, 043,
            // Face BOTTOM
        317, 045, 046, 316, 048, 049, 315, 051, 052,

        // Indices 162-215 => Moving Right in the net
             // Face UP
        001, 002, 329, 004, 005, 328, 007, 008, 327,
            // Face LEFT
        010, 011, 018, 013, 014, 021, 016, 017, 024,
            // Face FRONT
        019, 020, 027, 022, 023, 030, 025, 026, 033,
            // Face RIGHT
        028, 029, 036, 031, 032, 039, 034, 035, 042,
            // Face BACK
        037, 038, 009, 040, 041, 012, 043, 044, 015,
            // Face BOTTOM
        046, 047, 233, 049, 050, 234, 052, 053, 235
    };

    /// <summary> Enum representing the 12 possible individual Scrambling Moves </summary>
    enum ScramblingMove { Up, UpPrime, Left, LeftPrime, Front, FrontPrime, Right, RightPrime, Back, BackPrime, Down, DownPrime };
    enum Constellation { Latias, TapuLele, Mew, Uxie};

    /// <summary> Content of Appendix ANISTAR in the manual, for the coordinates of the Stars </summary>
    readonly int[][] StarCoordinatesPerConstellation = new int[4][] {
        new int[7] { 16, 25, 27, 34, 43, 48, 55},
        new int[7] { 16, 27, 32, 43, 45, 57, 66},
        new int[7] { 13, 17, 22, 24, 37, 43, 66},
        new int[7] { 14, 19, 23, 39, 54, 59, 65}
    };
    /// <summary> Content of Appendix ANISTAR in the manual, for the coordinates of the Voided tiles </summary>
    readonly int[][] VoidCoordinatesPerConstellation = new int[4][] {
        new int[6] { 14, 22, 39, 49, 53, 61 },
        new int[6] { 13, 23, 37, 42, 55, 69 },
        new int[6] { 19, 27, 36, 57, 62, 67 },
        new int[6] { 17, 25, 44, 46, 52, 64 }
    };

    /// <summary> Content of Table PSI in the manual </summary>
    readonly ScramblingMove[][] ScramblingMovesPerDigit = new ScramblingMove[10][] {
        new ScramblingMove[2] { ScramblingMove.Up, ScramblingMove.FrontPrime},
        new ScramblingMove[2] { ScramblingMove.Down, ScramblingMove.LeftPrime},
        new ScramblingMove[2] { ScramblingMove.Back, ScramblingMove.RightPrime},
        new ScramblingMove[2] { ScramblingMove.BackPrime, ScramblingMove.UpPrime},
        new ScramblingMove[2] { ScramblingMove.Right, ScramblingMove.Right},
        new ScramblingMove[2] { ScramblingMove.LeftPrime, ScramblingMove.UpPrime},
        new ScramblingMove[2] { ScramblingMove.RightPrime, ScramblingMove.Left},
        new ScramblingMove[2] { ScramblingMove.Front, ScramblingMove.DownPrime},
        new ScramblingMove[2] { ScramblingMove.Down, ScramblingMove.Front},
        new ScramblingMove[2] { ScramblingMove.Left, ScramblingMove.Back}
    };
    readonly string[] romanNumeralsToSix = new string[6] { "I", "II", "III", "IV", "V", "VI" };


    /// <summary> The 4 textures for the 4 possible Constellations </summary>
    [SerializeField] Texture2D[] constellationTextures;
    [SerializeField] MeshRenderer constellationRenderer;
    Material constellationMaterial;

    int[] allSevenStarsNumbers = new int[7];
    Constellation selectedConstellation;

    Coroutine movementVibrationCoroutine;

    int currentPlayerLocationIndex;
    /// <summary> Representation of where Up moves the player, when seeing the Net. </summary>
    MovementDirection currentPlayerUpDirection;

    /// <summary> List of the Collected Stars, as tile Numbers </summary>
    List<int> CollectedStarsNumbers = new List<int>();


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
        platePressableButtons[4].OnInteract += delegate () { PressedCentralButton(); return false; };

        // Create a new instance of the Constellation Material so multiple Mind Plates don't end up with the same picture
        constellationMaterial = Instantiate<Material>(constellationRenderer.material);
        constellationRenderer.material = constellationMaterial;
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
        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch(.3f);

        if (summoningModule.isModuleSolved) { return; }

        MoveOnRubiksCubeInDirection(pressedDirection);
    }

    /// <summary> The "Submit" button. </summary>
    void PressedCentralButton()
    {
        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch(.7f);

        if (summoningModule.isModuleSolved) { return; }

        // Is there no Star?
        // Vibration but no strike
        if (allSevenStarsNumbers.Contains(rubiksCube[currentPlayerLocationIndex]) == false)
        {
            if (movementVibrationCoroutine != null) { StopCoroutine(movementVibrationCoroutine); }
            movementVibrationCoroutine = StartCoroutine(VibratePlate(2));

            summoningModule.ModuleLog(moduleId, "Pressed the Central Button but no Star is present in {0}!", GetAnistarCoordinateFormatting(currentPlayerLocationIndex));

            return;
        }


        // Is there a Star that's already Present?
        // Vibration but no strike
        if (CollectedStarsNumbers.Contains(rubiksCube[currentPlayerLocationIndex]))
        {
            if (movementVibrationCoroutine != null) { StopCoroutine(movementVibrationCoroutine); }
            movementVibrationCoroutine = StartCoroutine(VibratePlate(2));

            summoningModule.ModuleLog(moduleId, "Pressed the Central Button to submit the Star in {0} but it was already submitted!", GetAnistarCoordinateFormatting(currentPlayerLocationIndex));

            return;
        }



        // Else we have a new star to collect.
        CollectedStarsNumbers.Add(rubiksCube[currentPlayerLocationIndex]);

        if (CollectedStarsNumbers.Count == 7)
        {
            summoningModule.ModuleLog(moduleId, "Successfully submitted Star found in {0}. All Stars have been submitted. Solving module",
                GetAnistarCoordinateFormatting(currentPlayerLocationIndex));
            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Successfully submitted Star found in {0}.", GetAnistarCoordinateFormatting(currentPlayerLocationIndex));
        }
    }


    protected override void CasingTextButtonGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        summoningModule.ModuleLog(moduleId, "MIND text button pressed. Resetting the module to its initial state.");
        ResetValuesToStart();
    }


    void MoveOnRubiksCubeInDirection(MovementDirection pressedDirection)
    {
        // Which direction should we Actually move towards?
        MovementDirection _directionToGoInto = GetMovementDirectionFromInput(pressedDirection);

        summoningModule.ModuleLog(moduleId, "From {0}, pressed Direction button {1}. Due to current orientation, that represents a {2} on the net.",
            GetAnistarCoordinateFormatting(currentPlayerLocationIndex), pressedDirection, _directionToGoInto);


        // Movement, WITH VOID!
        // So it's while loop time

        bool _searchingForAnswer = true;
        int _failsafe = 0;
        int _resultLocationIndex = 0;
        
        while (_searchingForAnswer)
        {
            _failsafe++;
            if (_failsafe > 25)
            {
                summoningModule.ModuleLogError(moduleId, "Reached 25 movement iteration while trying to move. Stopping here. Something might be wrong!!");
                return;
            }




            // This is the part in the code where we might cry, because LEFT is not the same for every case
            // Of course because of the faces
            // In this case, LEFT when in the center of a face brings you to index x-1
            // But on the left edge of a face... It can be x-7 for easy cases (face FRONT to face LEFT)
            // But on some other cases it's pretty bad as it can be x-32 (face BOTTOM left to face LEFT) or even x+7 (face UP left to face LEFT)
            // The easiest way I can see right now is to just hard-code face transitions... Pretty much coding movements from any edge to any *other* edge
            // Maybe there is a formula for that, but I can't think of an algorithm that would work!
            // Then there is the whole "figure out the new facing Direction from old Face, new Face, and movement Direction" 

            // What if we solve both problems with the same solution: A Lookup Table?
            // MovementDirection (UDLR) * 54  + current index
            _resultLocationIndex = postMovementInformationLookupTable[(int)_directionToGoInto * 54 + currentPlayerLocationIndex];

            // Last two digits are the new index
            currentPlayerLocationIndex = _resultLocationIndex % 100;

            // Hundreds digit is the new Orientation
            int _newOrientation = Mathf.FloorToInt(_resultLocationIndex / 100);

            // No new Orientation
            if (_newOrientation == 0)
            {
                summoningModule.ModuleLog(moduleId, "Landed in {0}, without any orientation change relative to the Net (current Up is {1}).",
                    GetAnistarCoordinateFormatting(currentPlayerLocationIndex), currentPlayerUpDirection);
            }
            // New Orientation needed, switch it up!
            else
            {
                currentPlayerUpDirection = GetMovementDirectionFromInput((MovementDirection)_newOrientation);

                summoningModule.ModuleLog(moduleId, "Landed in {0}, with new 'Up' orientation being {1} relative to the Net.",
                    GetAnistarCoordinateFormatting(currentPlayerLocationIndex), currentPlayerUpDirection);
            }


            // Last 2 digits are the new index
            // So verify Void presence
            if (voidedCellsIndices.Contains(rubiksCube[currentPlayerLocationIndex]))
            {
                summoningModule.ModuleLog(moduleId, "{0} is Voided, so another Movement must be performed.",
                    GetAnistarCoordinateFormatting(currentPlayerLocationIndex));

                // In that case, potentially change orientation
                if (_newOrientation != 0)
                {
                    // Keep the pressed direction, but since the UpDirection has changed, it will update itself automatically
                    _directionToGoInto = GetMovementDirectionFromInput(pressedDirection);
                }
                continue;
            }
            else
            {
                summoningModule.ModuleLog(moduleId, "{0} is not Voided.",
                    GetAnistarCoordinateFormatting(currentPlayerLocationIndex));

                // Exit Loop
                _searchingForAnswer = false;
                continue;
            }

        }

        // Outside of While Loop
    }

    MovementDirection GetMovementDirectionFromInput(MovementDirection input)
    {
        // If current Up direction is Up, no turning around is needed
        if (currentPlayerUpDirection == MovementDirection.Up)
        { return input; }


        // Otherwise, pressing "Up" just moves them in the direction they're turned
        if (input == MovementDirection.Up)
        { return currentPlayerUpDirection; }

        // Otherwise...

        switch (currentPlayerUpDirection)
        {
            case MovementDirection.Right:
                switch (input)
                {
                    case MovementDirection.Right:
                        return MovementDirection.Down;

                    case MovementDirection.Left:
                        return MovementDirection.Up;

                    case MovementDirection.Down:
                        return MovementDirection.Left;
                }
                break;


            case MovementDirection.Down:
                switch (input)
                {
                    case MovementDirection.Right:
                        return MovementDirection.Left;

                    case MovementDirection.Left:
                        return MovementDirection.Right;

                    case MovementDirection.Down:
                        return MovementDirection.Up;
                }
                break;


            case MovementDirection.Left:
                switch (input)
                {
                    case MovementDirection.Right:
                        return MovementDirection.Up;

                    case MovementDirection.Left:
                        return MovementDirection.Down;

                    case MovementDirection.Down:
                        return MovementDirection.Right;
                }
                break;
        }

        // Shouldn't happen
        summoningModule.ModuleLogError(moduleId, "Uh-oh, tried to convert {0} with current {1} and landed at the bottom??", input, currentPlayerUpDirection);
        return MovementDirection.Up;
    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        SelectConstellation();

        summoningModule.ModuleLog(moduleId, "Starting Cube State:");
        LogRubiksCube();

        DetermineScramblingMovesToExecute();

        ResetValuesToStart();
    }

    void SelectConstellation()
    {
        // Randomly select Constellation
        selectedConstellation = (Constellation)UnityEngine.Random.Range(0, 4);

        // Initialize the location of Void and Stars
        voidedCellsIndices = new List<int>(VoidCoordinatesPerConstellation[(int)selectedConstellation]);
        allSevenStarsNumbers = StarCoordinatesPerConstellation[(int)selectedConstellation].ToArray();

        // Show Constellation
        constellationMaterial.SetTexture("_MainTex", constellationTextures[(int)selectedConstellation]);

        // Prepare string for logging
        string _ordinal = string.Empty;
        switch (selectedConstellation)
        {
            case Constellation.Latias: _ordinal = "1st"; break;
            case Constellation.TapuLele: _ordinal = "2nd"; break;
            case Constellation.Mew: _ordinal = "3rd"; break;
            case Constellation.Uxie: _ordinal = "4th"; break;
        }

        // Log
        summoningModule.ModuleLog(moduleId, "Selected constellation is {0}, which is the {1} in reading order in the manual.", selectedConstellation, _ordinal);
    }

    void DetermineScramblingMovesToExecute()
    {
        // Get Edgework
        int _numberOfBatteries = bombInfo.GetBatteryCount() % 10;
        int _numberOfIndicators = bombInfo.GetIndicators().Count() % 10;
        int _numberOfPorts = bombInfo.GetPortCount() % 10;
        int[] _serialNumberDigits = bombInfo.GetSerialNumberNumbers().ToArray();

        // Log Edgework
        summoningModule.ModuleLog(moduleId, "Found {0} Batteries, {1} Indicators and {2} Ports (only keeping least significant digit). Serial Number Digits are {3}.",
            _numberOfBatteries, _numberOfIndicators, _numberOfPorts, _serialNumberDigits.Join());

        // Create and populate list of Scrambling Moves
        List<ScramblingMove> _movesToPerform = new List<ScramblingMove>();

        _movesToPerform.AddRange(ScramblingMovesPerDigit[_numberOfBatteries]);
        _movesToPerform.AddRange(ScramblingMovesPerDigit[_numberOfIndicators]);
        _movesToPerform.AddRange(ScramblingMovesPerDigit[_numberOfPorts]);
        for (int i = 0; i < _serialNumberDigits.Length; i ++)
        {
            _movesToPerform.AddRange(ScramblingMovesPerDigit[_serialNumberDigits[i]]);
        }

        summoningModule.ModuleLog(moduleId, "Scrambling Moves, in order, will be {0}", _movesToPerform.Join());

        foreach (ScramblingMove _move in _movesToPerform)
        {
            PerformScramblingMove(_move);
        }

    }

    void PerformScramblingMove(ScramblingMove scramblingMove)
    {
        int[] _tempRubiksCubeCopy = rubiksCube.ToArray();

        switch(scramblingMove)
        {
            case ScramblingMove.Up:
                // Up Face
                rubiksCube[0] = _tempRubiksCubeCopy[6];
                rubiksCube[1] = _tempRubiksCubeCopy[3];
                rubiksCube[2] = _tempRubiksCubeCopy[0];
                rubiksCube[3] = _tempRubiksCubeCopy[7];
                rubiksCube[5] = _tempRubiksCubeCopy[1];
                rubiksCube[6] = _tempRubiksCubeCopy[8];
                rubiksCube[7] = _tempRubiksCubeCopy[5];
                rubiksCube[8] = _tempRubiksCubeCopy[2];
                // Connected Edges Left
                rubiksCube[9] = _tempRubiksCubeCopy[18];
                rubiksCube[10] = _tempRubiksCubeCopy[19];
                rubiksCube[11] = _tempRubiksCubeCopy[20];
                // Connected Edges Front
                rubiksCube[18] = _tempRubiksCubeCopy[27];
                rubiksCube[19] = _tempRubiksCubeCopy[28];
                rubiksCube[20] = _tempRubiksCubeCopy[29];
                // Connected Edges Right
                rubiksCube[27] = _tempRubiksCubeCopy[36];
                rubiksCube[28] = _tempRubiksCubeCopy[37];
                rubiksCube[29] = _tempRubiksCubeCopy[38];
                // Connected Edges Back
                rubiksCube[36] = _tempRubiksCubeCopy[9];
                rubiksCube[37] = _tempRubiksCubeCopy[10];
                rubiksCube[38] = _tempRubiksCubeCopy[11];
                break;

            case ScramblingMove.UpPrime:
                // Primes are easy, take the non-Prime, and flip the numbers on each sides of the =
                // If in Up you have [3] = [7], in UpPrime you have [7] = [3]
                // Up Face
                rubiksCube[0] = _tempRubiksCubeCopy[2];
                rubiksCube[1] = _tempRubiksCubeCopy[5];
                rubiksCube[2] = _tempRubiksCubeCopy[8];
                rubiksCube[3] = _tempRubiksCubeCopy[1];
                rubiksCube[5] = _tempRubiksCubeCopy[7];
                rubiksCube[6] = _tempRubiksCubeCopy[0];
                rubiksCube[7] = _tempRubiksCubeCopy[3];
                rubiksCube[8] = _tempRubiksCubeCopy[6];
                // Connected Edges Left
                rubiksCube[9] = _tempRubiksCubeCopy[36];
                rubiksCube[10] = _tempRubiksCubeCopy[37];
                rubiksCube[11] = _tempRubiksCubeCopy[38];
                // Connected Edges Front
                rubiksCube[18] = _tempRubiksCubeCopy[9];
                rubiksCube[19] = _tempRubiksCubeCopy[10];
                rubiksCube[20] = _tempRubiksCubeCopy[11];
                // Connected Edges Right
                rubiksCube[27] = _tempRubiksCubeCopy[18];
                rubiksCube[28] = _tempRubiksCubeCopy[19];
                rubiksCube[29] = _tempRubiksCubeCopy[20];
                // Connected Edges Back
                rubiksCube[36] = _tempRubiksCubeCopy[27];
                rubiksCube[37] = _tempRubiksCubeCopy[28];
                rubiksCube[38] = _tempRubiksCubeCopy[29];
                break;

            case ScramblingMove.Left:
                // Left Face
                rubiksCube[9] = _tempRubiksCubeCopy[15];
                rubiksCube[10] = _tempRubiksCubeCopy[12];
                rubiksCube[11] = _tempRubiksCubeCopy[9];
                rubiksCube[12] = _tempRubiksCubeCopy[16];
                rubiksCube[14] = _tempRubiksCubeCopy[10];
                rubiksCube[15] = _tempRubiksCubeCopy[17];
                rubiksCube[16] = _tempRubiksCubeCopy[14];
                rubiksCube[17] = _tempRubiksCubeCopy[11];
                // Connected Edges Up
                rubiksCube[0] = _tempRubiksCubeCopy[44];
                rubiksCube[3] = _tempRubiksCubeCopy[41];
                rubiksCube[6] = _tempRubiksCubeCopy[38];
                // Connected Edges Front
                rubiksCube[18] = _tempRubiksCubeCopy[0];
                rubiksCube[21] = _tempRubiksCubeCopy[3];
                rubiksCube[24] = _tempRubiksCubeCopy[6];
                // Connected Edges Down
                rubiksCube[45] = _tempRubiksCubeCopy[18];
                rubiksCube[48] = _tempRubiksCubeCopy[21];
                rubiksCube[51] = _tempRubiksCubeCopy[24];
                // Connected Edges Back
                rubiksCube[44] = _tempRubiksCubeCopy[45];
                rubiksCube[41] = _tempRubiksCubeCopy[48];
                rubiksCube[38] = _tempRubiksCubeCopy[51];
                break;

            case ScramblingMove.LeftPrime:
                // Left Face
                rubiksCube[9] = _tempRubiksCubeCopy[11];
                rubiksCube[10] = _tempRubiksCubeCopy[14];
                rubiksCube[11] = _tempRubiksCubeCopy[17];
                rubiksCube[12] = _tempRubiksCubeCopy[10];
                rubiksCube[14] = _tempRubiksCubeCopy[16];
                rubiksCube[15] = _tempRubiksCubeCopy[9];
                rubiksCube[16] = _tempRubiksCubeCopy[12];
                rubiksCube[17] = _tempRubiksCubeCopy[15];
                // Connected Edges Up
                rubiksCube[0] = _tempRubiksCubeCopy[18];
                rubiksCube[3] = _tempRubiksCubeCopy[21];
                rubiksCube[6] = _tempRubiksCubeCopy[24];
                // Connected Edges Front
                rubiksCube[18] = _tempRubiksCubeCopy[45];
                rubiksCube[21] = _tempRubiksCubeCopy[48];
                rubiksCube[24] = _tempRubiksCubeCopy[51];
                // Connected Edges Down
                rubiksCube[45] = _tempRubiksCubeCopy[44];
                rubiksCube[48] = _tempRubiksCubeCopy[41];
                rubiksCube[51] = _tempRubiksCubeCopy[38];
                // Connected Edges Back
                rubiksCube[44] = _tempRubiksCubeCopy[0];
                rubiksCube[41] = _tempRubiksCubeCopy[3];
                rubiksCube[38] = _tempRubiksCubeCopy[6];
                break;

            case ScramblingMove.Front:
                // Front Face
                rubiksCube[18] = _tempRubiksCubeCopy[24];
                rubiksCube[19] = _tempRubiksCubeCopy[21];
                rubiksCube[20] = _tempRubiksCubeCopy[18];
                rubiksCube[21] = _tempRubiksCubeCopy[25];
                rubiksCube[23] = _tempRubiksCubeCopy[19];
                rubiksCube[24] = _tempRubiksCubeCopy[26];
                rubiksCube[25] = _tempRubiksCubeCopy[23];
                rubiksCube[26] = _tempRubiksCubeCopy[20];
                // Connected Edges Up
                rubiksCube[6] = _tempRubiksCubeCopy[17];
                rubiksCube[7] = _tempRubiksCubeCopy[14];
                rubiksCube[8] = _tempRubiksCubeCopy[11];
                // Connected Edges Right
                rubiksCube[27] = _tempRubiksCubeCopy[6];
                rubiksCube[30] = _tempRubiksCubeCopy[7];
                rubiksCube[33] = _tempRubiksCubeCopy[8];
                // Connected Edges Down
                rubiksCube[47] = _tempRubiksCubeCopy[27];
                rubiksCube[46] = _tempRubiksCubeCopy[30];
                rubiksCube[45] = _tempRubiksCubeCopy[33];
                // Connected Edges Left
                rubiksCube[17] = _tempRubiksCubeCopy[47];
                rubiksCube[14] = _tempRubiksCubeCopy[46];
                rubiksCube[11] = _tempRubiksCubeCopy[45];
                break;

            case ScramblingMove.FrontPrime:
                // Front Face
                rubiksCube[18] = _tempRubiksCubeCopy[20];
                rubiksCube[19] = _tempRubiksCubeCopy[23];
                rubiksCube[20] = _tempRubiksCubeCopy[26];
                rubiksCube[21] = _tempRubiksCubeCopy[19];
                rubiksCube[23] = _tempRubiksCubeCopy[25];
                rubiksCube[24] = _tempRubiksCubeCopy[18];
                rubiksCube[25] = _tempRubiksCubeCopy[21];
                rubiksCube[26] = _tempRubiksCubeCopy[24];
                // Connected Edges Up
                rubiksCube[6] = _tempRubiksCubeCopy[27];
                rubiksCube[7] = _tempRubiksCubeCopy[30];
                rubiksCube[8] = _tempRubiksCubeCopy[33];
                // Connected Edges Right
                rubiksCube[27] = _tempRubiksCubeCopy[47];
                rubiksCube[30] = _tempRubiksCubeCopy[46];
                rubiksCube[33] = _tempRubiksCubeCopy[45];
                // Connected Edges Down
                rubiksCube[47] = _tempRubiksCubeCopy[17];
                rubiksCube[46] = _tempRubiksCubeCopy[14];
                rubiksCube[45] = _tempRubiksCubeCopy[11];
                // Connected Edges Left
                rubiksCube[17] = _tempRubiksCubeCopy[6];
                rubiksCube[14] = _tempRubiksCubeCopy[7];
                rubiksCube[11] = _tempRubiksCubeCopy[8];
                break;

            case ScramblingMove.Right:
                // Right Face
                rubiksCube[27] = _tempRubiksCubeCopy[33];
                rubiksCube[28] = _tempRubiksCubeCopy[30];
                rubiksCube[29] = _tempRubiksCubeCopy[27];
                rubiksCube[30] = _tempRubiksCubeCopy[34];
                rubiksCube[32] = _tempRubiksCubeCopy[28];
                rubiksCube[33] = _tempRubiksCubeCopy[35];
                rubiksCube[34] = _tempRubiksCubeCopy[32];
                rubiksCube[35] = _tempRubiksCubeCopy[29];
                // Connected Edges Up
                rubiksCube[8] = _tempRubiksCubeCopy[26];
                rubiksCube[5] = _tempRubiksCubeCopy[23];
                rubiksCube[2] = _tempRubiksCubeCopy[20];
                // Connected Edges Front
                rubiksCube[26] = _tempRubiksCubeCopy[53];
                rubiksCube[23] = _tempRubiksCubeCopy[50];
                rubiksCube[20] = _tempRubiksCubeCopy[47];
                // Connected Edges Down
                rubiksCube[53] = _tempRubiksCubeCopy[36];
                rubiksCube[50] = _tempRubiksCubeCopy[39];
                rubiksCube[47] = _tempRubiksCubeCopy[42];
                // Connected Edges Back
                rubiksCube[36] = _tempRubiksCubeCopy[8];
                rubiksCube[39] = _tempRubiksCubeCopy[5];
                rubiksCube[42] = _tempRubiksCubeCopy[2];
                break;

            case ScramblingMove.RightPrime:
                // Right Face
                rubiksCube[27] = _tempRubiksCubeCopy[29];
                rubiksCube[28] = _tempRubiksCubeCopy[32];
                rubiksCube[29] = _tempRubiksCubeCopy[35];
                rubiksCube[30] = _tempRubiksCubeCopy[28];
                rubiksCube[32] = _tempRubiksCubeCopy[34];
                rubiksCube[33] = _tempRubiksCubeCopy[27];
                rubiksCube[34] = _tempRubiksCubeCopy[30];
                rubiksCube[35] = _tempRubiksCubeCopy[33];
                // Connected Edges Up
                rubiksCube[8] = _tempRubiksCubeCopy[36];
                rubiksCube[5] = _tempRubiksCubeCopy[39];
                rubiksCube[2] = _tempRubiksCubeCopy[42];
                // Connected Edges Front
                rubiksCube[26] = _tempRubiksCubeCopy[8];
                rubiksCube[23] = _tempRubiksCubeCopy[5];
                rubiksCube[20] = _tempRubiksCubeCopy[2];
                // Connected Edges Down
                rubiksCube[53] = _tempRubiksCubeCopy[26];
                rubiksCube[50] = _tempRubiksCubeCopy[23];
                rubiksCube[47] = _tempRubiksCubeCopy[20];
                // Connected Edges Back
                rubiksCube[36] = _tempRubiksCubeCopy[53];
                rubiksCube[39] = _tempRubiksCubeCopy[50];
                rubiksCube[42] = _tempRubiksCubeCopy[47];
                break;

            case ScramblingMove.Back:
                // Back Face
                rubiksCube[36] = _tempRubiksCubeCopy[42];
                rubiksCube[37] = _tempRubiksCubeCopy[39];
                rubiksCube[38] = _tempRubiksCubeCopy[36];
                rubiksCube[39] = _tempRubiksCubeCopy[43];
                rubiksCube[41] = _tempRubiksCubeCopy[37];
                rubiksCube[42] = _tempRubiksCubeCopy[44];
                rubiksCube[43] = _tempRubiksCubeCopy[41];
                rubiksCube[44] = _tempRubiksCubeCopy[38];
                // Connected Edges Right
                rubiksCube[35] = _tempRubiksCubeCopy[51];
                rubiksCube[32] = _tempRubiksCubeCopy[52];
                rubiksCube[29] = _tempRubiksCubeCopy[53];
                // Connected Edges Up
                rubiksCube[2] = _tempRubiksCubeCopy[35];
                rubiksCube[1] = _tempRubiksCubeCopy[32];
                rubiksCube[0] = _tempRubiksCubeCopy[29];
                // Connected Edges Left
                rubiksCube[9] = _tempRubiksCubeCopy[2];
                rubiksCube[12] = _tempRubiksCubeCopy[1];
                rubiksCube[15] = _tempRubiksCubeCopy[0];
                // Connected Edges Down
                rubiksCube[51] = _tempRubiksCubeCopy[9];
                rubiksCube[52] = _tempRubiksCubeCopy[12];
                rubiksCube[53] = _tempRubiksCubeCopy[15];
                break;

            case ScramblingMove.BackPrime:
                // Back Face
                rubiksCube[36] = _tempRubiksCubeCopy[38];
                rubiksCube[37] = _tempRubiksCubeCopy[41];
                rubiksCube[38] = _tempRubiksCubeCopy[44];
                rubiksCube[39] = _tempRubiksCubeCopy[37];
                rubiksCube[41] = _tempRubiksCubeCopy[43];
                rubiksCube[42] = _tempRubiksCubeCopy[36];
                rubiksCube[43] = _tempRubiksCubeCopy[39];
                rubiksCube[44] = _tempRubiksCubeCopy[42];
                // Connected Edges Right
                rubiksCube[35] = _tempRubiksCubeCopy[2];
                rubiksCube[32] = _tempRubiksCubeCopy[1];
                rubiksCube[29] = _tempRubiksCubeCopy[0];
                // Connected Edges Up
                rubiksCube[2] = _tempRubiksCubeCopy[9];
                rubiksCube[1] = _tempRubiksCubeCopy[12];
                rubiksCube[0] = _tempRubiksCubeCopy[15];
                // Connected Edges Left
                rubiksCube[9] = _tempRubiksCubeCopy[51];
                rubiksCube[12] = _tempRubiksCubeCopy[52];
                rubiksCube[15] = _tempRubiksCubeCopy[53];
                // Connected Edges Down
                rubiksCube[51] = _tempRubiksCubeCopy[35];
                rubiksCube[52] = _tempRubiksCubeCopy[32];
                rubiksCube[53] = _tempRubiksCubeCopy[29];
                break;

            case ScramblingMove.Down:
                // Down Face
                rubiksCube[45] = _tempRubiksCubeCopy[51];
                rubiksCube[46] = _tempRubiksCubeCopy[48];
                rubiksCube[47] = _tempRubiksCubeCopy[45];
                rubiksCube[48] = _tempRubiksCubeCopy[52];
                rubiksCube[50] = _tempRubiksCubeCopy[46];
                rubiksCube[51] = _tempRubiksCubeCopy[53];
                rubiksCube[52] = _tempRubiksCubeCopy[50];
                rubiksCube[53] = _tempRubiksCubeCopy[47];
                // Connected Edges Left
                rubiksCube[15] = _tempRubiksCubeCopy[42];
                rubiksCube[16] = _tempRubiksCubeCopy[43];
                rubiksCube[17] = _tempRubiksCubeCopy[44];
                // Connected Edges Front
                rubiksCube[24] = _tempRubiksCubeCopy[15];
                rubiksCube[25] = _tempRubiksCubeCopy[16];
                rubiksCube[26] = _tempRubiksCubeCopy[17];
                // Connected Edges Right
                rubiksCube[33] = _tempRubiksCubeCopy[24];
                rubiksCube[34] = _tempRubiksCubeCopy[25];
                rubiksCube[35] = _tempRubiksCubeCopy[26];
                // Connected Edges Back
                rubiksCube[42] = _tempRubiksCubeCopy[33];
                rubiksCube[43] = _tempRubiksCubeCopy[34];
                rubiksCube[44] = _tempRubiksCubeCopy[35];
                break;

            case ScramblingMove.DownPrime:
                // Down Face
                rubiksCube[45] = _tempRubiksCubeCopy[47];
                rubiksCube[46] = _tempRubiksCubeCopy[50];
                rubiksCube[47] = _tempRubiksCubeCopy[53];
                rubiksCube[48] = _tempRubiksCubeCopy[46];
                rubiksCube[50] = _tempRubiksCubeCopy[52];
                rubiksCube[51] = _tempRubiksCubeCopy[45];
                rubiksCube[52] = _tempRubiksCubeCopy[48];
                rubiksCube[53] = _tempRubiksCubeCopy[51];
                // Connected Edges Left
                rubiksCube[15] = _tempRubiksCubeCopy[24];
                rubiksCube[16] = _tempRubiksCubeCopy[25];
                rubiksCube[17] = _tempRubiksCubeCopy[26];
                // Connected Edges Front
                rubiksCube[24] = _tempRubiksCubeCopy[33];
                rubiksCube[25] = _tempRubiksCubeCopy[34];
                rubiksCube[26] = _tempRubiksCubeCopy[35];
                // Connected Edges Right
                rubiksCube[33] = _tempRubiksCubeCopy[42];
                rubiksCube[34] = _tempRubiksCubeCopy[43];
                rubiksCube[35] = _tempRubiksCubeCopy[44];
                // Connected Edges Back
                rubiksCube[42] = _tempRubiksCubeCopy[15];
                rubiksCube[43] = _tempRubiksCubeCopy[16];
                rubiksCube[44] = _tempRubiksCubeCopy[17];
                break;

        }

        summoningModule.ModuleLog(moduleId, "=-= Performed Scrambling Action {0}. New Rubik's Cube state is: =-=", scramblingMove.ToString());
        LogRubiksCube();
    }

    void ResetValuesToStart()
    {
        CollectedStarsNumbers.Clear();
        currentPlayerLocationIndex = 22;
        currentPlayerUpDirection = MovementDirection.Up;
    }

    /// <summary> Returns both the Rubik's Cube State, but also a list of Voids and Stars (index + Placement) </summary>
    void LogRubiksCube()
    {
        // Rubik's Cube State looks like

        // _________11 12 13
        // _________14 15 16
        // _________17 18 19
        // 21 22 23 31 32 33 41 42 43 51 52 53
        // 24 25 26 34 35 36 44 45 46 54 55 56
        // 27 28 29 37 38 39 47 48 49 57 58 59
        // _________61 62 63
        // _________64 65 66
        // _________67 68 69
        // Star (16) is in I-C2,  Star (25) is in II-B2,  Star (27) is in II-A3,  Star (34) is in III-A2,  Star (43) is in IV-C1,  Star (48) is in IV-B3,  Star (55) is in V-B2
        // Void (14) is in I-A2,  Void (22) is in II-B1,  Void (39) is in III-C3,  Void (49) is in IV-C3,  Void (53) is in V-C1,  Void (61) is in VI-A1

        // It's a LOT of info, but still less than Pixie Plate



        summoningModule.ModuleLog(moduleId, "_________" + rubiksCube[0] + " " + rubiksCube[1] + " " + rubiksCube[2]);
        summoningModule.ModuleLog(moduleId, "_________" + rubiksCube[3] + " " + rubiksCube[4] + " " + rubiksCube[5]);
        summoningModule.ModuleLog(moduleId, "_________" + rubiksCube[6] + " " + rubiksCube[7] + " " + rubiksCube[8]);
        summoningModule.ModuleLog(moduleId, rubiksCube[9]  + " " + rubiksCube[10] + " " + rubiksCube[11] + " " + rubiksCube[18] + " " + rubiksCube[19] + " " + rubiksCube[20] + " " + rubiksCube[27] + " " + rubiksCube[28] + " " + rubiksCube[29] + " " + rubiksCube[36] + " " + rubiksCube[37] + " " + rubiksCube[38]);
        summoningModule.ModuleLog(moduleId, rubiksCube[12] + " " + rubiksCube[13] + " " + rubiksCube[14] + " " + rubiksCube[21] + " " + rubiksCube[22] + " " + rubiksCube[23] + " " + rubiksCube[30] + " " + rubiksCube[31] + " " + rubiksCube[32] + " " + rubiksCube[39] + " " + rubiksCube[40] + " " + rubiksCube[41]);
        summoningModule.ModuleLog(moduleId, rubiksCube[15] + " " + rubiksCube[16] + " " + rubiksCube[17] + " " + rubiksCube[24] + " " + rubiksCube[25] + " " + rubiksCube[26] + " " + rubiksCube[33] + " " + rubiksCube[34] + " " + rubiksCube[35] + " " + rubiksCube[42] + " " + rubiksCube[43] + " " + rubiksCube[44]);
        summoningModule.ModuleLog(moduleId, "_________" + rubiksCube[45] + " " + rubiksCube[46] + " " + rubiksCube[47]);
        summoningModule.ModuleLog(moduleId, "_________" + rubiksCube[48] + " " + rubiksCube[49] + " " + rubiksCube[50]);
        summoningModule.ModuleLog(moduleId, "_________" + rubiksCube[51] + " " + rubiksCube[52] + " " + rubiksCube[53]);



        // Stars
        string _starsLocation = string.Empty;
        for (int i = 0; i < 7; i++)
        {
            _starsLocation += "Star (" + allSevenStarsNumbers[i] + ") is in " + GetAnistarCoordinateFormatting(Array.IndexOf(rubiksCube, allSevenStarsNumbers[i])) + (i < 6 ? ",  " : "");
        }
        summoningModule.ModuleLog(moduleId, _starsLocation);


        // Void
        string _voidLocation = string.Empty;
        for (int i = 0; i < 6; i ++)
        {
            _voidLocation += "Void (" + voidedCellsIndices[i] + ") is in " + GetAnistarCoordinateFormatting(Array.IndexOf(rubiksCube, voidedCellsIndices[i])) + (i < 5 ? ",  " : "");
        }
        summoningModule.ModuleLog(moduleId, _voidLocation);
    }

    string GetAnistarCoordinateFormatting(int indexInRubiksCubeArray)
    {
        string _return = string.Empty;

        // Face number I through VI
        _return += romanNumeralsToSix[indexInRubiksCubeArray / 9] + "-";

        // Coordinate inside of face
        _return += GetCoordinateFromCellIndex(indexInRubiksCubeArray % 9, 3);

        return _return;
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

        // Either MIND
        if (commandParts[0] == "mind")
        {
            yield return "sendtochat {0} Successfully pressed MIND and reset the module.";
            CasingTextButtonGetsPressed();
            yield break;
        }

        // Accept the words "submit", "move", "press", or their initials
        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "move" && commandParts[0] != "m" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Unrecognized command. Please use 'mind' to reset module, or 'move', 'submit' or 'press' to move around.";
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
        // An Auto-solver could be done, but it's a complex 3D-grid voided pathfinding
        // and I don't feel like spending days to find a solution, for now.
        summoningModule.ReceiveSolve();

        yield break;
    }
}
