using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FistPlate : PlateBase {

    [SerializeField] TextMesh voidedCoordinatesText;
    /// <summary> Location of all possible Redirection Station </summary>
    readonly int[] redirectionStationIndices = new int[29] { 0, 3, 5, 7, 9, 11, 14, 16, 19, 21, 23, 25, 28, 30, 32, 33, 35, 37, 39, 42, 44, 49, 51, 54, 56, 57, 60, 61, 62};

    int[] ruleseedRedirectionModifier = new int[4];

    /// <summary> Dictionary containing each ACTIVE Redirection Station's location, with its associated Redirect Direction. </summary>
    Dictionary<int, int> redirectionDirections;

    /// <summary> Redirection String created using edgework, to distribute Redirect Directions to Redirection Stations </summary>
    string RedirectionString;

    // Data about the Unstoppable Force
    int unstoppableForceIndex;
    MovementDirection unstoppableForceDirection;

    // Final result!
    bool isMesagozaSafe;

    // Universal Logging Data
    static int moduleIdCounter = 1;


    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (hasPlateSolved)
        { return; }

        if (isMesagozaSafe)
        {
            summoningModule.ModuleLog(moduleId, "Pressed the SAFE button, which is correct!");
            StartCoroutine(PlateShouldSolve());
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Pressed the SAFE button, which is incorrect!");
            summoningModule.ReceiveStrike();
        }
    }

    void PressedEvacuateButton()
    {
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (hasPlateSolved)
        { return; }

        if (isMesagozaSafe == false)
        {
            summoningModule.ModuleLog(moduleId, "Pressed the EVACUATE button, which is correct!");
            StartCoroutine(PlateShouldSolve());
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
        InitializeRuleseed();

        summoningModule.ModuleLog(moduleId, "0 is associated to {0}, 1 to {1}, 2 to {2} and 3 to {3}", (MovementDirection)ruleseedRedirectionModifier[0],
            (MovementDirection)ruleseedRedirectionModifier[1], (MovementDirection)ruleseedRedirectionModifier[2], (MovementDirection)ruleseedRedirectionModifier[3]);

        GenerateVoidTiles();
        GenerateRedirectionString();
        DistributeRedirectionToStations();
        SimulateUnstoppableForcePath();
    }

    void InitializeRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        ruleseedRedirectionModifier = new int[4] { 0, 1, 2, 3 };

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        { return; }

        FisherYatesShuffle(ref ruleseedRedirectionModifier, Rng);
    }

    void GenerateVoidTiles()
    {
        summoningModule.ModuleLog(moduleId, "Generating 6 Voided Tiles.");

        int _attemptedVoidIndex;
        while (voidedCellsIndices.Count < 6)
        {
            _attemptedVoidIndex = UnityEngine.Random.Range(0, 64);

            // Do not allow Void to exist on the Immovable Object's position (27), the Unstoppable Force's starting position (59)
            // Nor D5 / D7  (35 and 51) because they are in the straight path to the Immovable Object and that's boring
            // Nor to have duplicates
            if (_attemptedVoidIndex == 27 || _attemptedVoidIndex == 59 || _attemptedVoidIndex == 35 || _attemptedVoidIndex == 51 || voidedCellsIndices.Contains(_attemptedVoidIndex))
            { continue; }

            voidedCellsIndices.Add(_attemptedVoidIndex);
        }

        // Cache the Coordinates of the tiles in string form
        string[] voidedCellsCoordinates = new string[6] { GetCoordinateFromCellIndex(voidedCellsIndices[0], 8), GetCoordinateFromCellIndex(voidedCellsIndices[1], 8),
        GetCoordinateFromCellIndex(voidedCellsIndices[2], 8), GetCoordinateFromCellIndex(voidedCellsIndices[3], 8),
        GetCoordinateFromCellIndex(voidedCellsIndices[4], 8), GetCoordinateFromCellIndex(voidedCellsIndices[5], 8)};


        summoningModule.ModuleLog(moduleId, "Voided Tiles will get generated in tiles {0}", voidedCellsCoordinates.Join());

        // Cells will be shown 2 per line, so there is some transformation to be done!!
        voidedCoordinatesText.text = voidedCellsCoordinates[0] + " " + voidedCellsCoordinates[1] + "\n" + voidedCellsCoordinates[2] + " "
            + voidedCellsCoordinates[3] + "\n" + voidedCellsCoordinates[4] + " " + voidedCellsCoordinates[5];
    }

    void GenerateRedirectionString()
    {
        // Serial Number Digits
        int[] _serialNumberDigits = bombInfo.GetSerialNumberNumbers().ToArray();

        // Concatenate them, mod 4
        RedirectionString = _serialNumberDigits.Select(x => x%4).Join("");

        summoningModule.ModuleLog(moduleId, "Serial Number Digits {0}, modulo 4, get appended to the Redirection String: {1}",_serialNumberDigits.Join(), RedirectionString);


        // Number of Batteries % 4
        int _numberOfBatteries = bombInfo.GetBatteryCount();
        RedirectionString += (_numberOfBatteries % 4).ToString();
        summoningModule.ModuleLog(moduleId, "The {0} batteries, modulo 4, get appended to the Redirection String: {1}", _numberOfBatteries, RedirectionString);


        // Number of Indicators % 4
        int _numberOfIndicators = bombInfo.GetIndicators().Count();
        RedirectionString += (_numberOfIndicators % 4).ToString();
        summoningModule.ModuleLog(moduleId, "The {0} indicators, modulo 4, get appended to the Redirection String: {1}", _numberOfIndicators, RedirectionString);


        // Number of Ports % 4
        int _numberOfPorts = bombInfo.GetPortCount();
        RedirectionString += (_numberOfPorts % 4).ToString();
        summoningModule.ModuleLog(moduleId, "The {0} ports, modulo 4, get appended to the Redirection String: {1}",
            _numberOfPorts, RedirectionString);


        summoningModule.ModuleLog(moduleId, "Final Redirection string is {0}, repeated indefinitely", RedirectionString);

        // For ease of use, the Redirection string will be repeated 6 times so that even with its shortest length of 5 (2 SN# + battery + indic + port)
        // its length becomes 30 which is enough to cover the 29 Redirection Stations
        RedirectionString = RedirectionString + RedirectionString + RedirectionString + RedirectionString + RedirectionString + RedirectionString;
    }

    void DistributeRedirectionToStations()
    {
        redirectionDirections = new Dictionary<int, int>();

        int _numberOfStationsWithDirections = 0;

        for (int i = 0; i < 29; i ++)
        {
            // Register to that dictionary every station with its associated integer direction
            // Ignore Voided Stations
            if (voidedCellsIndices.Contains(redirectionStationIndices[i]))
            { continue; }


            // Cannot directly gather the i-th value from the redirection string, because i might not be
            // "The i-th station to receive a direction" due to Void!
            // Instead we use the number of stations added up until now
            redirectionDirections.Add(redirectionStationIndices[i], ruleseedRedirectionModifier[CharToInt(RedirectionString[_numberOfStationsWithDirections])]);

            _numberOfStationsWithDirections++;
        }

        summoningModule.ModuleLog(moduleId, "The non-voided Redirect Stations now have those directions: {0}",
            redirectionDirections.Select(x => GetCoordinateFromCellIndex(x.Key, 8) + '_' + ((MovementDirection)(x.Value)).ToString()).Join(" ") );
    }

    void SimulateUnstoppableForcePath()
    {
        // There is a non-zero chance that the Unstoppable Force gets stuck in an infinite loop
        // To detect them, I could use a limit of "100 redirections" and be safe from that.
        // However I want to be 100% sure of the loop, even if I misjudged it

        // Since stations always redirect in the same direction, and they can fail (can't u-turn and doesn't do anything if redirect in same direction)
        // Then getting redirected by a station that already redirected successfully means a loop is happening.
        // The outcome of the redirection will always be the same because there is no change to the state of the board over time.

        // So we just keep track of which stations Actually redirected the Unstoppable Force, and if we successfully get redirected by
        // a Station that already redirected, then that means an infinite loop has been found

        // Starting location & direction of the Unstoppable Force
        unstoppableForceIndex = 59;
        unstoppableForceDirection = MovementDirection.Up;

        // Safety out counter since while loops are dangerous
        // This is the number of **movements** that is allowed, not redirections
        int _safetyCounter = 1000;


        isMesagozaSafe = true;
        List<int> _redirectionStationInfiniteLoopTracker = new List<int>();
        

        // Prepare memory locations for use in the loop
        VoidMovementData _forceMovementData;
        int _stationDirectionIndex;
        MovementDirection _stationDirection;


        // While true because I want an infinite loop that gets forcefully exited by hitting the Immovable Object,
        // exiting the board, or being in an infinite loop
        while (true)
        {
            _safetyCounter--;
            if (_safetyCounter == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Something went TERRIBLY WRONG with the Unstoppable Force's movement and we reached the limit of 1000 movements. Please report this to the developper. Auto-solving to avoid unfair strikes or softlocks.");
                StartCoroutine(PlateShouldSolve());
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
            if (redirectionStationIndices.Contains(unstoppableForceIndex))
            {
                // Gather the data
                redirectionDirections.TryGetValue(unstoppableForceIndex, out _stationDirectionIndex);
                _stationDirection = (MovementDirection)_stationDirectionIndex;


                // Check if the Redirection Station should be ignored!
                // (Redirect Direction aligns or is opposite with current Force direction)
                if (_stationDirection == unstoppableForceDirection)
                {
                    // Ignore redirection station, do nothing
                    summoningModule.ModuleLog(moduleId, "The Unstoppable Force moved {0} into tile {1}, but didn't get redirected as its direction is aligned with the Redirection Station!",
                        unstoppableForceDirection.ToString(), GetCoordinateFromCellIndex(unstoppableForceIndex, 8));
                    continue;
                }
                else if (GetOppositeMovementDirection(_stationDirection) == unstoppableForceDirection)
                {
                    // Ignore redirection station, do nothing
                    summoningModule.ModuleLog(moduleId, "The Unstoppable Force moved {0} into tile {1}, but didn't get redirected as its direction is exactly opposite to the Redirection Station!",
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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { yield break; }

        Debug.LogFormat("<Fist Plate #{0}> Received Command ''{1}''", moduleId, command);
        if (hasPlateSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);


        if (commandParts.Length == 0)
        {
            Debug.LogFormat("<Fist Plate #{0}> Received empty command!", moduleId);
            yield return "sendtochaterror {0} Received empty command!";
            yield break;
        }

        if (commandParts.Length != 2)
        {
            Debug.LogFormat("<Fist Plate #{0}> Received command formatted incorrectly!", moduleId);
            yield return "sendtochaterror {0} Received command formatted incorrectly!";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            Debug.LogFormat("<Fist Plate #{0}> Received unknown command! Please use 'submit' or 'press' to submit an answer.", moduleId);
            yield return "sendtochaterror {0} Received unknown command! Please use 'submit' or 'press' to submit an answer.";
            yield break;
        }

        if (commandParts[1] == "safe")
        {
            yield return null;
            platePressableButtons[00].OnInteract();
            yield break;
        }
        else if (commandParts[1] == "evacuate")
        {
            yield return null;
            platePressableButtons[01].OnInteract();
            yield break;
        }

        Debug.LogFormat("<Fist Plate #{0}> Received unknown button: '{1}'. Please use 'safe' or 'evacuate' for buttons.", moduleId, commandParts[1]);
        yield return "sendtochaterror {0} Received unknown button: " + commandParts[1] + ". Please use 'safe' or 'evacuate' for buttons.";

    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        if (isMesagozaSafe)
        {
            platePressableButtons[00].OnInteract();
        }
        else
        {
            platePressableButtons[01].OnInteract();
        }
    }
}
