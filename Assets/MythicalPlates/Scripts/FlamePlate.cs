using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class FlamePlate : PlateBase {

    readonly int[] magmaTable = new int[100]
    {  4, 8, 5, 1, 6, 3, 1, 7, 9, 2,
       9, 3, 8, 2, 1, 7, 5, 2, 6, 4,
       2, 9, 3, 4, 3, 8, 6, 5, 1, 7,
       7, 4, 2, 9, 5, 1, 3, 8, 4, 6,
       1, 5, 4, 7, 8, 2, 5, 6, 3, 9,
       8, 6, 9, 6, 2, 5, 4, 1, 7, 3,
       7, 4, 3, 1, 7, 6, 9, 2, 5, 8,
       5, 1, 6, 8, 9, 4, 7, 3, 2, 8,
       6, 2, 7, 5, 4, 3, 8, 9, 9, 1,
       3, 7, 1, 6, 7, 9, 2, 4, 8, 5};

    int currentLocation;
    int xMovement, yMovement;
    int numberOfVoidLinesPassed;

    // Universal Logging Data
    static int moduleIdCounter = 1;

    string finalPasscode;
    int nextPasscodeDigitToSubmit;

    /// <summary> Records which Lines have been voided, to avoid Voiding the same line multiple times.
    /// Rolumns are [0-9] and Rows are [10-19] for ease of mind. </summary>
    List<int> voidedLines;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake() 
    { 
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PlateKeypadGetsPressed(1); return false; };
        platePressableButtons[1].OnInteract += delegate () { PlateKeypadGetsPressed(2); return false; };
        platePressableButtons[2].OnInteract += delegate () { PlateKeypadGetsPressed(3); return false; };
        platePressableButtons[3].OnInteract += delegate () { PlateKeypadGetsPressed(4); return false; };
        platePressableButtons[4].OnInteract += delegate () { PlateKeypadGetsPressed(5); return false; };
        platePressableButtons[5].OnInteract += delegate () { PlateKeypadGetsPressed(6); return false; };
        platePressableButtons[6].OnInteract += delegate () { PlateKeypadGetsPressed(7); return false; };
        platePressableButtons[7].OnInteract += delegate () { PlateKeypadGetsPressed(8); return false; };
        platePressableButtons[8].OnInteract += delegate () { PlateKeypadGetsPressed(9); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        // Starting cell is 23, or D3
        currentLocation = 23;
        finalPasscode = string.Empty;
        voidedLines = new List<int>();
        nextPasscodeDigitToSubmit = 0;

        DetermineXAndY();
        DoAllMoveCycles();
    }

    // public override void UpdateModule() { base.UpdateModule(); }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    void PlateKeypadGetsPressed(int sentIndex)
    {
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved) { return; }

        if (CharToInt(finalPasscode[nextPasscodeDigitToSubmit]) == sentIndex)
        {
            // Correct button pressed
            summoningModule.ModuleLog(moduleId, "Pressed {0}, which is correct.", sentIndex);
            nextPasscodeDigitToSubmit++;

            if (nextPasscodeDigitToSubmit == 8)
            {
                summoningModule.ReceiveSolve();
            }
        }
        else // Incorrect button pressed
        {
            summoningModule.ModuleLog(moduleId, "Pressed {0}, which is incorrect, expected {1}.", sentIndex, finalPasscode[nextPasscodeDigitToSubmit]);
            summoningModule.ReceiveStrike();
        }
    }


    protected override void CasingTextButtonGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        nextPasscodeDigitToSubmit = 0;
        summoningModule.ModuleLog(moduleId, "FLAME Casing text pressed. The currently input digits have been reset and forgotten.");
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void DetermineXAndY()
    {
        // "x" and "y" values
        xMovement = CharToInt(bombInfo.GetSerialNumber()[2]);
        yMovement = CharToInt(bombInfo.GetSerialNumber()[5]);

        summoningModule.ModuleLog(moduleId, "Value for “x” is {0}, and value for “y” is {1}.", xMovement, yMovement);
    }


    /// <summary> Repeat Movements and Voiding </summary>
    void DoAllMoveCycles()
    {
        // The Game Design goal of this is to get a Vortex feel of perpetually rotating in a chaotic environment.
        // This is Magma Vortex, Heatran's signature move!
        // Heatran's Pokédex number is 485, which is why it's in the topleft and bottomright of the table!!

        // Yes it's not pretty, idc :D
        SingleMove(1);
        VoidNextLine();
        SingleMove(2);
        VoidNextLine();
        SingleMove(3);
        VoidNextLine();
        SingleMove(4);
        VoidNextLine();
        SingleMove(1);
        VoidNextLine();
        SingleMove(2);
        VoidNextLine();
        SingleMove(3);
        VoidNextLine();
        SingleMove(4);

        summoningModule.ModuleLog(moduleId, "Submit passcode {0} to solve the module.", finalPasscode);
    }

    /// <summary> Do a movement in the table, depending on the rule number [1-4] in the manual. </summary>
    void SingleMove(int ruleNumber)
    {
        // Do both horizontal and vertical movements
        MovementDirection directionForX = MovementDirection.Right;
        MovementDirection directionForY = MovementDirection.Down;

        //
        switch (ruleNumber)
        {
            case 1:
                directionForX = MovementDirection.Right;
                directionForY = MovementDirection.Down;
                break;

            case 2:
                directionForX = MovementDirection.Down;
                directionForY = MovementDirection.Right;
                break;

            case 3:
                directionForX = MovementDirection.Left;
                directionForY = MovementDirection.Up;
                break;

            case 4:
                directionForX = MovementDirection.Up;
                directionForY = MovementDirection.Left;
                break;

            default:
                summoningModule.ModuleLog(moduleId, "Unknown Rule Number {0} received in SingleMove()!! Report this to thunder725 on Discord please!", ruleNumber);
                break;
        }


        numberOfVoidLinesPassed = 0;


        // Do movements, but since this returns a Struct with the data, just gather the Number of Passed Cells
        for (int _x = 0; _x < xMovement; _x++)
        {
            numberOfVoidLinesPassed += MoveAroundGridWithVoid(directionForX, 100, ref currentLocation, 10, true).NumberOfPassedVoidCells;
        }

        for (int _y = 0; _y < yMovement; _y++)
        {
            numberOfVoidLinesPassed += MoveAroundGridWithVoid(directionForY, 100, ref currentLocation, 10, true).NumberOfPassedVoidCells;
        }


        // Sample the current location
        finalPasscode += magmaTable[currentLocation].ToString();

        // Log
        summoningModule.ModuleLog(moduleId, "Moved {0} {1} and {2} {3}. Landing in {4} which is {5}. Current Passcode is {6}", 
            xMovement, directionForX.ToString(),
            yMovement, directionForY.ToString(),
            GetCoordinateFromCellIndex(currentLocation, 10), magmaTable[currentLocation], finalPasscode);
    }


    /// <summary> Void a single line depending on the previous line index. CANNOT VOID CURRENT POSITION. </summary>
    void VoidNextLine()
    {
        // CANNOT VOID CURRENT POSITION
        int _forbiddenRow = GetRowFromCellIndex(currentLocation, 10);
        int _forbiddenColumn = GetColumnFromCellIndex(currentLocation, 10);

        // Either take the last voided Line, or first digit of passcode if no previous Voids.
        int _lineToVoid = voidedLines.Count > 0 ? (voidedLines.Last() % 10) : CharToInt(finalPasscode[0]);

        // Multiply by 4, Add 1, Take Digital Root
        _lineToVoid = DigitalRoot((_lineToVoid * 4) + 1);


        // If passed even number of Lines, void a Column. Else a Row
        bool _shouldVoidColumn = numberOfVoidLinesPassed % 2 == 0;

        bool _searchingForLine = true;
        while (_searchingForLine) // While loop because we can keep incrementing the index to void if it's already voided...
        {

            if (_shouldVoidColumn)
            {
                // Is the line already voided?
                // Or current position is this line?
                if (voidedLines.Contains(_lineToVoid) || _forbiddenColumn == _lineToVoid)
                {
                    // Go next, looping
                    summoningModule.ModuleLog(moduleId, "Cannot Void column {0}, going next.", _lineToVoid);
                    _lineToVoid = (_lineToVoid + 1) % 10;
                }
                else
                {
                    // Otherwise, we found the one!
                    _searchingForLine = false;
                }
            }
            else
            {
                // Is the line already voided?
                // Or current position is this line?
                if (voidedLines.Contains(_lineToVoid + 10) || _forbiddenRow == _lineToVoid)
                {
                    // Go next, looping
                    summoningModule.ModuleLog(moduleId, "Cannot Void row {0}, going next.", _lineToVoid);
                    _lineToVoid = (_lineToVoid + 1) % 10;
                }
                else
                {
                    // Otherwise, we found the one!
                    _searchingForLine = false;
                }
            }
        }


        // We know what to Void, Void it!
        for (int i = 0; i < 10; i++)
        {
            if (_shouldVoidColumn)
            {
                voidedCellsIndices.Add(10 * i + _lineToVoid);
            }
            else
            {
                voidedCellsIndices.Add(i + _lineToVoid * 10);
            }
        }

        // Record line for future use
        voidedLines.Add(_shouldVoidColumn ? _lineToVoid : _lineToVoid + 10);


        // Log
        summoningModule.ModuleLog(moduleId, "Passed {0} Voided lines, which is {1}. Voiding {2} with index {3} (after potential skips).",
            numberOfVoidLinesPassed, _shouldVoidColumn ? "even" : "odd", _shouldVoidColumn ? "column" : "row", _lineToVoid);
    }







    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (command == "flame" || command == "f")
        {
            yield return "sendtochat {0} Successfully pressed FLAME and reset the submission sequence.";
            CasingTextButtonGetsPressed();
            yield break;
        }


        if (!commandParts[0].Equals("submit") && !commandParts[0].Equals("s") && !commandParts[0].Equals("press") && !commandParts[0].Equals("p"))
        {
            yield return "sendtochaterror {0} you must format the submission with “!{1} Submit 13848426”, starting with the word “Submit” or “Press”.";
            yield break;
        }


        // If numbers were spread, concatenate them to facilitate everything
        if (commandParts.Length > 2)
        {
            // Concatenate them all
            string submits = "";
            for (int i = 1; i < commandParts.Length; i++)
            {
                submits += commandParts[i];
            }

            // Replace
            commandParts[1] = submits;
        }

        // Press all values
        foreach (char _numberToPress in commandParts[1])
        {
            // Is any of the buttons incorrect?
            if (CharToInt(_numberToPress) < 1 || CharToInt(_numberToPress) > 9)
            {
                yield return "sendtochaterror {0} One of the numbers '" + _numberToPress + "' wasn't between 1 and 9 inclusive! Stopping there";
                yield break;
            }

            // Wait a bit to avoid destroying the module visually
            yield return new WaitForSeconds(.15f);

            // Press the plate
            platePressableButtons[CharToInt(_numberToPress) - 1].OnInteract();
        }
    }
        

    public override IEnumerator TwitchHandleForcedSolve()
    {
        foreach (char _numberToPress in finalPasscode)
        {
            // Wait a bit to avoid destroying the module visually
            yield return new WaitForSeconds(.15f);

            // Press the plate
            platePressableButtons[CharToInt(_numberToPress) - 1].OnInteract();
        }

        yield break;
    }

}
