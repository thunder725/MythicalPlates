using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

public class FistPlate : PlateBase {

    [SerializeField] TextMesh voidedCoordinatesText;
    readonly int[] redirectStationIndices = new int[29] { 0, 3, 5, 7, 9, 11, 14, 16, 19, 21, 23, 25, 28, 30, 32, 33, 35, 37, 39, 42, 44, 49, 51, 54, 56, 57, 60, 61, 62};

    Dictionary<int, int> redirectDirections;
    string RedirectionString;

    int unstoppableForceIndex;
    MovementDirection unstoppableForceDirection;

    bool isMesagozaSafe;


    // Universal Logging Data
    static int moduleIdCounter = 1;

    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        // ButtonIndex is 0-11 but MonthNumbers are 1-12!!
        platePressableButtons[00].OnInteract += delegate () { PressedSafeButton(); return false; };
        platePressableButtons[01].OnInteract += delegate () { PressedEvacuateButton(); return false; };

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

    void PressedSafeButton()
    {
        platePressableButtons[0].AddInteractionPunch(0.5f);
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        if (isMesagozaSafe)
        {
            summoningModule.ModuleLog(moduleId, "Pressed the SAFE button, which was correct!");
            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Pressed the SAFE button, which is incorrect!");
            summoningModule.ReceiveStrike();
        }
    }

    void PressedEvacuateButton()
    {
        platePressableButtons[0].AddInteractionPunch(0.5f);
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        if (isMesagozaSafe == false)
        {
            summoningModule.ModuleLog(moduleId, "Pressed the EVACUATE button, which was correct!");
            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Pressed the EVACUATE button, which is incorrect!");
            summoningModule.ReceiveStrike();
        }
    }


