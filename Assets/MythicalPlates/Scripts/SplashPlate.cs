using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class SplashPlate : PlateBase {

    /// <summary> Not necessary for the runtime puzzle, but used for demo purposes in DeterminePermutationWithMostIntersections().
    /// More info in that method. </summary>
    [SerializeField] TextAsset PermutationsOfEight;
    [SerializeField] TextMesh PlateTextMesh;

    /// <summary> In the Lustrous Wheel, where to start. </summary>
    char startingLetter;

    /// <summary> What movements should be done to move around the Plate </summary>
    int[] LustrousMovements;
    string finalPointsSequence = "";

    /// <summary> Final expected answer: the number of Intersections obtained by the 7 segments </summary>
    int numberOfIntersections;


    string requiredPlayerAnswer = "";
    string currentPlayerAnswer;
    Coroutine playerSubmissionTimerCoroutine;

    /// <summary> Location of the 8 points in order ABCDEFGH </summary>
    readonly Vector2[] points = new Vector2[8] { new Vector2(-1, 2), new Vector2(1, 2), new Vector2(2, 1), new Vector2(2, -1),
        new Vector2(1, -2), new Vector2(-1, -2), new Vector2(-2, -1), new Vector2(-2, 1)};

    /// <summary> The 8 points in the new order they must be connected in. </summary>
    int[] LustrousPointIndices;

    /// <summary> Structure to represent a 2D segment between two points </summary>
    public struct Segment { public Vector2 p1; public Vector2 p2; };

    /// <summary> Since submission is delayed by 2 seconds after the last press,
    /// this boolean blocks commands from being received during that delay to avoid any submission issue. </summary>
    bool TpExclusiveAllowCommands;


    // Universal Logging Data
    static int moduleIdCounter = 1;

    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PressedBinaryButton(0); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressedBinaryButton(1); return false; };
        platePressableButtons[2].OnInteract += delegate () { PressedBinaryButton(2); return false; };
        platePressableButtons[3].OnInteract += delegate () { PressedBinaryButton(3); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        InitializePuzzle();


        // After puzzle initialization, allow TP commands
        TpExclusiveAllowCommands = true;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Interaction
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void PressedBinaryButton(int buttonIndex)
    {
        // Feedback
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }


        // Add a 1 to the current answer that wants to be submitted
        currentPlayerAnswer = currentPlayerAnswer.Remove(buttonIndex, 1).Insert(buttonIndex, "1");

        summoningModule.ModuleLog(moduleId, "Pressed the {0} button. Current answer is {1}",
            GetReadableButtonPosition(buttonIndex), currentPlayerAnswer);


        // If the player has pressed previously, cancel that submission timer
        if (playerSubmissionTimerCoroutine != null)
        {
            StopCoroutine(playerSubmissionTimerCoroutine);
        }

        playerSubmissionTimerCoroutine = StartCoroutine(PlayerAnswerSubmissionCountdown());
    }

    string GetReadableButtonPosition (int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 0: return "Top-Left";
            case 1: return "Top-Right";
            case 2: return "Bottom-Left";
            case 3: return "Bottom-Right";
        }

        return "Unknown";
    }

    IEnumerator PlayerAnswerSubmissionCountdown()
    {
        if (summoningModule.isModuleSolved)
        { yield return false; }

        // While the cooldown is on, prevent new TP commands from passing through
        TpExclusiveAllowCommands = false;

        yield return new WaitForSeconds(2f);

        if (summoningModule.isModuleSolved)
        { yield return false; }

        TpExclusiveAllowCommands = true;

        ComparePlayerAnswer();
    }

    void ComparePlayerAnswer()
    {
        if (currentPlayerAnswer == requiredPlayerAnswer)
        {

            summoningModule.ModuleLog(moduleId, "You correctly submitted the answer {0}", currentPlayerAnswer);

            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "You submitted the answer {0} but the correct answer was {1}!!!", currentPlayerAnswer, requiredPlayerAnswer);

            // Reset player answer
            currentPlayerAnswer = "0000";

            summoningModule.ReceiveStrike();
        }
    }

    protected override void CasingTextButtonGetsPressed() { }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        // StartCoroutine(DeterminePermutationWithMostIntersections());


        DeterminePlateText();
        ApplyLustrousWheelMovements();
        DetermineRequiredPlayerInput();

        currentPlayerAnswer = "0000";
    }

    void DeterminePlateText()
    {
        // Determine the letter + 6 numbers shown on the plate

        startingLetter = alphabet[UnityEngine.Random.Range(0, 8)][0];


        // I do not wish for the same number to appear too much, so a random shuffle is better than actual randomness
        LustrousMovements = new int[] { 2, 2, 3, 4, 5, 5, 6, 6, 7, 8, 8, 9 }.Shuffle().Take(6).ToArray();

        // Concatenate those and add a line break so it's formatted as
        // F 3 8 4
        //  7 8 4
        PlateTextMesh.text = (startingLetter + " " + LustrousMovements.Join(" ")).Remove(7, 1).Insert(7, "\n");

        summoningModule.ModuleLog(moduleId, "Starting letter is {0}, further movements in the Lustrous Wheel are {1}",
            startingLetter, LustrousMovements.Join());
    }

    void ApplyLustrousWheelMovements()
    {
        LustrousPointIndices = new int[8];

        // Determine if movement is clockwise or not
        // Does the Serial Number contain a letter from "PALKIA", or "AIKLP" when sorted alphabetically
        bool isMovementClockwise = bombInfo.GetSerialNumberLetters().Any(x => "PALKIA".Contains(x));
        if (isMovementClockwise)
        {
            summoningModule.ModuleLog(moduleId, "Found a letter in common with AIKLP, movements in the Lustrous Wheel will be clockwise.");
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Did not find a letter in common with AIKLP, movements in the Lustrous Wheel will be counter-clockwise.");
        }


        // Starting character
        LustrousPointIndices[0] = Array.IndexOf(alphabet, startingLetter.ToString());

        // Void Characters
        voidedCellsIndices.Add(LustrousPointIndices[0]);

        summoningModule.ModuleLog(moduleId, "First Point given from the Plate is {0}.", startingLetter);


        int _indexToLookAt = 0;
        int _offset = 0;

        // Following characters
        for (int i = 0; i < 6; i++)
        {
            // Start at the previous chracter's index
            _indexToLookAt = LustrousPointIndices[i];

            // Offset, only positive for now
            _offset = LustrousMovements[i];

            // Moving around the Lustrous Wheel
            for (int j = 0; j < _offset; j++)
            {
                // Offset
                _indexToLookAt += isMovementClockwise ? 1 : -1;

                // Go around the wheel
                if (_indexToLookAt < 0) { _indexToLookAt += 8; }
                else if (_indexToLookAt > 7) { _indexToLookAt -= 8; }

                // Check for Void
                if (voidedCellsIndices.Contains(_indexToLookAt))
                {
                    // This is equivalent to saying "move once more"
                    _offset++;
                }
            }

            // Movements done, register where we landed  
            LustrousPointIndices[i + 1] = _indexToLookAt;

            // Void Character
            voidedCellsIndices.Add(_indexToLookAt);

            summoningModule.ModuleLog(moduleId, "After moving {0} times and passing a total of {1} Voided cells, the next point is {2}",
                LustrousMovements[i], _offset - LustrousMovements[i], alphabet[LustrousPointIndices[i + 1]]);
        }


        // Last character
        // Get the only index [0-7] that is not voided
        LustrousPointIndices[7] = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }.Where(x => voidedCellsIndices.Contains(x) == false).First();

        for (int i = 0; i < 8; i ++)
        {
            finalPointsSequence += alphabet[LustrousPointIndices[i]] + " ";
        }

        summoningModule.ModuleLog(moduleId, "Last Point is {0}; so the points in order are {1}",
            alphabet[LustrousPointIndices[7]], finalPointsSequence);
    }

    void DetermineRequiredPlayerInput()
    {
        numberOfIntersections = ComputeNumberOfIntersectionsInPermutation(LustrousPointIndices);

        // "numbersAsBinary" goes from 00000 to 11111; we only want the rightmost 4 bits though
        requiredPlayerAnswer = numberOfIntersections == 0 ? "1111" : numbersAsBinary[numberOfIntersections].Remove(0, 1);

        summoningModule.ModuleLog(moduleId, "Found a total of {0} intersections with the given point order! The awaited binary number then is {1}",
            numberOfIntersections, numberOfIntersections == 0 ? "1111 because 0 should be represented as all presses" : requiredPlayerAnswer);
    }

    IEnumerator DeterminePermutationWithMostIntersections()
    {
        // Fun fact about that:
        // The most amount of intersections is 13, using points A D G C H E B F in this order for example
        // There's every mirror and rotation of this configuration, but there might even be another permutation with this value.
        // It's just the first one the code saw when evaluating them all.
        // It was found in the first 2,200 permutations; and there are over 40,000 of them... lol



        // For every single possible random combination of the 8 points, find what the biggest number of intersection points is

        // This is going to use the mathematical concept of "Permutations", where a Permutation is a unique sequence of all 8 points, in order, without repeat.
        // 0 1 2 3 4 5 6 7 is one permutation
        // 2 4 7 6 5 1 3 0 is another
        // etc.
        // There are 40,320 permutations; and while I could find a way to compute them on the fly, it is too much work
        // So I used https://numbergenerator.org/permutations-and-combinations/permutations to extract all permutations of 8 numbers to a csv that I'll simply read

        string _allPermutationsOfEightElements = PermutationsOfEight.text;


        // To avoid Unity flagging this whole process as an infinite loop, it will be cut into chunks and done over several frames
        // Actually Unity might not do that, that's why it can get stuck in infinite loops due to badly-done while() loops...
        // I'm too used to Unreal Engine who has a failsafe to avoid infinite loops... woops!!
        // It's still better practice to do it that way
        int _currentChunkNumber = 0; ;
        int _permutationsPerChunk = 40; // Use 40 for the real test
        int _totalPermutationsChunks = 1008; // 1008 for the real test


        // We of course keep track of the record-breaking Permutation
        int _biggestNumberOfIntersections = 0;
        string _biggestIntersectionPermutationSubstring = "";

        // This is just a register of variables that will be used in the loop, to avoid generating and deleting memory over and over
        string _permutationSubstring;
        int _numberOfIntersections;
        int _permutatinStartingCharacterIndex;
        int[] _permutedPointsIndices = new int[8];




        // Separated in a while loop to do a certain number of PointPermutation checks over several frames
        while (_currentChunkNumber < _totalPermutationsChunks)
        {

            // Separating all 40320 PointPermutations in chunks, do one entire chunk at once
            for (int i = 0; i < _permutationsPerChunk; i ++)
            {
                // Find the first character index of the permutation
                _permutatinStartingCharacterIndex = (_currentChunkNumber * _permutationsPerChunk * 16) + (i * 16);

                // Extract the substring corresponding to that Permutation, for easier use
                _permutationSubstring = _allPermutationsOfEightElements.Substring(_permutatinStartingCharacterIndex, 15);

                // summoningModule.ModuleLog(moduleId, "Testing permutation {0}.", _permutationSubstring);


                // Register Points
                for (int _PointIndex = 0; _PointIndex < 8; _PointIndex++)
                {
                    _permutedPointsIndices[_PointIndex] = CharToInt(_permutationSubstring[_PointIndex * 2]);
                }
                

                // Send those points to the NumberOfIntersection-computing method
                _numberOfIntersections = ComputeNumberOfIntersectionsInPermutation(_permutedPointsIndices);


                // If that's more than the previous record...
                if (_numberOfIntersections > _biggestNumberOfIntersections)
                {
                    summoningModule.ModuleLog(moduleId, "Permutation {0} intersect in {1} distinct points; which is more than the previous {2}. Registering it.",
                        _permutationSubstring, _numberOfIntersections, _biggestNumberOfIntersections);

                    // Record that!
                    _biggestNumberOfIntersections = _numberOfIntersections;
                    _biggestIntersectionPermutationSubstring = _permutationSubstring;

                    continue;
                }
            }


            summoningModule.ModuleLog(moduleId, "Finished checking {0} permutations.", _currentChunkNumber * _permutationsPerChunk);

            _currentChunkNumber++;

            yield return new WaitForEndOfFrame();
        }

        summoningModule.ModuleLog(moduleId, "Checked a total of {0} permutations. The biggest number of intersection is {1} with the Permutation {2}.",
            _currentChunkNumber * _permutationsPerChunk, _biggestNumberOfIntersections, _biggestIntersectionPermutationSubstring);
    }


    int ComputeNumberOfIntersectionsInPermutation(int[] pointIndices)
    {
        if (pointIndices.Length != 8)
        {
            summoningModule.ModuleLogError(moduleId, "Tried to check Number of Intersections in Permutation with less than 8 points! Received {0} which is {1} points!", 
                pointIndices.Join(" / "), pointIndices.Length);
            return -1;
        }

        if (pointIndices.Distinct().Count() != 8)
        {
            summoningModule.ModuleLogError(moduleId, "Tried to check Number of Intersections in Permutation with some points identical! Received {0}",
                pointIndices.Join(" / "));
            return -1;
        }


        // Register all 7 Segments
        Segment[] segments = new Segment[7];

        for (int i = 0; i < 7; i++)
        {
            segments[i] = new Segment() { p1 = points[pointIndices[i]], p2 = points[pointIndices[i + 1]]};
        }



        // Prepare memory locations for more optimized computation
        float _t;
        float _u;
        float _intersectionX;
        float _intersectionY;
        Vector2 _intersectionPoint;

        // List of Intersection Points, to filter out the ones that are at the same location
        List<Vector2> _intersectionPointsFound = new List<Vector2>();


        summoningModule.ModuleLog(moduleId, "For the intersections giving coordinates, point A is in (-1; 2), C is in (2; 1), E is in (1; -2), and G is in (-2; -1)");

        // Do intersection check for all 7 segment pairs; but we do not need to check for interversions: 
        // Checks between 0-1 and 1-0 are the same
        for (int _s1 = 0; _s1 < 6; _s1++)
        {
            // We do not need to check 0-0 so segment2 can start as (segment1 + 1)
            // This should all end up at 21 segment checks per SegmentArrayPermutation
            for (int _s2 = _s1 + 1; _s2 < 7; _s2++)
            {

                // summoningModule.ModuleLog(moduleId, "Checking for segments {0} and {1}.", _s1, _s2);

                // Don't check intersections if two points are the same, it doesn't count
                if (segments[_s1].p1 == segments[_s2].p1) { /* summoningModule.ModuleLog(moduleId, "P1 and P3 are the same!!"); */ continue; }
                if (segments[_s1].p1 == segments[_s2].p2) { /* summoningModule.ModuleLog(moduleId, "P1 and P4 are the same!!"); */ continue; }
                if (segments[_s1].p2 == segments[_s2].p1) { /* summoningModule.ModuleLog(moduleId, "P2 and P3 are the same!!"); */ continue; }
                if (segments[_s1].p2 == segments[_s2].p2) { /* summoningModule.ModuleLog(moduleId, "P2 and P4 are the same!!"); */ continue; }


                // Make sure the segments aren't parallel either
                if (AreTwoSegmentsParallel(segments[_s1], segments[_s2])) { /* summoningModule.ModuleLog(moduleId, "Segments are parallel!"); */ continue; }



                // Thanks to https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection#Given_two_points_on_each_line_segment
                // We can check if two segments have an intersection using inequalities
                // _t and _u are values where we must only check if they are within 0 and 1 to know if there is an intersection
                // Then we later determine the actual intersection point if needed

                _t = ComputeT(segments[_s1], segments[_s2]);
                _u = ComputeU(segments[_s1], segments[_s2]);


                if (0 <= _t && _t <= 1 && 0 <= _u && _u <= 1)
                {
                    // In case of an intersection, its coordinates (x, y) are
                    // x = x1 + t*(x2 - x1)
                    // y = y1 + t*(y2 - y1)


                    // Round values to avoid having floating imprecisions be annoying
                    // Otherwise we might have a nice "0.49999999 is not equal to 0.5" issue.
                    _intersectionX = Mathf.Round((segments[_s1].p1.x + _t * (segments[_s1].p2.x - segments[_s1].p1.x)) * 1000) / 1000;
                    _intersectionY = Mathf.Round((segments[_s1].p1.y + _t * (segments[_s1].p2.y - segments[_s1].p1.y)) * 1000) / 1000;

                    _intersectionPoint = new Vector2(_intersectionX, _intersectionY);


                    if (_intersectionPointsFound.Contains(_intersectionPoint) == false)
                    {
                        _intersectionPointsFound.Add(_intersectionPoint);
                        summoningModule.ModuleLog(moduleId, "Found an intersection between segment {0}{1} and {2}{3} at coordinate {4}. This point is unique and is number {5}.",
                            alphabet[pointIndices[_s1]], alphabet[pointIndices[_s1 + 1]], alphabet[pointIndices[_s2]], alphabet[pointIndices[_s2 + 1]],
                            _intersectionPoint, _intersectionPointsFound.Count);
                    }
                    else
                    {
                        summoningModule.ModuleLog(moduleId, "Found an intersection between segment {0}{1} and {2}{3} at coordinate {4}. However this point has already been counted.",
                            alphabet[pointIndices[_s1]], alphabet[pointIndices[_s1 + 1]], alphabet[pointIndices[_s2]], alphabet[pointIndices[_s2 + 1]],
                            _intersectionPoint);
                    }

                    continue;
                }

                // End of this singular segment pair intersection check
            }
        }

        // End of all 21 intersection checks

        return _intersectionPointsFound.Count;
    }


    bool AreTwoSegmentsParallel(Segment s1, Segment s2)
    {
        // For two segments represented by points (x1, y1) (x2, y2) and (x3, y3) (x4, y4) respectively
        // (x1 - x2)(y3 - y4) == (y1 - y2)(x3 - x4) if they are parallel
        return (s1.p1.x - s1.p2.x) * (s2.p1.y - s2.p2.y) == (s1.p1.y - s1.p2.y) * (s2.p1.x - s2.p2.x);
    }

    float ComputeU(Segment s1, Segment s2)
    {
        // For two segments represented by points (x1, y1) (x2, y2) and (x3, y3) (x4, y4) respectively
        // u = -((x1 - x2)(y1 - y3) - (y1 - y2)(x1 - x3)) / ((x1 - x2)(y3 - y4) - (y1 - y2)(x3 - x4))
        return -((s1.p1.x - s1.p2.x) * (s1.p1.y - s2.p1.y) - (s1.p1.y - s1.p2.y) * (s1.p1.x - s2.p1.x))
                            / ((s1.p1.x - s1.p2.x) * (s2.p1.y - s2.p2.y) - (s1.p1.y - s1.p2.y) * (s2.p1.x - s2.p2.x));
    }

    float ComputeT(Segment s1, Segment s2)
    {
        // For two segments represented by points (x1, y1) (x2, y2) and (x3, y3) (x4, y4) respectively
        // t = ((x1 - x3)(y3 - y4) - (y1 - y3)(x3 - x4)) / ((x1 - x2)(y3 - y4) - (y1 - y2)(x3 - x4))
        return ((s1.p1.x - s2.p1.x) * (s2.p1.y - s2.p2.y) - (s1.p1.y - s2.p1.y) * (s2.p1.x - s2.p2.x))
                            / ((s1.p1.x - s1.p2.x) * (s2.p1.y - s2.p2.y) - (s1.p1.y - s1.p2.y) * (s2.p1.x - s2.p2.x));
    }






    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }
        
        // Prevent commands while the 2s cooldown is active, to avoid having sumbitted answers becoming wrong
        if (TpExclusiveAllowCommands == false) 
        {
            yield return "sendtochat {0} Blocked command from passing through as the previous submission hasn't finished processing yet!";
            yield break;
        }

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

        if (commandParts[0] != "submit" &&  commandParts[0] != "s")
        {
            yield return "sendtochaterror {0} Please use keyword Submit or just s to submit an answer";
            yield break;
        }

        if (commandParts[1].Length != 4)
        {
            yield return "sendtochaterror {0} Please submit a four-digit binary number";
            yield break;
        }

        if (commandParts[1].Any(x => x != '1' && x != '0'))
        {
            yield return "sendtochaterror {0} Please submit a four-digit binary number, using only 0 and 1";
            yield break;
        }

        // Reset Player Answer before pressing buttons, just in case
        currentPlayerAnswer = "0000";



        // Press buttons for the answer
        char _treatedCharacter;

        for (int i = 0; i < 4; i ++)
        {
            _treatedCharacter = commandParts[1][i];

            if (_treatedCharacter == '0')
            {

            }
            else if (_treatedCharacter == '1')
            {
                PressedBinaryButton(i);
            }
            else
            {
                yield return "sendtochaterror {0} Received unknown character: '" + _treatedCharacter + "'. The submission has been cancelled and player input has been reset.";
                StopAllCoroutines();
                currentPlayerAnswer = "0000";
                yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        // Preemptively send a solve or strike since the answer is already known 
        if (currentPlayerAnswer == requiredPlayerAnswer)
        {
            yield return "solve";
        }
        else
        {
            yield return "strike";
        }

        yield break;
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        // Hard-resetting the player answer
        currentPlayerAnswer = "0000";

        for (int i = 0; i < 4; i ++)
        {
            if (requiredPlayerAnswer[i] == '1') { PressedBinaryButton(i); }
            yield return new WaitForSeconds(0.2f);
        }

        // Stop coroutines because I'm not sure how TP will like that
        StopAllCoroutines();

        summoningModule.ReceiveSolve();

        yield break;
    }
}
