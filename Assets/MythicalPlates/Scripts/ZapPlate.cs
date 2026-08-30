using KModkit;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;


public class ZapPlate : PlateBase {

    /// <summary> Table DYN4M0. This is the starting position for Ruleseed 1.</summary>
    int[] tableDynamo = new int[150]
    {   8, 5, 0, 3, 2, 6, 7, 9, 1, 4,
        3, 8, 4, 2, 0, 7, 1, 5, 6, 9,
        4, 1, 8, 9, 5, 0, 6, 7, 2, 3,
        2, 0, 9, 6, 3, 1, 5, 4, 7, 8,
        1, 7, 0, 3, 2, 4, 9, 8, 5, 6,
        5, 8, 6, 0, 7, 9, 2, 4, 3, 1,
        7, 4, 1, 5, 9, 3, 6, 2, 8, 0,
        6, 3, 5, 7, 2, 8, 1, 0, 4, 9,
        8, 3, 2, 9, 1, 7, 0, 6, 4, 5,
        9, 2, 1, 0, 6, 4, 5, 3, 8, 7,
        3, 9, 6, 8, 4, 5, 7, 1, 0, 2,
        2, 6, 0, 7, 8, 1, 4, 5, 9, 3,
        4, 5, 7, 8, 1, 6, 3, 9, 2, 0,
        0, 7, 5, 4, 8, 2, 9, 6, 3, 1,
        7, 2, 3, 5, 0, 9, 4, 8, 1, 6 };

    int currentLocation;
    string[] sixDigitStageValues;
    int currentStage;
    int secondsToPressOn;

    // Universal Logging Data
    static int moduleIdCounter = 1;





    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake() 
    { 
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PlateGetsPressed(); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        sixDigitStageValues = new string[5] { "", "", "", "", "" };

        CalculateStartingCoordinate();
        ConstructTableFromRuleseed();
        FindAllSixDigitNumbers();
        FindSubmissionTimerNumber();
    }

    // public override void UpdateModule() { base.UpdateModule(); }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void PlateGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved) { return; }

