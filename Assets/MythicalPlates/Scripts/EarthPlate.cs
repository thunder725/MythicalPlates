using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EarthPlate : PlateBase {

    /// <summary>
    /// The field is a giant 12x12 grid of 144 tiles; but only 93 of them are valid
    /// </summary>
    readonly int[] allowedTiles = new int[93] {
        000, 001, 002,                006, 007, 008,
        012, 013, 014,                018, 019, 020,
        024, 025, 026,                030, 031, 032, 033, 034,
        036, 037, 038,                042, 043, 044, 045, 046,
        048, 049, 050,                054, 055, 056, 057, 058,
             061, 062,                066, 067, 068, 069, 070,
             073, 074, 075,      077, 078, 079, 080, 081, 082,
             085, 086, 087,      089, 090,           093, 094, 095,
             097, 098, 099, 100, 101, 102,           105, 106, 107,
        108, 109, 110, 111, 112, 113, 114,           117, 118, 119,
        120, 121, 122, 123, 124, 125,                129, 130, 131,
        132, 133,                                    141, 142, 143
    };

    /// <summary> Store the union of the row and columns to void both at once </summary>
    readonly Dictionary<int, int[]> voidLines = new Dictionary<int, int[]>()
    {
        {1, new int[23]{0, 12, 24, 36, 48, 60, 72, 84, 96, 108, 120, 132, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95}},
        {2, new int[23]{3, 15, 27, 39, 51, 63, 75, 87, 99,  111, 123, 135, 36, 37, 38, 40, 41, 42, 43, 44, 45, 46, 47}},
        {3, new int[23]{4, 16, 28, 40, 52, 64, 76, 88, 100, 112, 124, 136, 48, 49, 50, 51, 53, 54, 55, 56, 57, 58, 59}},
        {4, new int[23]{5, 17, 29, 41, 53, 65, 77, 89, 101, 113, 125, 137, 120, 121, 122, 123, 124, 126, 127, 128, 129, 130, 131}},
        {5, new int[23]{8, 20, 32, 44, 56, 68, 80, 92, 104, 116, 128, 140, 12, 13, 14, 15, 16, 17, 18, 19, 21, 22, 23}},
        {6, new int[23]{9, 21, 33, 45, 57, 69, 81, 93, 105, 117, 129, 141, 60, 61, 62, 63, 64, 65, 66, 67, 68, 70, 71}}
    };


    int startingTileIndex, targetTileIndex;
    int currentTileIndex;

    // Universal Logging Data
    static int moduleIdCounter = 1;


    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;


        platePressableButtons[0].OnInteract += delegate () { PressedMovementButton(1); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressedMovementButton(2); return false; };
        platePressableButtons[2].OnInteract += delegate () { PressedMovementButton(3); return false; };
        platePressableButtons[3].OnInteract += delegate () { PressedMovementButton(4); return false; };
        platePressableButtons[4].OnInteract += delegate () { PressedMovementButton(5); return false; };
        platePressableButtons[5].OnInteract += delegate () { PressedMovementButton(6); return false; };
        platePressableButtons[6].OnInteract += delegate () { PressedMovementButton(7); return false; };
        platePressableButtons[7].OnInteract += delegate () { PressedMovementButton(8); return false; };
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

    void PressedMovementButton(int LShapeMovementIndex)
    {
        // Feedback
        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        summoningModule.ModuleLog(moduleId, "Pressed movement button {0}.", LShapeMovementIndex);
        MoveInLShape(LShapeMovementIndex);
    }

    protected override void CasingTextButtonGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }        


        summoningModule.ModuleLog(moduleId, "EARTH casing button got pressed! Resetting to the starting position.");
        currentTileIndex = startingTileIndex;
    }


    void MoveInLShape(int LShapeMovementIndex)
    {
        // All 8 movements can be done either by moving first horizontally or first vertically
        // Both have to be checked, because you can have paths that are valid only in one way and not the other
        // since it's forbidden to exit the grid's bounds.

        // S-shaped or Z-shaped movements are not valid, so the length that is 2-tile long is done at once.

        int _tryoutIndex = currentTileIndex;

        // Try to move in a direction, the method returns the tile index if the movement was valid, or a -1 otherwise
        // The method also moves once or twice by itself depending on the movement that is asked.

        // Try first to move horizontally then vertically
        _tryoutIndex = MoveHorizontallyFromLShape(LShapeMovementIndex, _tryoutIndex);
        if (_tryoutIndex != -1)
        {
            _tryoutIndex = MoveVerticallyFromLShape(LShapeMovementIndex, _tryoutIndex);
            if (_tryoutIndex != -1)
            {
                // We succeeded a Horizontal then Vertical movement!
                // So the press was valid.
                ApplyValidMovement(_tryoutIndex);
                return;
            }
        }

        // Otherwise try to move vertically then horizontally
        _tryoutIndex = currentTileIndex;

        _tryoutIndex = MoveVerticallyFromLShape(LShapeMovementIndex, _tryoutIndex);
        if (_tryoutIndex != -1)
        {
            _tryoutIndex = MoveHorizontallyFromLShape(LShapeMovementIndex, _tryoutIndex);
            if (_tryoutIndex != -1)
            {
                // We succeeded a Horizontal then Vertical movement!
                // So the press was valid.
                ApplyValidMovement(_tryoutIndex);
                return;
            }
        }

        // If we reach this point, both failed, so that is a Strike!!
        summoningModule.ModuleLog(moduleId, "That movement brought you outside the bounds of the grid, so it is invalid. Movement has been cancelled");
        summoningModule.ReceiveStrike();
        return;
    }

    /// <summary> Once we know that the movement was valid, apply it for real! </summary>
    void ApplyValidMovement(int newTile)
    {
        currentTileIndex = newTile;

        if (currentTileIndex == targetTileIndex)
        {
            summoningModule.ModuleLog(moduleId, "Successfully moved to tile {0}; which is your Target Tile!!!", GetCoordinateFromCellIndex(currentTileIndex, 12));
            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Successfully moved to tile {0}", GetCoordinateFromCellIndex(currentTileIndex, 12));
        }
    }

    int MoveHorizontallyFromLShape(int LShapeMovementIndex, int startingPosition)
    {
        // Move to the Right for directions 2 3 4 5
        MovementDirection directionToMoveTowards;
        if (LShapeMovementIndex > 1 && LShapeMovementIndex < 6)
        {
            directionToMoveTowards = MovementDirection.Right;
        }
        // But to the left for 1 6 7 8
        else
        {
            directionToMoveTowards = MovementDirection.Left;
        }

        VoidMovementData _movementData = MoveAroundGridWithVoid(directionToMoveTowards, 144, ref startingPosition, 12, false);

        // It is forbidden to go outside of the 12x12 grid or to land in a non-allowed tile
        if (_movementData.ranIntoGridEdges || (allowedTiles.Contains(startingPosition) == false))
        {
            return -1;
        }


        // If number is 1 2 5 6, that is the only movement we need to do
        // 1 2 5 6 become 0 1 4 5 which when %4 become 0 1 0 1 as opposed to 2 3 2 3 so that works
        if ((LShapeMovementIndex - 1) % 4 < 2)
        {
            return startingPosition;
        }

        // Otherwise, number is 3 4 7 8 so we move again and do the verification again
        _movementData = MoveAroundGridWithVoid(directionToMoveTowards, 144, ref startingPosition, 12, false);

        if (_movementData.ranIntoGridEdges || (allowedTiles.Contains(startingPosition) == false))
        {
            return -1;
        }
        return startingPosition;
    }

    int MoveVerticallyFromLShape(int LShapeMovementIndex, int startingPosition)
    {
        // Move Down for directions 4 5 6 7
        MovementDirection directionToMoveTowards;
        if (LShapeMovementIndex > 3 && LShapeMovementIndex < 8)
        {
            directionToMoveTowards = MovementDirection.Down;
        }
        // otherwise to Up
        else
        {
            directionToMoveTowards = MovementDirection.Up;
        }

        VoidMovementData _movementData = MoveAroundGridWithVoid(directionToMoveTowards, 144, ref startingPosition, 12, false);

        // It is forbidden to go outside of the 12x12 grid or to land in a non-allowed tile
        if (_movementData.ranIntoGridEdges || (allowedTiles.Contains(startingPosition) == false))
        {
            return -1;
        }


        // If number is 3 4 7 8, that is the only movement we need to do
        // 3 4 7 8 become 2 3 6 7 which become 2 3 2 3 after %4 so that works
        if ((LShapeMovementIndex - 1) % 4 > 1)
        {
            return startingPosition;
        }

        // Otherwise, number is 1 2 5 6 so we move again and do the verification again
        _movementData = MoveAroundGridWithVoid(directionToMoveTowards, 144, ref startingPosition, 12, false);

        if (_movementData.ranIntoGridEdges || (allowedTiles.Contains(startingPosition) == false))
        {
            return -1;
        }
        return startingPosition;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        DecideStartAndTargetTiles();
        DetermineVoids();

        currentTileIndex = startingTileIndex;
    }

    /// <summary> Determine which tile is the Start and which is the Target using bomb start time </summary>
    void DecideStartAndTargetTiles()
    {
        // Round to avoid being tricked by the time being 449.94 instead of 450
        float bombTime = Mathf.Round(bombInfo.GetTime());

        // Bomb Time starts below 4500 seconds
        if (bombTime < 4500)
        {
            startingTileIndex = 1;
            targetTileIndex = 142;
            summoningModule.ModuleLog(moduleId, "Starting Bomb Time has been detected as {0} seconds which is less than 4500 (75 minutes). Starting cell is B1 and target cell is K12", bombTime);
        }
        else
        {
            startingTileIndex = 142;
            targetTileIndex = 1;
            summoningModule.ModuleLog(moduleId, "Starting Bomb Time has been detected as {0} seconds which is more than 4500 (75 minutes). Starting cell is K12 and target cell is B1", bombTime);
        }
    }

    /// <summary> Void lines and columns using existing Port types </summary>
    void DetermineVoids()
    {
        IEnumerable<string> allPorts = bombInfo.GetPorts().Distinct();

        if (allPorts.Count() == 0)
        {
            summoningModule.ModuleLog(moduleId, "No Ports found. That means there is no void this time!");
        }
        else
        {
            int[] _cellsToVoid = new int[0];
            int _portLineNumber = 0;

            foreach (string port in allPorts)
            {
                switch (port)
                {
                    case "Serial":
                        _portLineNumber = 1;
                        break;

                    case "StereoRCA":
                        _portLineNumber = 2;
                        break;

                    case "PS2":
                        _portLineNumber = 3;
                        break;

                    case "DVI":
                        _portLineNumber = 4;
                        break;

                    case "RJ45":
                        _portLineNumber = 5;
                        break;

                    case "Parallel":
                        _portLineNumber = 6;
                        break;
                }

                voidLines.TryGetValue(_portLineNumber, out _cellsToVoid);

                summoningModule.ModuleLog(moduleId, "Found a {0} port! Voiding row and column number {1}.", port, _portLineNumber);
                voidedCellsIndices.AddRange(_cellsToVoid);
            }
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

        if (command == "earth")
        {
            yield return "sendtochat {0} Successfully pressed EARTH and reset you to your starting location.";
            CasingTextButtonGetsPressed();
            yield break;
        }

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

        if (commandParts[1].Length == 0)
        {
            yield return "sendtochaterror {0} Received unknown movement! Please send a string of movements like '1357682'.";
            yield break;
        }

        int _movementNumber;

        foreach (char _movement in commandParts[1])
        {
            _movementNumber = CharToInt(_movement);

            if (_movementNumber > 0 && _movementNumber < 9)
            {
                platePressableButtons[_movementNumber - 1].OnInteract();
            }
            else
            {
                yield return "sendtochaterror {0} Received unknown movement number: '" + _movement + "'! Previous movements have been done, future movements have been aborted.";
                yield break;
            }

            yield return new WaitForSeconds(0.15f);
        }

    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        summoningModule.ReceiveSolve();

        yield break;
    }

}
