using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DracoPlate : PlateBase
{
    string dracoGrid;
    /// <summary> Only non-edge cells are allowed to become colored </summary>
    readonly int[] allowedColoredCellsIndices = new int[36] { 9, 10, 11, 12, 13, 14, 17, 18, 19, 20, 21, 22, 25, 26, 27, 28, 29, 30, 33, 34, 35, 36, 37, 38, 41, 42, 43, 44, 45, 46, 49, 50, 51, 52, 53, 54};
    int cyanCellIndex, magentaCellIndex, yellowCellIndex;
    string yellowDistanceField;
    string lineToSubmit;
    string submittedLine;

    /// <summary> Temporary pointer to an index inside of the table, for keeping track of locations when moving around </summary>
    int scratchLocationPointer;
    List<int> cellsMarkedForToggle;

    [SerializeField] TextMesh encryptedGridInscription;
    [SerializeField] TextMesh coloredCellsCoordinatesInscription;

    // Universal Logging Data
    static int moduleIdCounter = 1;


    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PressingPlateButton("k"); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressingPlateButton("w"); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        GenerateVoidCellsFromBombData();
        GenerateStartingGrid();

        EncryptingGridIntoBinary();
        ShowColoredCellsOnModule();

        cellsMarkedForToggle = new List<int>();

        // Apply the Colored Steps
        ApplyCyanCellRules();
        ApplyMagentaCellRules();
        ApplyYellowCellRules();

        DetermineFinalRequiredSubmission();
        submittedLine = "";
    }

    // public override void UpdateModule() { base.UpdateModule(); }

    
    void PressingPlateButton(string buttonColor)
    {
        if (summoningModule.isModuleSolved) { return; }

        platePressableButtons[0].AddInteractionPunch();

        // Save the input
        submittedLine += buttonColor;

        // If value was incorrect
        if (submittedLine.Last() != lineToSubmit[submittedLine.Length - 1])
        {
            summoningModule.ModuleLog(moduleId, "You pressed {0}, which was incorrect. Expected {1}. Not adding this to the current submission.", buttonColor == "w" ? "White" : "Black", lineToSubmit[submittedLine.Length - 1] == 'w' ? "White" : "Black");

            // Remove the previous input
            submittedLine = submittedLine.Substring(0, submittedLine.Length - 1);

            summoningModule.ReceiveStrike();
        }
        // If the two are the same!
        else if (lineToSubmit == submittedLine)
        {
            summoningModule.ModuleLog(moduleId, "You pressed {0}, which is correct.", buttonColor == "w" ? "White" : "Black");
            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "You pressed {0}, which is correct.", buttonColor == "w" ? "White" : "Black");
        }

    }

    void GenerateVoidCellsFromBombData()
    {
        // Letter-Number pairs, top-left is A1

        List<char> _serialNumberLetters = bombInfo.GetSerialNumberLetters().ToList();
        List<int> _serialNumberNumbers = bombInfo.GetSerialNumberNumbers().ToList();

        // Make same size
        while (_serialNumberLetters.Count > _serialNumberNumbers.Count)
        { _serialNumberLetters.RemoveAt(_serialNumberNumbers.Count); }
        while (_serialNumberNumbers.Count > _serialNumberLetters.Count)
        { _serialNumberNumbers.RemoveAt(_serialNumberLetters.Count); }

        int row, col;
        for (int i = 0; i < _serialNumberLetters.Count; i ++)
        {
            // "+ 7" on the row to transform 0 into 7 (last row), and 1 into 0 (first row)
            row = (_serialNumberNumbers[i] + 7) % 8;
            col = (Array.IndexOf(alphabet, _serialNumberLetters[i].ToString())) % 8;

            voidedCellsIndices.Add( 8 * row + col);
        }
    }

    void GenerateStartingGrid()
    {
        dracoGrid = "";

        // Generate just White, Black, and Void
        for (int i = 0; i < 64; i++)
        {
            if (voidedCellsIndices.Contains(i))
            {
                dracoGrid += "V";
                continue;
            }
            dracoGrid += UnityEngine.Random.value > 0.5f ? "w" : "k";
        }

        // Randomize the allowed locations, and remove Voided locations to not override them
        List<int> _allowedCellsResults = allowedColoredCellsIndices.Shuffle().ToList();
        _allowedCellsResults.RemoveAll(nb => voidedCellsIndices.Contains(nb));


        int _index = 0;

        for (int i = 0; i < 3; i++)
        {
            _index = _allowedCellsResults[i];

            // Insert the colored cell into the grid
            switch (i)
            {
                case 0:
                    dracoGrid = dracoGrid.Remove(_index, 1).Insert(_index, "C");
                    cyanCellIndex = _index;
                    break;
                case 1:
                    dracoGrid = dracoGrid.Remove(_index, 1).Insert(_index, "M");
                    magentaCellIndex = _index;
                    break;
                case 2:
                    dracoGrid = dracoGrid.Remove(_index, 1).Insert(_index, "Y");
                    yellowCellIndex = _index;
                    break;
            }
        }

        // There was an idea to tweak the cells surrounding Magenta, to make them more White
        // and ensure that "2 Whites" doesn't appear too often (many cells will have 2 whites)
        // However, Magenta is not applied first, so it doesn't really matter, and the 2 whites might still happen.
        // So it's better to change the rules to make them less tiresome instead.



        summoningModule.ModuleLog(moduleId, "Initial Grid State:");
        PrintDracoGrid();
    }

    void EncryptingGridIntoBinary()
    {
        string _binaryEncryptedGrid = "";

        string _rowToEncrypt;
        int _tempRowResult;

        for (int i = 0; i < 8; i++)
        {
            // Get the next 8 bits
            _rowToEncrypt = dracoGrid.Substring(8 * i, 8);
            _tempRowResult = 0;

            // Convert the byte into its decimal value
            // Only Black & Void count as 0, colors count as 1
            for (int j = 0; j < 8; j ++)
            {
                // Can't use IsCellWhite() because that checks the table directly
                // There could be a way to do it easily, but this works
                if (_rowToEncrypt[7 - j] != 'k' && _rowToEncrypt[7 - j] != 'V')
                {
                    _tempRowResult += (int)Mathf.Pow(2, j);
                }
            }

            // Add it
            _binaryEncryptedGrid += _tempRowResult.ToString() + (i == 3 ? "\n" : " ");
        }

        summoningModule.ModuleLog(moduleId, "Binary-Encrypted grid becomes: {0}", _binaryEncryptedGrid.Replace("\n", " "));

        // Show on module
        encryptedGridInscription.text = _binaryEncryptedGrid;
    }

    /// <summary> Inscribes on the physical Plate the coordinated of Cyan Magenta and Yellow cells </summary>
    void ShowColoredCellsOnModule()
    {
        string _coloredCellsCoordinates = GetCoordinateFromCellIndex(cyanCellIndex, 8) + " " + GetCoordinateFromCellIndex(magentaCellIndex, 8) + " " + GetCoordinateFromCellIndex(yellowCellIndex, 8);

        summoningModule.ModuleLog(moduleId, "Colored Cells coordiantes are: {0}", _coloredCellsCoordinates);
        coloredCellsCoordinatesInscription.text = _coloredCellsCoordinates;
    }

    void ApplyCyanCellRules()
    {
        summoningModule.ModuleLog(moduleId, "Applying Cyan rules.");

        // For every cell in the row and column of Cyan Cell
        // Toggle, then try to apply GoL rules

        int _cyanRow = GetRowFromCellIndex(cyanCellIndex, 8);
        int _cyanColumn = GetColumnFromCellIndex(cyanCellIndex, 8);

        cellsMarkedForToggle.Clear();

        List<int> _cellsToVerifyGameOfLifeRulesOn = new List<int>();

        // Register and Toggle
        for (int i = 0; i < 8; i ++)
        {
            // Cells in the same Row
            scratchLocationPointer = 8 * _cyanRow + i;
            // Don't save to check if the cell isn't black or white
            // Also don't save Cyan cell because it's useless
            if (scratchLocationPointer != cyanCellIndex && IsCellToggleable(scratchLocationPointer))
            {
                ToggleCell(scratchLocationPointer);
                _cellsToVerifyGameOfLifeRulesOn.Add(scratchLocationPointer);
            }


            // Cells in the same Column
            scratchLocationPointer = 8 * i + _cyanColumn;
            if (scratchLocationPointer != cyanCellIndex && IsCellToggleable(scratchLocationPointer))
            {
                ToggleCell(scratchLocationPointer);
                _cellsToVerifyGameOfLifeRulesOn.Add(scratchLocationPointer);
            }
        }

        // THEN Apply GoL rules (after all toggles)
        foreach (int _cellToCheck in _cellsToVerifyGameOfLifeRulesOn)
        {
            if (DoesCellNeedToggleFromGameOfLifeRuleset(_cellToCheck))
            {
                cellsMarkedForToggle.Add(_cellToCheck);
            }
        }


        // Toggle all Marked cells at once
        if (cellsMarkedForToggle.Count > 0)
        {
            foreach (int _index in cellsMarkedForToggle)
            {
                ToggleCell(_index);
            }
        }

        PrintDracoGrid();
    }

    void ApplyMagentaCellRules()
    {
        summoningModule.ModuleLog(moduleId, "Applying Magenta rules:");

        // For every CMY and Voided cell:
        // Toggle the 4 surrounding cells, and toggle the 180° projected cell

        cellsMarkedForToggle.Clear();

        // Add for toggle all colored cells
        cellsMarkedForToggle.AddRange(ReturnNeighborCells(cyanCellIndex, 4));
        cellsMarkedForToggle.AddRange(ReturnNeighborCells(magentaCellIndex, 4));
        cellsMarkedForToggle.AddRange(ReturnNeighborCells(yellowCellIndex, 4));

        // Add for toggle the reverse of those cells
        cellsMarkedForToggle.Add(63 - cyanCellIndex);
        cellsMarkedForToggle.Add(63 - magentaCellIndex);
        cellsMarkedForToggle.Add(63 - yellowCellIndex);


        // Add for toggle all Voided Cells
        foreach (int _void in voidedCellsIndices)
        {

            cellsMarkedForToggle.AddRange(ReturnNeighborCells(_void, 4));

            // And their reverse
            cellsMarkedForToggle.Add(63 - _void);
        }
        


        // Toggle all Marked cells at once
        if (cellsMarkedForToggle.Count > 0)
        {
            foreach (int _index in cellsMarkedForToggle)
            {
                // Don't toggle "-1"
                if (_index == -1) { continue; }

                ToggleCell(_index);
            }
        }

        PrintDracoGrid();
    }

    void ApplyYellowCellRules()
    {
        summoningModule.ModuleLog(moduleId, "Applying Yellow rules:");

        // For every cell, compute Manhattan Distance to Yellow
        // Toggle cells that are 2, 4 and 8 away.

        // Manhattan Distance is of course just a Distance Field...
        //          ...With Voided Cells
        GenerateYellowManhattanDistanceField();


        // For each cell in the grid, if there are WhiteCells > DistanceField
        // Then toggle
        cellsMarkedForToggle.Clear();

        for (int i = 0; i < 64; i ++)
        {
            scratchLocationPointer = CharToInt(yellowDistanceField[i]);

            if (scratchLocationPointer == 2 || scratchLocationPointer == 4 || scratchLocationPointer == 8)
            {
                cellsMarkedForToggle.Add(i);
            }
        }

        // Toggle all Marked cells at once
        if (cellsMarkedForToggle.Count > 0)
        {
            foreach (int _index in cellsMarkedForToggle)
            {
                ToggleCell(_index);
            }
        }

        PrintDracoGrid();
    }

    /// <summary> After all three stages, determine what the Defuser must input to solve the module </summary>
    void DetermineFinalRequiredSubmission()
    {
        lineToSubmit = "";
        bool _isThirdEven = (CharToInt(bombInfo.GetSerialNumber()[2]) % 2) == 0;

        // +7 to transform 0 ports to index 7 (which visually is 8)
        int _portCount = (bombInfo.GetPortCount() + 7) % 8;

        if (_isThirdEven)
        {
            // Submit Column
            // Simply grab the row, remove Voids, and replace colors by white
            for (int i = _portCount; i < 64; i += 8)
            {
                switch (dracoGrid[i])
                {
                    case 'w': lineToSubmit += "w"; break;

                    case 'k': lineToSubmit += "k"; break;

                    case 'C': lineToSubmit += "w"; break;

                    case 'M': lineToSubmit += "w"; break;

                    case 'Y': lineToSubmit += "w"; break;

                    default: break;
                }
            }
        }
        else
        {
            // Submit Row
            // Simply grab the row, remove Voids, and replace colors by white
            for (int i = _portCount * 8; i < (_portCount + 1) * 8 ; i++)
            {
                switch (dracoGrid[i])
                {
                    case 'w': lineToSubmit += "w"; break;

                    case 'k': lineToSubmit += "k"; break;

                    case 'C': lineToSubmit += "w"; break;

                    case 'M': lineToSubmit += "w"; break;

                    case 'Y': lineToSubmit += "w"; break;

                    default: break;
                }
            }
        }

        summoningModule.ModuleLog(moduleId, "Line to submit is {0} with zero-index {1} (ignoring Voids): {2}", _isThirdEven ? "column" : "row", _portCount, lineToSubmit);
    }

    /// <summary> Helper Method to quickly print the entire Draco Grid to log </summary>
    void PrintDracoGrid()
    {
        string _dracoGridToPrint = "";
        for (int i = 0; i < 8; i ++)
        {
            if (i != 0)
            {
                _dracoGridToPrint += "\n";
            }
            _dracoGridToPrint += dracoGrid.Substring(8 * i, 8);
        }

        summoningModule.ModuleLog(moduleId, _dracoGridToPrint);
    }

    /// <summary> Determine if a Cell index should be considered White. Allows to filter for or against Colored cells too. </summary> 
    bool IsCellWhite(int cellIndex, bool allowColoredAsWhite)
    {
        switch (dracoGrid[cellIndex]) 
        {
            case 'w': return true;
            case 'k': return false;
            case 'V':  return false;
            case 'C': if (allowColoredAsWhite) { return true; } return false;
            case 'M': if (allowColoredAsWhite) { return true; } return false;
            case 'Y': if (allowColoredAsWhite) { return true; } return false;
            default: return false;
        }
    }
    /// <summary> Determine if a Cell index is either Black or White, as Voids & Colored can't be toggled </summary>
    bool IsCellToggleable(int cellIndex)
    {
        switch (dracoGrid[cellIndex])
        {
            case 'w': return true;
            case 'k': return true;
            case 'V': return false;
            case 'C': return false;
            case 'M': return false;
            case 'Y': return false;
            default:  return false;
        }
    }

    /// <summary> Loop through Neighboring cells and count how many are White, using Void too. Input 4 for the four cardinal direction, or 8 for all neighbors.</summary>
    /// <param name="movementsToDo">Input 4 for the four cardinal direction, or 8 for all neighbors.</param>
    int VerifyNeighboringNumberOfWhites(int cellIndex, int movementsToDo)
    {
        int _totalWhiteCells = 0;
        var _cells = ReturnNeighborCells(cellIndex, movementsToDo);

        foreach (var cell in _cells)
        {
            // Can return -1 if it goes off the edge
            if (cell == -1) { continue; }

            _totalWhiteCells += IsCellWhite(cell, true) ? 1 : 0;
        }

        return _totalWhiteCells;
    }

    /// <summary> Loop through Neighboring cells and return the neighbors, using Void too. Input 4 for the four cardinal direction, or 8 for all neighbors. Might return -1 as padding.</summary>
    /// <param name="movementsToDo">Input 4 for the four cardinal direction, or 8 for all neighbors.</param>
    int[] ReturnNeighborCells(int cellIndex, int movementsToDo)
    {
        int[] _neighborIndices = new int[movementsToDo];
        bool _stillInTable;

        /*
            Algorithm is as follows:  
            > Check if moving in [Direction] would bring us outside of the table
                > If Not
                    > Move the scratchLocationPointer there for analysis
                    > Is the new cell we land on Void?
                        > If No, check if White and increment if needed
                        > If Yes, loop back to the "Check if moving would being us outside of the table"
                    
        */

        // Do it once for each direction, using PlateBase.MovementDirection's order
        for (int i = 0; i < movementsToDo; i++)
        {
            // Initialize values
            _stillInTable = true;
            scratchLocationPointer = cellIndex;

            // Loop as long as we are in the table (and encounter Void)
            while (_stillInTable)
            {
                // Make sure moving will keep us in the table
                if (DoesMovementStayInGrid((MovementDirection)i, scratchLocationPointer))
                {
                    // Move Pointer to the cell to analyze
                    scratchLocationPointer += GetMovementIndexOffsetFromDirection((MovementDirection)i);

                    // Is the new cell void?
                    if (dracoGrid[scratchLocationPointer] == 'V')
                    {
                        // Loop back to the while() loop after movement
                        continue;
                    }
                    // Else we don't need to move anymore: Record it
                    else
                    {
                        _neighborIndices[i] = scratchLocationPointer;
                        _stillInTable = false;
                        break;
                    }
                }

                // If we leave the table, just break and record -1
                _neighborIndices[i] = -1;
                _stillInTable = false;
                break;
            }
        }

        return _neighborIndices;
    }

    /// <summary> Verifies Neightbooring Number of White Cells all around, and check GoL rules </summary>
    bool DoesCellNeedToggleFromGameOfLifeRuleset(int cellIndex)
    {
        int _whites = VerifyNeighboringNumberOfWhites(cellIndex, 8);

        // Now we know the number of white cells around the target
        // Only possibilities are w or k
        if (dracoGrid[cellIndex] == 'w')
        {
            // Toggle a white cell if white neighbors != 2 or 3
            return !(_whites == 2 || _whites == 3);
        }
        else
        {
            // Toggle a black cell if white neightboors == 3
            return _whites == 3;
        }
    }

    /// <summary> Returns true if the movement in this direction is valid </summary>
    bool DoesMovementStayInGrid(MovementDirection direction, int cellIndex)
    {
        switch (direction)
        {
            case MovementDirection.Up: return GetRowFromCellIndex(cellIndex, 8) != 0;
            case MovementDirection.Down: return GetRowFromCellIndex(cellIndex, 8) != 7;
            case MovementDirection.Left: return GetColumnFromCellIndex(cellIndex, 8) != 0;
            case MovementDirection.Right: return GetColumnFromCellIndex(cellIndex, 8) != 7;
            case MovementDirection.UpLeft: return GetRowFromCellIndex(cellIndex, 8) != 0 && GetColumnFromCellIndex(cellIndex, 8) != 0;
            case MovementDirection.UpRight: return GetRowFromCellIndex(cellIndex, 8) != 0 && GetColumnFromCellIndex(cellIndex, 8) != 7;
            case MovementDirection.DownLeft: return GetRowFromCellIndex(cellIndex, 8) != 7 && GetColumnFromCellIndex(cellIndex, 8) != 0;
            case MovementDirection.DownRight: return GetRowFromCellIndex(cellIndex, 8) != 7 && GetColumnFromCellIndex(cellIndex, 8) != 7;
            default: return false;
        }
    }

    int GetMovementIndexOffsetFromDirection(MovementDirection direction)
    {
        switch (direction)
        {
            case MovementDirection.Up: return -8;
            case MovementDirection.Down:  return 8;
            case MovementDirection.Left:  return -1;
            case MovementDirection.Right: return 1;
            case MovementDirection.UpLeft: return -9;
            case MovementDirection.UpRight: return -7;
            case MovementDirection.DownLeft: return 7;
            case MovementDirection.DownRight: return 9;
            default: return 0;
        }
    }

    /// <summary> Flip a cell from Black to White and inversely </summary>
    void ToggleCell(int cellIndex, bool bShouldDebug = false)
    {
        if (bShouldDebug) { summoningModule.ModuleLog(moduleId, "Toggling cell index {0} // Coordinate {1}", cellIndex, GetCoordinateFromCellIndex(cellIndex, 8)); }

        // Can only toggle pure Black or Whites
        if (dracoGrid[cellIndex] == 'k')
        {
            dracoGrid = dracoGrid.Remove(cellIndex, 1).Insert(cellIndex, "w");
        }
        else if (dracoGrid[cellIndex] == 'w')
        {
            dracoGrid = dracoGrid.Remove(cellIndex, 1).Insert(cellIndex, "k");
        }
    }

    /// <summary> Manhattan Distance is affected by Voided cells.
    /// So the quickest way to determine the Manhattan Distance is to do a Distance Field algorithm starting from the Yellow Cell. </summary>
    void GenerateYellowManhattanDistanceField()
    {

        // Create an empty string composed of 64 . cells
        yellowDistanceField = "................................................................";

        // Place Yellow as 0
        ReplaceDistanceFieldCell(yellowCellIndex, "0");

        bool _stillInTable;

        // Each loop looks through the grid for cells with value of _valueToCheck
        // Since the maximum value for the max opposite corner is 12 (since Yellow can't be on an edge), loop until we analyze values 11
        for (int _valueToCheck = 0; _valueToCheck < 12; _valueToCheck ++)
        {
            // Check the entire table
            for (int _cellIndex = 0; _cellIndex < 64; _cellIndex ++)
            {
                // Only do something for cells that have the correct value
                if (yellowDistanceField[_cellIndex] == hexadecimal[_valueToCheck][0])
                {
                    // For all 4 orthogonal movement directions
                    for (int _movementDir = 0; _movementDir < 4; _movementDir++)
                    {

                        
                        // Use same code as Neighbor Checking, just setting the AStar value 
                        _stillInTable = true;
                        scratchLocationPointer = _cellIndex;

                        while (_stillInTable)
                        {
                            if (DoesMovementStayInGrid((MovementDirection)_movementDir, scratchLocationPointer))
                            {

                                scratchLocationPointer += GetMovementIndexOffsetFromDirection((MovementDirection)_movementDir);

                                // If void, loop but still set the value!
                                if (dracoGrid[scratchLocationPointer] == 'V')
                                {
                                    if (yellowDistanceField[scratchLocationPointer] == '.')
                                    { ReplaceDistanceFieldCell(scratchLocationPointer, hexadecimal[_valueToCheck + 1]); }
                                    continue;
                                }
                                // If not void, stop looping but still set the value
                                else if (yellowDistanceField[scratchLocationPointer] == '.')
                                {
                                    ReplaceDistanceFieldCell(scratchLocationPointer, hexadecimal[_valueToCheck + 1]);
                                    _stillInTable = false;
                                    break;
                                }
                                else
                                {
                                    _stillInTable = false;
                                    break;
                                }
                            }
                            _stillInTable = false;
                            break;
                        }

                    }
                }
            }

        }

        summoningModule.ModuleLog(moduleId, "Debug Yellow Distance Field: {0}", yellowDistanceField);
    }

    /// <summary> Helper Function to create the Distance Field: Just sets a cell with a given value. </summary>
    void ReplaceDistanceFieldCell(int cellIndex, string characterToPlace)
    {
        yellowDistanceField = yellowDistanceField.Remove(cellIndex, 1).Insert(cellIndex, characterToPlace);
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length != 2)
        {
            yield return "sendtochat {0} you must format the submission with “!{1} Submit wkkwkwwk” with exactly 2 arguments.";
            yield break;
        }

        if (!commandParts[0].Equals("submit") && !commandParts[0].Equals("s"))
        {
            yield return "sendtochat {0} you must format the submission with “!{1} Submit wkkwkwwk”, starting with the word “Submit”.";
            yield break;
        }

        string _submittedSequence = commandParts[1];

        for (int i = 0; i < _submittedSequence.Length; i++)
        {
            yield return new WaitForSeconds(0.15f);

            switch (_submittedSequence[i])
            {
                case 'w':
                    platePressableButtons[1].OnInteract();
                    break;

                case 'k':
                    platePressableButtons[0].OnInteract();
                    break;

                default:
                    yield return "sendtochat {0} Unknown character “" + _submittedSequence[i] + "”. Stopping sequence input.";
                    yield break;
            }
        }
    }

    public override IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < lineToSubmit.Length; i++)
        {
            yield return new WaitForSeconds(0.15f);

            switch (lineToSubmit[i])
            {
                case 'w':
                    platePressableButtons[1].OnInteract();
                    break;

                case 'k':
                    platePressableButtons[0].OnInteract();
                    break;

                default:
                    break;
            }
        }
    }

    protected override void CasingTextButtonGetsPressed() { }
}


// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
//    CODE GRAVEYARD
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

// Previews iterations of Draco Plate had different Cyan, Magenta and Yellow Rules which were much more extreme
// To make the module shorter and more enjoyable, they have been reworked, and the code moved here in case it is needed

/*

void DetermineColoredStepsOrder()
{
    int _lastDigit = bombInfo.GetSerialNumberNumbers().Last() % 3;

    switch (_lastDigit) { 
        case 0:
            coloredStepsOrder = "CMY";
            summoningModule.ModuleLog(moduleId, "Applying rules Cyan, then Magenta, then Yellow.");
            break;
        case 1:
            coloredStepsOrder = "MYC";
            summoningModule.ModuleLog(moduleId, "Applying rules Magenta, then Yellow, then Cyan.");
            break;
        default:
            coloredStepsOrder = "YCM";
            summoningModule.ModuleLog(moduleId, "Applying rules Yellow, then Cyan, then Magenta.");
            break;
    }
} 


// =-= In Initialize Start
{
    // Apply the Colored Steps
    for (int i = 0; i < 3; i++)
    {
        switch (coloredStepsOrder[i])
        {
            case 'C':
                ApplyCyanCellRules();
                break;
            case 'M':
                ApplyMagentaCellRules();
                break;
            case 'Y':
                ApplyYellowCellRules();
                break;
        }
    }
}



// =-= In Cyan Rules =-=
{
    // Then do the same for diagonals
    cellsMarkedForToggle.Clear();
    _cellsToVerifyGameOfLifeRulesOn.Clear();


    // Register and Toggle
    // Check diagonal directions until edge is touched
    bool topLeftBlocked = false, topRightBlocked = false, bottomLeftBlocked = false, bottomRightBlocked = false;

    // Just do steps offset from Cyan. Cyan cannot be on the edges so it's always safe to move 1 outwards
    for (int _offsetFromCyan = 1; _offsetFromCyan < 8; _offsetFromCyan ++)
    {
        if (!topLeftBlocked)
        {
            // Set the ScratchPointer
            scratchLocationPointer = cyanCellIndex - (_offsetFromCyan * 9);
            // Maybe block next iteration
            topLeftBlocked = !DoesMovementStayInGrid(MovementDirection.UpLeft, scratchLocationPointer);
            // Mark for GameOfLife if needed
            if (IsCellToggleable(scratchLocationPointer)) 
            {
                _cellsToVerifyGameOfLifeRulesOn.Add(scratchLocationPointer);
                ToggleCell(scratchLocationPointer);
            }
        }
        if (!topRightBlocked)
        {
            scratchLocationPointer = cyanCellIndex - (_offsetFromCyan * 7);
            topRightBlocked = !DoesMovementStayInGrid(MovementDirection.UpRight, scratchLocationPointer);
            if (IsCellToggleable(scratchLocationPointer)) 
            {
                _cellsToVerifyGameOfLifeRulesOn.Add(scratchLocationPointer);
                ToggleCell(scratchLocationPointer);
            }
        }
        if (!bottomLeftBlocked)
        {
            scratchLocationPointer = cyanCellIndex + (_offsetFromCyan * 7);
            bottomLeftBlocked = !DoesMovementStayInGrid(MovementDirection.DownLeft, scratchLocationPointer);
            if (IsCellToggleable(scratchLocationPointer)) 
            {
                _cellsToVerifyGameOfLifeRulesOn.Add(scratchLocationPointer); 
                ToggleCell(scratchLocationPointer);
            }
        }
        if (!bottomRightBlocked)
        {
            scratchLocationPointer = cyanCellIndex + (_offsetFromCyan * 9);
            bottomRightBlocked = !DoesMovementStayInGrid(MovementDirection.DownRight, scratchLocationPointer);
            if (IsCellToggleable(scratchLocationPointer)) 
            {
                _cellsToVerifyGameOfLifeRulesOn.Add(scratchLocationPointer); 
                ToggleCell(scratchLocationPointer);
            }
        }
    }


    // THEN Apply GoL rules (after all toggles)
    foreach (int _cellToCheck in _cellsToVerifyGameOfLifeRulesOn)
    {
        if (DoesCellNeedToggleFromGameOfLifeRuleset(_cellToCheck))
        {
            cellsMarkedForToggle.Add(_cellToCheck);
        }
    }


    // Toggle all Marked cells at once
    if (cellsMarkedForToggle.Count > 0)
    {
        foreach (int _index in cellsMarkedForToggle)
        {
            ToggleCell(_index);
        }
    }


    summoningModule.ModuleLog(moduleId, "After affecting the diagonals:");
    PrintDracoGrid();
}



// =-= In Magenta Rules =-=

{
    // Check every cell for neighbors. Even corners can have them!
    for (int _cellIndex = 0; _cellIndex < 64; _cellIndex++)
    {
        // Is the cell even valid to check? (not Void nor Colored)
        if (IsCellToggleable(_cellIndex))
        {
            // Are there also the same # of White neighbors
            if (VerifyNeighboringNumberOfWhites(_cellIndex, 4) == _targetNumberOfWhites)
            {
                // Mark for toggle that one cell
                cellsMarkedForToggle.Add(_cellIndex);


                // Go to the other side, and try to mark for add the four other cells
                // Starting from this scratch location Pointer, for each direction (0-3)
                // Move until you find a non-Void cell, and mark for Toggle if toggleable
                for (int _movementDirection = 0; _movementDirection < 4; _movementDirection ++)
                {
                    // Use same code as Neighbor Checking, just marking for toggle instead of counting whites
                    _stillInTable = true;

                    // This is the index of the "180° Rotationally" cell
                    scratchLocationPointer = 63 - _cellIndex;

                    while (_stillInTable)
                    {
                        if (DoesMovementStayInGrid((MovementDirection)_movementDirection, scratchLocationPointer))
                        {
                            scratchLocationPointer += GetMovementIndexOffsetFromDirection((MovementDirection)_movementDirection);
                            if (dracoGrid[scratchLocationPointer] == 'V')
                            {
                                continue;
                            }
                            else if (IsCellToggleable(scratchLocationPointer))
                            {
                                cellsMarkedForToggle.Add(scratchLocationPointer);
                                _stillInTable = false;
                                break;
                            }
                            else
                            {
                                _stillInTable = false;
                                break;
                            }
                        }
                        _stillInTable = false;
                        break;
                    }
                }
            }
        }
    }
}


*/