using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IciclePlate : PlateBase {

    struct Boat { public int icebreakingPower; public int currentResistance; };

    readonly Boat[] possibleBoats = new Boat[10]
    {
        new Boat(){ icebreakingPower = 4, currentResistance = 2},
        new Boat(){ icebreakingPower = 2, currentResistance = 1},
        new Boat(){ icebreakingPower = 3, currentResistance = 2},
        new Boat(){ icebreakingPower = 2, currentResistance = 2},
        new Boat(){ icebreakingPower = 2, currentResistance = 3},
        new Boat(){ icebreakingPower = 4, currentResistance = 3},
        new Boat(){ icebreakingPower = 1, currentResistance = 1},
        new Boat(){ icebreakingPower = 4, currentResistance = 1},
        new Boat(){ icebreakingPower = 3, currentResistance = 3},
        new Boat(){ icebreakingPower = 3, currentResistance = 1}
    };

    readonly int[] startingCells = new int[6] { 1, 1, 1, 1, 1, 1 };

    // Representing the entire board for easier visualization
    readonly int[] crebaseRiverIceTiles = new int[117]
    {
        000, 001, 002,                006, 007, 008,           011, 012,
        013, 014, 015, 016,           019, 020, 021,           024, 025,
        026, 027, 028,                032, 033, 034, 035, 036, 037, 038,
        039, 040, 041,      043,      045, 046,      048,           051,
        052, 053, 054,      056, 057,      059,      061,           064,
        065, 066,           069, 070,                074, 075,      077,
        078, 079,           082,           085,      087, 088,      090,
        091,           094, 095, 096,                     101,      103,
        104,           107, 108, 109,           112,      114,      116,
        117, 118,      120,      122, 123,           126, 127,      129,
        130, 131, 132, 133,                     138, 139,           142,
        143,           146,           149,                     154, 155,
        156,                          162,                          168,
        169,                          175,                          181,
        182,      184,           187,           190,           193, 194,
        195,      197,           200,      202, 203, 204,      206, 207,
        208,      210,           213,      215, 216,           219, 220
    };

    readonly int[] startingLocations = new int[6] { 209, 211, 212, 214, 217, 218 };

    Boat selectedBoat;
    int currentBoatLocationIndex;
    int currentBoatIcebreakingCounter;
    int currentBoatCurrentCounter;
    List<int> visitedTiles = new List<int>();


    public TextMesh rightCurrentsText, leftCurrentsText, voidPlateText;

    List<int> rightCurrents = new List<int>();
    List<string> rightCurrentsTextForm = new List<string>();
    List<int> leftCurrents = new List<int>();
    List<string> leftCurrentsTextForm = new List<string>();

    // Puzzle Generation
    int intendedStartingLocationIndex;
    string intendedBoatMovements = "";
    List<int> tilesInExpectedMovementPath = new List<int>();


    // Universal Logging Data
    static int moduleIdCounter = 1;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;


        platePressableButtons[0].OnInteract += delegate () { PressedAnswerButton(0); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressedAnswerButton(1); return false; };
        platePressableButtons[2].OnInteract += delegate () { PressedAnswerButton(2); return false; };
        platePressableButtons[3].OnInteract += delegate () { PressedAnswerButton(3); return false; };
        platePressableButtons[4].OnInteract += delegate () { PressedAnswerButton(4); return false; };
        platePressableButtons[5].OnInteract += delegate () { PressedAnswerButton(5); return false; };
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

    void PressedAnswerButton(int shipStartingPositionIndex)
    {
        if (summoningModule.isModuleSolved) { return; }

        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch();

        // Set the boat at its starting location
        currentBoatLocationIndex = startingLocations[shipStartingPositionIndex];

        summoningModule.ModuleLog(moduleId, "Pressed button linked to Starting Position {0}! Boat starts in tile {1}",
            shipStartingPositionIndex + 1, GetCoordinateFromCellIndex(currentBoatLocationIndex, 13));

        visitedTiles.Clear();

        // Start Movement!
        // First movement is free, so we can just keep moving!
        BoatMovesInDirection(MovementDirection.Up);
    }


    protected override void CasingTextButtonGetsPressed() { }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Boat Movement & River Probing
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    bool isTileIce(int tileIndex)
    {
        return crebaseRiverIceTiles.Contains(tileIndex);
    }

    void BoatMovesInDirection(MovementDirection direction)
    {
        // Move in direction with Void!
        VoidMovementData _movementData = MoveAroundGridWithVoid(direction, 221, ref currentBoatLocationIndex, 13, false);
        summoningModule.ModuleLog(moduleId, "Boat moved {0} into {1}", direction.ToString(), GetCoordinateFromCellIndex(currentBoatLocationIndex, 13));


        // Where we land is checked against multiple conditions.


        // Did we leave the board upwards?
        if (_movementData.ranIntoGridEdges)
        {
            summoningModule.ModuleLog(moduleId, "The boat correctly escaped Crebase River! Solving module!!");
            summoningModule.ReceiveSolve();
            return;
        }

        // To avoid submitting an infinite loop
        // (which is only if tiles [R][L] appear and you have 1 Current Resistance only)
        // Make sure we are not visiting the same tile twice
        if (visitedTiles.Contains(currentBoatLocationIndex))
        {
            summoningModule.ModuleLog(moduleId, "Boat is landing in this tile for the second time!!! This is indicative of an infinite loop, so the solution is invalid!!");
            summoningModule.ReceiveStrike();
            return;
        }

        visitedTiles.Add(currentBoatLocationIndex);


        // Ice? Increment Icebreaking Counter! (and reset Current counter)
        if (isTileIce(currentBoatLocationIndex))
        {
            // If we moved in any other direction than Up, strike
            if (direction != MovementDirection.Up)
            {
                summoningModule.ModuleLog(moduleId, "It is an Ice Tile, but we moved sideways into it! Strike!");
                summoningModule.ReceiveStrike();
                return;
            }

            // Increment Icebreaking Counter and reset Current Counter
            currentBoatIcebreakingCounter++;
            currentBoatCurrentCounter = 0;

            // If we've broken more ice than allowed, strike!
            if (currentBoatIcebreakingCounter > selectedBoat.icebreakingPower)
            {
                summoningModule.ModuleLog(moduleId, "It is an Ice Tile, but we have broken {0} of them in a row which is above the Icebreaking Power of {1}. Strike!!",
                    currentBoatIcebreakingCounter, selectedBoat.icebreakingPower);
                summoningModule.ReceiveStrike();
                return;
            }

            // Else log and move on
            summoningModule.ModuleLog(moduleId, "It is an Ice Tile. We have broken {0} in a row", currentBoatIcebreakingCounter);

            BoatMovesInDirection(MovementDirection.Up);
            return;
        }
        // Current to the Left?
        else if (leftCurrents.Contains(currentBoatLocationIndex))
        {
            // Reset Icebreaking counter
            currentBoatIcebreakingCounter = 0;

            // If we previously were in a current of the other direction,
            // OR if we got pushed in here by another current,
            // Reset to -1  (Leftwards 1)
            if (currentBoatCurrentCounter >= 0 || direction != MovementDirection.Up)
            {
                currentBoatCurrentCounter = -1;
            }
            // Otherwise just keep decrementing it to show multiple left in a row
            else
            {
                currentBoatCurrentCounter--;
            }

            // Shall we move sideways?
            if (Mathf.Abs(currentBoatCurrentCounter) >= selectedBoat.currentResistance)
            {
                summoningModule.ModuleLog(moduleId, "Landed in leftward current which is enough to make the Boat move leftward!");
                BoatMovesInDirection(MovementDirection.Left);
                return;
            }

            // Otherwise, log then move up!
            summoningModule.ModuleLog(moduleId, "Landed in leftward current, but the Current Resistance prevents the boat from moving leftward.");
            BoatMovesInDirection(MovementDirection.Up);
            return;

        }
        // Current to the Right?
        else if (rightCurrents.Contains(currentBoatLocationIndex))
        {
            // Same as Left, but the other way around
            currentBoatIcebreakingCounter = 0;

            if (currentBoatCurrentCounter <= 0 || direction != MovementDirection.Up)
            {
                currentBoatCurrentCounter = 1;
            }
            else
            {
                currentBoatCurrentCounter++;
            }

            // Shall we move sideways?
            if (currentBoatCurrentCounter >= selectedBoat.currentResistance)
            {
                summoningModule.ModuleLog(moduleId, "Landed in rightward current which is enough to make the Boat move rightward!");
                BoatMovesInDirection(MovementDirection.Right);
                return;
            }

            // Otherwise, log then move up!
            summoningModule.ModuleLog(moduleId, "Landed in rightward current, but the Current Resistance prevents the boat from moving rightward.");
            BoatMovesInDirection(MovementDirection.Up);
            return;
        }
        else
        {
            // None of the above! Just a plain vanilla water tile
            // Reset the counters and move up!
            currentBoatIcebreakingCounter = 0;
            currentBoatCurrentCounter = 0;
            BoatMovesInDirection(MovementDirection.Up);
            return;
        }
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
	{
        // Puzzle Generation is as follows:
        // Ice Blocks are always at the same position
        // Boat Stat are determined by Edgework
        // Then puzzle generates a starting location and a wanted path (according to stats)
        // It adds currents to make that path possible
        // Then adds fake currents that do not affect the current path but are here to throw off the expert


        SelectBoat();
        PlaceVoidTiles();
        SelectStartingLocation();
        GenerateValidPath();
        ExtractDataFromPath();
        AddFakeCurrents();
    }

    void SelectBoat()
    {
        int _lastDigit = bombInfo.GetSerialNumberNumbers().Last();
        selectedBoat = possibleBoats[_lastDigit];

        summoningModule.ModuleLog(moduleId, "Last digit of Serial Number is {0}. The associated Boat has an Icebreaking Power of {1} and Current Resistance of {2}",
            _lastDigit, selectedBoat.icebreakingPower, selectedBoat.currentResistance);
    }

    void PlaceVoidTiles()
    {
        // In the case where the previous Void Tiles resulted in bad RNG, this function can be called multiple times
        voidedCellsIndices.Clear();

        // To avoid potential issues, avoid Void on the edges
        // Randomly generate 6 tiles, but don't re-random; so it *usually* should stay within 3-6 Voids
        int _possibleVoid = 0;

        for (int i = 0; i < 6; i ++)
        {
            // Generate without top and bottom edge
            _possibleVoid = UnityEngine.Random.Range(14, 207);

            // Check to avoid left and right edges; and dupes
            if (_possibleVoid % 13 == 0 || _possibleVoid % 13 == 12 || voidedCellsIndices.Contains(_possibleVoid))
            {
                continue;
            }

            voidedCellsIndices.Add(_possibleVoid);
        }

        IEnumerable<string> voidCoordinates = voidedCellsIndices.Select(x => GetCoordinateFromCellIndex(x, 13));

        summoningModule.ModuleLog(moduleId, "Void tiles will be in coordinates {0}", voidCoordinates.Join());

        // We need to put a line break after the 3rd coordinate; but coordinates can be 2 or 3 characters
        // So instead we need to do some funky Linq stuff
        voidPlateText.text = voidCoordinates.Take(3).Join(" ") + "\n" + voidCoordinates.TakeLast(voidedCellsIndices.Count - 3).Join(" ");
    }

    void SelectStartingLocation()
    {
        intendedStartingLocationIndex = UnityEngine.Random.Range(0, 6);

        currentBoatLocationIndex = startingLocations[intendedStartingLocationIndex];

        summoningModule.ModuleLog(moduleId, "The intended boat starting location will be at index {0} or tile {1}",
            intendedStartingLocationIndex + 1, GetCoordinateFromCellIndex(currentBoatLocationIndex, 13));
    }

    void GenerateValidPath()
    {
        // Using the starting location, void tiles, ice tiles, and boat stats, 
        // Find a movement path (adding currents) that is valid


        // To generate a path, do a recursive chain of functions that check movements just like Blank Plate
        // Only we move X forward for each Y side path so there's not a lot to check!

        // Start generation
        intendedBoatMovements = GenerateSingleTileMovement(currentBoatLocationIndex, 0, ' ', 0);

        // If we get a length of 0, that means there was no valid paths!!
        if (intendedBoatMovements.Length == 0)
        {
            // This is bad; 
            // Place new Void tiles and retry
            PlaceVoidTiles();
            GenerateValidPath();
        }

        summoningModule.ModuleLog(moduleId, "Found a valid movement path starting in tile {0}! The path is {1}",
            GetCoordinateFromCellIndex(currentBoatLocationIndex, 13), intendedBoatMovements);
    }

    void ExtractDataFromPath()
    {
        // We have found the valid path that guarantees that we go from the starting location to outside the playfield
        // Now, we need to extract information from that generation, namely:
        //      - Which cells are on the path (and so are forbidden to receive current on top of them?)
        //      - Which cells should contain current (due to L or R movement, + the current resistance of the boat)


        // Loop for each movement, move a virtual pointer and record every tile into the 


        // Starting location
        currentBoatLocationIndex = startingLocations[intendedStartingLocationIndex];
        tilesInExpectedMovementPath.Add(currentBoatLocationIndex);

        int _currentRecorderCount;
        int _currentRecorderTileIndex;

        // All movements
        foreach (char _movement in intendedBoatMovements)
        {
            switch (_movement)
            {
                case 'U':
                    // Move upwards
                    currentBoatLocationIndex -= 13;

                    // Check if we've left the board
                    if (currentBoatLocationIndex < 0)
                    {
                        // If so, there's nothing to record actually, we can just leave!
                        continue;
                    }

                    // Record tile
                    tilesInExpectedMovementPath.Add(currentBoatLocationIndex);

                    // Check for void, adding tiles until we leave the movement (in which case the foreach will add it itself)
                    while (voidedCellsIndices.Contains(currentBoatLocationIndex))
                    {
                        currentBoatLocationIndex -= 13;
                        tilesInExpectedMovementPath.Add(currentBoatLocationIndex);
                    }
                    break;


                case 'L':
                    // Sideways movement, start by recording the Current
                    // Recording means adding to leftCurrents the tiles
                    // As well as recording the compressed string form for the plate
                    // However there's VOID!!! So we cannot just move down 3 times and be done with it

                    // Loop until we've recorded x tiles
                    _currentRecorderCount = selectedBoat.currentResistance;
                    _currentRecorderTileIndex = currentBoatLocationIndex;
                    while (_currentRecorderCount > 0)
                    {
                        // Skip for void
                        if (voidedCellsIndices.Contains(_currentRecorderTileIndex))
                        { _currentRecorderTileIndex += 13; continue; }

                        // No void? Add to the current
                        leftCurrents.Add(_currentRecorderTileIndex);

                        // We recorded, so decrement
                        _currentRecorderCount--;

                        // And move down
                        _currentRecorderTileIndex += 13;
                    }

                    // After recording, we're offset 1 tile down too much
                    // So move up and record string form
                    _currentRecorderTileIndex -= 13;

                    leftCurrentsTextForm.Add(string.Format("{0}-{1}", GetCoordinateFromCellIndex(_currentRecorderTileIndex, 13), selectedBoat.currentResistance));


                    // Then move Left
                    currentBoatLocationIndex --;

                    // Record Tile
                    tilesInExpectedMovementPath.Add(currentBoatLocationIndex);

                    // Loop for Void
                    while (voidedCellsIndices.Contains(currentBoatLocationIndex))
                    {
                        currentBoatLocationIndex--;
                        tilesInExpectedMovementPath.Add(currentBoatLocationIndex);
                    }
                    break;


                case 'R':
                    // Same as L, but to the Right!

                    _currentRecorderCount = selectedBoat.currentResistance;
                    _currentRecorderTileIndex = currentBoatLocationIndex;
                    while (_currentRecorderCount > 0)
                    {
                        if (voidedCellsIndices.Contains(_currentRecorderTileIndex))
                        { _currentRecorderTileIndex += 13; continue; }

                        rightCurrents.Add(_currentRecorderTileIndex);

                        _currentRecorderCount--;
                        _currentRecorderTileIndex += 13;
                    }

                    _currentRecorderTileIndex -= 13;
                    rightCurrentsTextForm.Add(string.Format("{0}-{1}", GetCoordinateFromCellIndex(_currentRecorderTileIndex, 13), selectedBoat.currentResistance));

                    currentBoatLocationIndex++;
                    tilesInExpectedMovementPath.Add(currentBoatLocationIndex);

                    while (voidedCellsIndices.Contains(currentBoatLocationIndex))
                    {
                        currentBoatLocationIndex++;
                        tilesInExpectedMovementPath.Add(currentBoatLocationIndex);
                    }
                    break;
            }
        }

        // All the data has been extracted!
    }

    void AddFakeCurrents()
    {
        // Now that we have a valid certain path, we can add random Current on tiles that are not on the path
        // We can also add current on the path, but only if we are certain its length is lower than the resistance
        // To avoid multiple "separate" currents mixing into one bigger that would affect the path, only 1L and 1R are allowed on the path
        // And only if the boat has resistance > 1

        FillCurrentsForOneDirection(ref leftCurrentsTextForm, ref leftCurrents);
        FillCurrentsForOneDirection(ref rightCurrentsTextForm, ref rightCurrents);


        // Once we have all currents, randomize the arrays before showing them, to avoid giving away the information ^^'
        leftCurrentsTextForm.Shuffle();
        rightCurrentsTextForm.Shuffle();

        // Show the currents on the plate
        leftCurrentsText.text = string.Format("LEFT:  {0}\n{1}  {2}\n{3}  {4}\n{5}  {6}\n{7}  {8}",
            leftCurrentsTextForm[0], leftCurrentsTextForm[1], leftCurrentsTextForm[2], leftCurrentsTextForm[3], leftCurrentsTextForm[4],
            leftCurrentsTextForm[5], leftCurrentsTextForm[6], leftCurrentsTextForm[7], leftCurrentsTextForm[8]);

        rightCurrentsText.text = string.Format("RIGHT:  {0}\n{1}  {2}\n{3}  {4}\n{5}  {6}\n{7}  {8}",
            rightCurrentsTextForm[0], rightCurrentsTextForm[1], rightCurrentsTextForm[2], rightCurrentsTextForm[3], rightCurrentsTextForm[4],
            rightCurrentsTextForm[5], rightCurrentsTextForm[6], rightCurrentsTextForm[7], rightCurrentsTextForm[8]);

        summoningModule.ModuleLog(moduleId, "All rightward currents are in tiles {0}.", rightCurrents.Select(x => GetCoordinateFromCellIndex(x, 13)).Join());
        summoningModule.ModuleLog(moduleId, "All leftward currents are in tiles {0}.", leftCurrents.Select(x => GetCoordinateFromCellIndex(x, 13)).Join());
    }


    void FillCurrentsForOneDirection(ref List<string> directionTextForm, ref List<int> directionCoordinates)
    {
        int _startingCurrentCoordinate;
        int _currentCurrentCoordinate;
        int _maximumUpwardsCurrentLength;
        int _currentCurrentLength;

        // Boolean used to determine if we've added the first "decoy" that is on the path
        // If true, add currents outside of the path
        // If false, add ONCE a current on the path, of length explicitely lower than current resistance
        bool addedPathFakeout = false;

        // Cannot add decoy if the Current Resistance is 1
        if (selectedBoat.currentResistance == 1)
        { addedPathFakeout = true; }


        int testlimit = 100;

        // We want 9 grouping of left and right currents, for 18 total
        while (directionTextForm.Count < 9)
        {
            testlimit--;
            if (testlimit == 0) { summoningModule.ModuleLogError(moduleId, "YIKES!!, There was an error during generation!! Please report this log to thunder725 with error code 'Current Fill 100 Test Limit'!!!"); }

            // Splatter a point, not on the top nor bottom edges
            _startingCurrentCoordinate = UnityEngine.Random.Range(14, 207);

            // Make sure it is not in the ice
            if (isTileIce(_startingCurrentCoordinate)) { continue; }

            // nor in the void
            if (voidedCellsIndices.Contains(_startingCurrentCoordinate)) { continue; }

            // nor in the path
            // but only if we've already added a Path Fakeout!
            if (addedPathFakeout && tilesInExpectedMovementPath.Contains(_startingCurrentCoordinate)) { continue; }
            

            // nor in any of the already-existing currents
            if (leftCurrents.Contains(_startingCurrentCoordinate)) { continue; }
            if (rightCurrents.Contains(_startingCurrentCoordinate)) { continue; }


            // If we arrive here, we have a valid point that is just water, outside of the path!
            // How long should it last maximum? 
            if (addedPathFakeout)
            {
                // within 1 and 5 tiles for regular decoy currents
                _maximumUpwardsCurrentLength = UnityEngine.Random.Range(0, 5);
            }
            else 
            {
                // within 1 and currentResistance-1 for path decoy
                _maximumUpwardsCurrentLength = UnityEngine.Random.Range(0, selectedBoat.currentResistance);
            }
            

            // Start the loop
            _currentCurrentLength = 0;
            _currentCurrentCoordinate = _startingCurrentCoordinate;


            // And move up while recording
            while (_currentCurrentLength <= _maximumUpwardsCurrentLength)
            {
                // Do the checks that stop the current
                if (_currentCurrentCoordinate < 0) { break; }
                if (isTileIce(_currentCurrentCoordinate)) { break; }
                if (addedPathFakeout && tilesInExpectedMovementPath.Contains(_currentCurrentCoordinate)) { break; }
                if (leftCurrents.Contains(_currentCurrentCoordinate)) { break; }
                if (rightCurrents.Contains(_currentCurrentCoordinate)) { break; }

                // Void just moves one tile up more without recording
                if (voidedCellsIndices.Contains(_currentCurrentCoordinate)) { _currentCurrentCoordinate -= 13; continue; }

                // if we land in pure water
                // Record
                directionCoordinates.Add(_currentCurrentCoordinate);
                _currentCurrentLength++;

                // Move upwards
                _currentCurrentCoordinate -= 13;
            }

            // At the end, add it to the TextForm!
            directionTextForm.Add(string.Format("{0}-{1}", GetCoordinateFromCellIndex(_startingCurrentCoordinate, 13), _currentCurrentLength));

            // Make sure we do not add fake currents on the path anymore
            addedPathFakeout = true;
        }
    }


    /// <summary>
    /// Recursive Function!
    /// Using the same logic as Blank Plate's generation, we return a string denominating failure if empty.
    /// Every generation goes one tile further, and if possible, explores the sides using currents
    /// Once the top of the map is reached, we return the path in movement form (string)
    /// This will help us finish the generation.
    /// This string is gotten from the movementDirection characters that are passed (U, R or L)
    /// </summary>
    string GenerateSingleTileMovement(int tileIndexToCheck, int potentialCurrentCounter, char movementDirection, int cumulativeIceBroken)
    {
        // Did we reach beyond the final tile? Well that's a success!
        // Start by returning your Movement Direction so we know what the final movement was!
        if (tileIndexToCheck < 0)
        {
            return movementDirection + "";
        }

        // Void? Just Skip to the next one without any modification
        // simply return to avoid any modification
        if (voidedCellsIndices.Contains(tileIndexToCheck))
        {
            // Next one can be horizontal though!
            switch (movementDirection)
            {
                case 'U':
                    return GenerateSingleTileMovement(tileIndexToCheck - 13, potentialCurrentCounter, movementDirection, cumulativeIceBroken);
                case 'R':
                    return GenerateSingleTileMovement(tileIndexToCheck + 1, 1, movementDirection, cumulativeIceBroken);
                case 'L':
                    return GenerateSingleTileMovement(tileIndexToCheck - 1, 1, movementDirection, cumulativeIceBroken);
            }
            return string.Empty;
        }


        
        

        // Are we in ice?
        if (isTileIce(tileIndexToCheck))
        {
            // Not allowed to sideways into ice!
            if (movementDirection != 'U')
            {
                return string.Empty;
            }

            // Increment the Cumulative Ice
            cumulativeIceBroken++;

            // There cannot be current on an ice tile, so reset that too
            potentialCurrentCounter = 0;

            // Check if the boat cannot withstand the ice, if the cumulative is too big
            if (cumulativeIceBroken > selectedBoat.icebreakingPower)
            {
                return string.Empty;
            }

            // Otherwise we are breaking the ice and that is just valid!
        }

        // Every movement we go up if the tile is valid (non-ice or ice that can be broken using power)
        // Then we increment a counter of potential Current
        // Currents can start at any tile, not just at the first available tile in the straight path
        // So we can move horizontally on any tile, as long as the counter of potential Current is >= to the boat's current resistance


        // The key difference with BlankPlate is that we are not checking if a generation is valid, we are checking for interesting paths
        // So we shouldn't just return the first valid movement path and that's it, it will end up hugging the right wall since the
        // Movement Right is first in the code
        // Instead, every generation the order of movements (up, right, left) is randomized!
        List<char> _allowedMovements = new List<char>() { 'U' };


        // Are we allowed to go sideways?
        if (potentialCurrentCounter >= selectedBoat.currentResistance)
        {
            // Do not add infinite loops
            // So if we just moved Right, we're not allowed to move Left immediately

            if (movementDirection != 'R')
            { _allowedMovements.Add('L'); }

            if (movementDirection != 'L')
            { _allowedMovements.Add('R'); }
        }


        // Randomize movements order and do them
        _allowedMovements.Shuffle();


        // We don't just pick the first one and that's it; because it might not be correct!
        // So we still do a foreach that can return if needed
        foreach (char _movementToCheck in _allowedMovements)
        {
            switch( _movementToCheck )
            {
                // Test Up!
                case 'U':
                    intendedBoatMovements = GenerateSingleTileMovement(tileIndexToCheck - 13, potentialCurrentCounter + 1, 'U', cumulativeIceBroken);
                    if (intendedBoatMovements.Length > 0)
                    {
                        return movementDirection + intendedBoatMovements;
                    }
                    continue;

                // Test Right!
                case 'R':
                    intendedBoatMovements = GenerateSingleTileMovement(tileIndexToCheck + 1, 1, 'R', 0);
                    if (intendedBoatMovements.Length > 0)
                    {
                        return movementDirection + intendedBoatMovements;
                    }
                    continue;

                // Test Left!
                case 'L':
                    intendedBoatMovements = GenerateSingleTileMovement(tileIndexToCheck - 1, 1, 'L', 0);
                    if (intendedBoatMovements.Length > 0)
                    {
                        return movementDirection + intendedBoatMovements;
                    }
                    continue;
            }
        }


        // None of the above movement checks were valid, so this entire subtree is incorrect
        return string.Empty;
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

        if (commandParts[1].Length != 1)
        {
            yield return "sendtochaterror {0} Received unknown boat location. Please send a single digit within 1 and 6.";
            yield break;
        }

        int _submittedBoatStartingLocation = int.Parse(commandParts[1]);

        if (_submittedBoatStartingLocation < 1 || _submittedBoatStartingLocation > 6)
        {
            yield return "sendtochaterror {0} Received unknown boat location. Please send a single digit within 1 and 6.";
            yield break;
        }

        platePressableButtons[_submittedBoatStartingLocation - 1].OnInteract();
        yield break;
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        platePressableButtons[intendedStartingLocationIndex].OnInteract();

        yield break;
    }
}