    protected override void CasingTextButtonGetsPressed() { }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        GenerateVoidTiles();
        GenerateRedirectionString();
        DistributeRedirectionToStations();
        SimulateUnstoppableForcePath();
    }

    void GenerateVoidTiles()
    {
        summoningModule.ModuleLog(moduleId, "Generating Void Tiles.");

        int _attemptedVoidIndex;
        while (voidedCellsIndices.Count < 6)
        {
            _attemptedVoidIndex = UnityEngine.Random.Range(0, 64);

            // Do not allow Void to exist on the Immovable Object's position, the Unstoppable Force's starting position
            // Nor to have duplicate
            if (_attemptedVoidIndex == 27 || _attemptedVoidIndex == 59 || voidedCellsIndices.Contains(_attemptedVoidIndex))
            { continue; }

            voidedCellsIndices.Add(_attemptedVoidIndex);
        }

        summoningModule.ModuleLog(moduleId, "Void Tiles will get generated in tiles {0}", voidedCellsIndices.Select(x => GetCoordinateFromCellIndex(x, 8)).Join());
        voidedCoordinatesText.text = voidedCellsIndices.Select(x => GetCoordinateFromCellIndex(x, 8)).Join().Remove(5, 1).Insert(5, "\n").Remove(11, 1).Insert(11, "\n");
    }

    void GenerateRedirectionString()
    {
        // Serial Number Digits
        int[] _serialNumberDigits = bombInfo.GetSerialNumberNumbers().ToArray();

        RedirectionString = _serialNumberDigits.Select(x => x%4).Join("");

        summoningModule.ModuleLog(moduleId, "Redirection String gets the Serial Number Digits {0}; which when Modulo 4 and concatenated becomes {1}",
            _serialNumberDigits.Join(), RedirectionString);


        // Number of Batteries
        int _numberOfBatteries = bombInfo.GetBatteryCount();
        RedirectionString += (_numberOfBatteries % 4).ToString();
        summoningModule.ModuleLog(moduleId, "There are {0} batteries; which when Modulo 4 and added to the Redirection String gives {1}",
            _numberOfBatteries, RedirectionString);


        // Number of Indicators
        int _numberOfIndicators = bombInfo.GetIndicators().Count();
        RedirectionString += (_numberOfIndicators % 4).ToString();
        summoningModule.ModuleLog(moduleId, "There are {0} indicators; which when Modulo 4 and added to the Redirection String gives {1}",
            _numberOfIndicators, RedirectionString);


        // Number of Ports
        int _numberOfPorts = bombInfo.GetPortCount();
        RedirectionString += (_numberOfPorts % 4).ToString();
        summoningModule.ModuleLog(moduleId, "There are {0} ports; which when Modulo 4 and added to the Redirection String gives {1}",
            _numberOfPorts, RedirectionString);


        summoningModule.ModuleLog(moduleId, "Final Redirection string is {0} repeated indefinitely", RedirectionString);

        // For ease of use, the Redirection string will be repeated 6 times so that even with its shortest length of 5 (2 SN# + battery + indic + port)
        // its length becomes 30 which is enough to cover the 29 Redirection Stations
        RedirectionString = RedirectionString + RedirectionString + RedirectionString + RedirectionString + RedirectionString + RedirectionString;
    }

    void DistributeRedirectionToStations()
    {
        redirectDirections = new Dictionary<int, int>();

        for (int i = 0; i < 29; i ++)
        {
            // Register to that dictionary every station with its associated integer direction
            // Ignore Voided Stations
            if (voidedCellsIndices.Contains(redirectStationIndices[i]))
            { continue; }

            // Cannot directly gather the i-th value from the redirection string, because i might not be
            // "The i-th station to receive a direction" due to Void!
            // Instead we use the number of stations added up until now
            redirectDirections.Add(redirectStationIndices[i], CharToInt(RedirectionString[redirectDirections.Count]));
        }
    }

    void SimulateUnstoppableForcePath()
    {
        // There is a non-zero chance that the Unstoppable Force gets stuck in an infinite loop
        // To detect them, I could use a limit of "100 redirections" and be safe from that.
        // However I want to be 100% sure of the loop
        // Since stations always redirect in the same direction, and they can fail (can't u-turn and doesn't do anything if redirect in same direction)
        // Then getting redirected by a station that already redirected successfully means a loop is happening.
        // The outcome of the redirection will always be the same because there is no change to the state of the board over time.
        // So we just keep track of which stations Actually redirected the Unstoppable Force, and if we successfully get redirected by
        // a Station that already redirected, then that means an infinite loop has been found

        unstoppableForceIndex = 59;
        unstoppableForceDirection = MovementDirection.Up;

        int _safetyCounter = 1000;
        isMesagozaSafe = true;
        int _stationDirectionIndex = 0;
        MovementDirection _stationDirection;

        List<int> _redirectionStationInfiniteLoopTracker = new List<int>();

        // Prepare memory locations
        VoidMovementData _forceMovementData;



        while (true)
        {
            _safetyCounter--;
            if (_safetyCounter == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Something went TERRIBLY WRONG with the Unstoppable Force's movement and we reached the limit of 1000 movements. Please report this to the developper. Auto-solving to avoid unfair strikes or softlocks.");
                summoningModule.ReceiveSolve();
                return;
            }

            // Move, using Void
            _forceMovementData = MoveAroundGridWithVoid(unstoppableForceDirection, 64, ref unstoppableForceIndex, 8, false);


            // Hit the Immovable Object.
            if (unstoppableForceIndex == 27)
            {
                isMesagozaSafe = false;
                summoningModule.ModuleLog(moduleId, "The Unstoppable Force has reached the Immovable Object! Mesagoza is not safe and the EVACUATE button must be pressed.");
                return;
            }


            // Left the edges
            if (_forceMovementData.ranIntoGridEdges)
            {
                isMesagozaSafe = true;
                summoningModule.ModuleLog(moduleId, "The Unstoppable Force left the board while moving {0}! Mesagoza is safe and the SAFE button must be pressed.",
                    unstoppableForceDirection.ToString());
                return;
            }


            // Landed in a Redirection Station
            if (redirectStationIndices.Contains(unstoppableForceIndex))
            {
                // Gather the data
                redirectDirections.TryGetValue(unstoppableForceIndex, out _stationDirectionIndex);
                _stationDirection = (MovementDirection)_stationDirectionIndex;


                // Check if the Redirection Station should be ignored!
                // (Redirect Direction aligns or is opposite with current Force direction)
                if (_stationDirection == unstoppableForceDirection || GetOppositeMovementDirection(_stationDirection) == unstoppableForceDirection)
                {
                    // Ignore redirection station, do nothing
                    summoningModule.ModuleLog(moduleId, "The Unstoppable Force moved {0} into tile {1}. The Redirection Station didn't affect it as its direction is aligned!",
                        unstoppableForceDirection.ToString(), GetCoordinateFromCellIndex(unstoppableForceIndex, 8));
                    continue;
                }


                // Else, we actually get redirected
                // Make sure we're not in an infinite loop
                if (_redirectionStationInfiniteLoopTracker.Contains(unstoppableForceIndex))
                {
                    isMesagozaSafe = true;
                    summoningModule.ModuleLog(moduleId, "The Unstoppable Force got redirected by the Redirection Station in {0} again! This is an Infinite Loop! Mesagoza is safe and the SAFE button must be pressed.",
                        GetCoordinateFromCellIndex(unstoppableForceIndex, 8));
                    return;
                }



                // Get Redirected
                summoningModule.ModuleLog(moduleId, "The Unstoppable Force moved {0} into tile {1} and got redirected to {2}.",
                    unstoppableForceDirection.ToString(), GetCoordinateFromCellIndex(unstoppableForceIndex, 8), _stationDirection.ToString());

                unstoppableForceDirection = _stationDirection;


                // Track that station so we can detect future infinite loops
                _redirectionStationInfiniteLoopTracker.Add(unstoppableForceIndex);
                continue;
            }


            // Just moved ^^
            summoningModule.ModuleLog(moduleId, "The Unstoppable Force moved {0} into tile {1}.",
                unstoppableForceDirection.ToString(), GetCoordinateFromCellIndex(unstoppableForceIndex, 8));

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

        if (commandParts[1] == "safe")
        {
            PressedSafeButton();
            yield break;
        }
        else if (commandParts[1] == "evacuate")
        {
            PressedEvacuateButton();
            yield break;
        }

        yield return "sendtochaterror {0} Received unknown button: " + commandParts[1] + ". Please use 'safe' or 'evacuate' for buttons.";
        yield break;

    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        if (isMesagozaSafe)
        {
            PressedSafeButton();
        }
        else
        {
            PressedEvacuateButton();
        }

        yield break;
    }
}