        char _pressedSecond = bombInfo.GetFormattedTime().Last();
        if (CharToInt(_pressedSecond) == secondsToPressOn)
        {
            summoningModule.ModuleLog(moduleId, "Plate was pressed on a {0} second time, this is correct.", secondsToPressOn, _pressedSecond);
            StartCoroutine(PlateShouldSolve());
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Expected a press on a {0} second time, but you pressed the Plate on a {1}!", secondsToPressOn, _pressedSecond);
            summoningModule.ReceiveStrike();
        }
    }

    protected override void CasingTextButtonGetsPressed() { }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    /// <summary> Get starting coordinate from SN character 3 and 6 </summary>
    void CalculateStartingCoordinate()
    {
        string _serialNumber = bombInfo.GetSerialNumber();
        currentLocation = CharToInt(_serialNumber[2]) + (CharToInt(_serialNumber[5]) * 10);

        // Log
        summoningModule.ModuleLog(moduleId, "Starting Coordinate in the table is column {0}, row {1}, also known as {2}",
            GetColumnFromCellIndex(currentLocation, 10), GetRowFromCellIndex(currentLocation, 10), GetCoordinateFromCellIndex(currentLocation, 10));
    }

    void ConstructTableFromRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        { return; }

        FisherYatesShuffle(ref tableDynamo, Rng);

        string _log = string.Empty;
        for (int i = 0; i < 15; i ++)
        {
            for (int j = 0; j < 10; j ++)
            {
                _log += tableDynamo[10 * i + j];
            }
            _log += " ";
        }

        Debug.LogFormat("<Zap Plate #{0}> For verification purposes, the whole grid is {1}.", moduleId, _log);
    }


    /// <summary> Finds the Six-Digit number for all 5 stages </summary>
    void FindAllSixDigitNumbers()
    {
        // Get the codes for each stage
        for (currentStage = 0; currentStage < 5; currentStage++)
        {
            VoidLineFromIterationNumber(6 - currentStage);

            // Get the letters
            while (sixDigitStageValues[currentStage].Length < 6)
            {
                // If we move back up the grid
                if (MoveAroundGridWithVoid(MovementDirection.Down, 150, ref currentLocation, 10, true).ranIntoGridEdges)
                {
                    // Move one column right
                    if (MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true).ranIntoGridEdges)
                    {
                        summoningModule.ModuleLog(moduleId, "Moved back up the grid. Changing column to the leftmost one (it is looping).");
                    }
                    else
                    {
                        summoningModule.ModuleLog(moduleId, "Moved back up the grid. Moving column to the right.");
                    }
                }

                sixDigitStageValues[currentStage] += tableDynamo[currentLocation].ToString();
            }
            summoningModule.ModuleLog(moduleId, "Six-digit number for n = {0} is {1}", 6 - currentStage, sixDigitStageValues[currentStage]);
        }
    }

    void VoidLineFromIterationNumber(int iterationNumber)
    {
        voidedCellsIndices.Clear();

        // Simply loop over the whole table to Void
        for (int i = 0; i < 150; i ++)
        {
            if ((GetRowFromCellIndex(i, 10) + 1) % iterationNumber == 0)
            {
                voidedCellsIndices.Add(i);
            }
        }
    }

    void FindSubmissionTimerNumber()
    {
        int _sum = int.Parse(sixDigitStageValues[0]) + int.Parse(sixDigitStageValues[1]) + int.Parse(sixDigitStageValues[2]) + int.Parse(sixDigitStageValues[3]) + int.Parse(sixDigitStageValues[4]);
        secondsToPressOn = DigitalRoot(_sum);

        summoningModule.ModuleLog(moduleId, "Sum of all numbers is {0}, so press the Plate when the last digit on the bomb's timer is equal to {1}", _sum, secondsToPressOn);
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        Debug.LogFormat("<Zap Plate #{0}> Received Command ''{1}''", moduleId, command);
        if (summoningModule.isModuleSolved) { yield break; }

        

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length != 2)
        {
            Debug.LogFormat("<Zap Plate #{0}> Please format the submission with “!# Press 3” with exactly 2 arguments.", moduleId);
            yield return "sendtochaterror {0} Please format the submission with “!{1} Press 3” with exactly 2 arguments.";
            yield break;
        }

        if (!commandParts[0].Equals("press") && !commandParts[0].Equals("p") && !commandParts[0].Equals("submit") && !commandParts[0].Equals("s"))
        {
            Debug.LogFormat("<Zap Plate #{0}> Please format the submission with “!# Press 3”, starting with the word “Press” or “Submit”.", moduleId);
            yield return "sendtochaterror {0} Please format the submission with “!{1} Press 3”, starting with the word “Press” or “Submit”.";
            yield break;
        }

        int _timeToPressAt = int.Parse(commandParts[1]);
        if (_timeToPressAt < 0 || _timeToPressAt > 9)
        {
            Debug.LogFormat("<Zap Plate #{0}> Please ask for submission time that is within 0 and 9 only.", moduleId);
            yield return "sendtochaterror {0} Please ask for submission time that is within 0 and 9 only.";
            yield break;
        }


        // Without this yield return null, the yield return sendtochat exits the command instantly
        // since that sendtochat is the first yield return.
        yield return null;
        Debug.LogFormat("<Zap Plate #{0}> Will press the Plate on {0} seconds.", moduleId, _timeToPressAt);
        yield return "sendtochat {0} will press the Plate on " + _timeToPressAt + " seconds. The command is cancellable.";

        while ((int)bombInfo.GetTime() % 10 != secondsToPressOn)
        {
            // Allow chat to cancel the command.
            Debug.LogFormat("<Zap Plate #{0}> Command was cancelled before the Plate was pressed.", moduleId);
            yield return "trycancel Command was cancelled before the Plate was pressed.";

            yield return new WaitForSeconds(0.2f);
        }

        platePressableButtons[0].OnInteract();
    }

    // Code from Tenpins. Credit goes to TasThiluna
    public override IEnumerator TwitchHandleForcedSolve()
    {
        summoningModule.ModuleLog(moduleId, "Will solve automatically on {0} seconds.", secondsToPressOn);

        
        yield return null;
        while ((int)bombInfo.GetTime() % 10 != secondsToPressOn)
        {
            yield return true;
            yield return null;
        }

        platePressableButtons[0].OnInteract();        
    }

}
