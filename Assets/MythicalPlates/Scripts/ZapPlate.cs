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

    int ruleseedColumnConditionIndex;
    int ruleseedRowConditionIndex;
    int ruleseedLoopBehaviour;

    string[] coordinateConditionsStrings = new string[8]
    {
        "its third character",
        "its sixth character",
        "its first digit",
        "its last digit",
        "the last digit of the sum of its digits",
        "the digital root of the sum of its digits",
        "the last digit of its first letter's position (A1-Z26)",
        "the last digit of its last letter's position (A1-Z26)"
    };

    string[] loopBehaviourStrings = new string[7]
    {
        "moving one column to the right",
        "moving one column to the left",
        "not changing columns",
        "moving two columns to the right",
        "moving two columns to the left",
        "moving three columns to the right",
        "moving three columns to the left"
    };

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

        ManageRuleseed();
        CalculateStartingCoordinates();
        FindAllSixDigitNumbers();
        FindSubmissionTimerNumber();
    }

    // public override void UpdateModule() { base.UpdateModule(); }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void PlateGetsPressed()
    {
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (hasPlateSolved) { return; }

        int _pressedSecond = Mathf.FloorToInt(bombInfo.GetTime()) % 10;

        if (_pressedSecond == secondsToPressOn)
        {
            summoningModule.ModuleLog(moduleId, "Plate was pressed on a {0} ({1}) second time, this is correct.", secondsToPressOn, _pressedSecond);
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
    void CalculateStartingCoordinates()
    {
        string _serialNumber = bombInfo.GetSerialNumber();
        int columnIndex = GetCoordinate(ruleseedColumnConditionIndex);
        int rowIndex = GetCoordinate(ruleseedRowConditionIndex);

        currentLocation = columnIndex + (rowIndex * 10);

        // Log
        summoningModule.ModuleLog(moduleId, "Starting Coordinate in the table is column {0}, row {1}, also known as {2}",
            columnIndex, rowIndex, GetCoordinateFromCellIndex(currentLocation, 10));
    }

    int GetCoordinate(int coordinateRuleIndex)
    {
        switch (coordinateRuleIndex)
        {
            default: return 0;
            case 0: return CharToInt(bombInfo.GetSerialNumber()[2]);
            case 1: return CharToInt(bombInfo.GetSerialNumber()[5]);
            case 2: return bombInfo.GetSerialNumberNumbers().First();
            case 3: return bombInfo.GetSerialNumberNumbers().Last();
            case 4: return (bombInfo.GetSerialNumberNumbers().Sum()) % 10;
            case 5: return DigitalRoot(bombInfo.GetSerialNumberNumbers().Sum());
            case 6: return (Array.IndexOf(alphabet, bombInfo.GetSerialNumberLetters().First().ToString()) + 1) % 10;
            case 7: return (Array.IndexOf(alphabet, bombInfo.GetSerialNumberLetters().Last().ToString()) + 1) % 10;
        }
    }

    void ManageRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        {
            ruleseedColumnConditionIndex = 0;
            ruleseedRowConditionIndex = 1;
            ruleseedLoopBehaviour = 0;
            return;
        }

        Rng.ShuffleFisherYates(tableDynamo);
        Debug.LogFormat("<Zap Plate #{0}> For verification purposes, the whole grid is {1}.", moduleId, tableDynamo.Join(""));


        int[] possibleStartingConditions = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 };
        Rng.ShuffleFisherYates(possibleStartingConditions);
        ruleseedColumnConditionIndex = possibleStartingConditions[0];
        ruleseedRowConditionIndex = possibleStartingConditions[1];
        Debug.LogFormat("<Zap Plate #{0}> Column condition is {1} and Row condition is {2}.",
            moduleId, coordinateConditionsStrings[ruleseedColumnConditionIndex], coordinateConditionsStrings[ruleseedRowConditionIndex]);


        ruleseedLoopBehaviour = Rng.Next(0, 7);
        Debug.LogFormat("<Zap Plate #{0}> Looping behaviour is {1}.",
            moduleId, loopBehaviourStrings[ruleseedLoopBehaviour]);
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
                    switch(ruleseedLoopBehaviour)
                    {
                        case 0: // Move one right
                            MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true);
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Changing one column to the right.");
                            break;

                        case 1: // Move one left
                            MoveAroundGridWithVoid(MovementDirection.Left, 150, ref currentLocation, 10, true);
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Changing one column to the left.");
                            break;

                        case 2: // Stay in the column
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Not changing column.");
                            break;

                        case 3: // Move two right
                            MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true);
                            MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true);
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Changing two columns to the right.");
                            break;

                        case 4: // Move two left
                            MoveAroundGridWithVoid(MovementDirection.Left, 150, ref currentLocation, 10, true);
                            MoveAroundGridWithVoid(MovementDirection.Left, 150, ref currentLocation, 10, true);
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Changing two columns to the left.");
                            break;

                        case 5: // Move three right
                            MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true);
                            MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true);
                            MoveAroundGridWithVoid(MovementDirection.Right, 150, ref currentLocation, 10, true);
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Changing three columns to the right.");
                            break;

                        case 6: // Move three left
                            MoveAroundGridWithVoid(MovementDirection.Left, 150, ref currentLocation, 10, true);
                            MoveAroundGridWithVoid(MovementDirection.Left, 150, ref currentLocation, 10, true);
                            MoveAroundGridWithVoid(MovementDirection.Left, 150, ref currentLocation, 10, true);
                            summoningModule.ModuleLog(moduleId, "Moving back up the grid. Changing three columns to the left.");
                            break;
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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { yield break; }

        Debug.LogFormat("<Zap Plate #{0}> Received Command ''{1}''", moduleId, command);
        if (hasPlateSolved) { yield break; }

        

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
        Debug.LogFormat("<Zap Plate #{0}> Will press the Plate on {1} seconds.", moduleId, _timeToPressAt);
        yield return "sendtochat {0} will press the Plate on " + _timeToPressAt + " seconds. The command is cancellable.";

        while ((int)bombInfo.GetTime() % 10 != secondsToPressOn)
        {
            // Allow chat to cancel the command.
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
