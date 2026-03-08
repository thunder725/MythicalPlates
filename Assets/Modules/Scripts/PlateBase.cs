using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementDirection { Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight};


/// <summary>
/// Base class for the summonable Plates that contain the game logic
/// </summary>
public abstract class PlateBase : MonoBehaviour {

    // =-=-= Universal Plate Data =-=-=
    // Information that the Module knows by itself
    public KMSelectable[] platePressableButtons;
    protected List<int> voidedCellsIndices;
    [SerializeField] protected string customTwitchHelpMessage = "“!{0}”";
    public string fullPlateName;
    [SerializeField] protected AudioClip platePressedSound;


    // Information that gets transmitted by the Summoning Module
    [HideInInspector] public KMBombModule thisModule;
    [HideInInspector] public KMBombInfo bombInfo;
    [HideInInspector] public SummoningModule summoningModule;
    [HideInInspector] public KMSelectable casingPressableButton;


    // Visual Data
    float plateIdleRotationFrequency = 0.2f;
    float plateIdleRotationIntensity = 1.0f;
    float startingPlateIdleRotationOffset;

    // Universal Logging Data
    [HideInInspector] public int moduleId;

    /// <summary> Structure to return data about Movements around grids </summary>
    protected struct VoidMovementData
    {
        /// <summary> Represents how many Void Cells were passed through during this movement. </summary>
        public int NumberOfPassedVoidCells;
        /// <summary> Indicates if the movement ran into the edges of the grid. Will be true whether looping is enabled or not. </summary>
        public bool ranIntoGridEdges;
    }

    // Helper Data
    protected string[] alphabet = new string[26] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    protected string[] hexadecimal = new string[16] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E", "F"};
    protected string[] morseCodeAlphabet = new string[26] { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--.."};
    protected string[] morseCodeNumbers = new string[10] { "-----", ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----." };
    protected string[] numbersAsBinary = new string[32] { "00000", "00001", "00010", "00011", "00100", "00101", "00110", "00111", "01000", "01001", "01010", "01011", "01100", "01101", "01110", "01111", "10000", "10001", "10010", "10011", "10100", "10101", "10110", "10111", "11000", "11001", "11010", "11011", "11100", "11101", "11110", "11111" };


    /// <summary> Called by the Module after Awake() is called. Used for component gathering but not logic. </summary>
    public virtual void InitializeModuleAwake()
    {
        if (platePressableButtons != null)
        {
            summoningModule.ReceivePlateButtons(platePressableButtons);
        }

        if (casingPressableButton != null)
        {
            casingPressableButton.OnInteract += delegate () { CasingTextButtonGetsPressed(); return false; };
        }

        summoningModule.ReceiveTwitchHelpMessage(customTwitchHelpMessage);
    }
    /// <summary> Called by the Module after Start() is called. Used for puzzle initialization. </summary>
    public virtual void InitializeModuleStart()
    {
        summoningModule.ModuleLog(moduleId, "Initializing Module");
        voidedCellsIndices = new List<int>();
        startingPlateIdleRotationOffset = UnityEngine.Random.Range(0f, 10f);
    }
    /// <summary> Called by the Module every tick. </summary>
    public virtual void UpdateModule()
    {
        UpdatePlateIdleRotation();
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Global Table Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    /// <summary> Method so that individual plates can do something when the Casing Text Button gets pressed. Usually resetting submissions. </summary>
    protected abstract void CasingTextButtonGetsPressed();


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Visual Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void UpdatePlateIdleRotation()
    {
        float scaledTime = UnityEngine.Time.time * plateIdleRotationFrequency + startingPlateIdleRotationOffset;

        float sin1 = Mathf.Sin(scaledTime);
        float sin2 = Mathf.Sin(scaledTime - 1);
        float cos1 = Mathf.Cos(scaledTime + 2);
        float cos2 = Mathf.Cos(scaledTime + 1);

        transform.localEulerAngles = new Vector3(sin1 * cos2 - cos1, cos1 + cos2, sin1 * cos1 + sin2) * plateIdleRotationIntensity;
    }




    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Global Table Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    
    /// <summary> Gets a Row (topmost = 0) from a cell index and the number of elements in a row of the grid </summary>
    protected int GetRowFromCellIndex(int cellIndex, int rowSize) { return cellIndex / rowSize; }
    /// <summary> Gets a Column (leftmost = 0) from a cell index and the number of elements in a row of the grid </summary>
    protected int GetColumnFromCellIndex(int cellIndex, int rowSize) { return cellIndex % rowSize; }
    /// <summary> Gets a coordinate (format A1) from a cell index and the number of elements in a row of the grid </summary>
    protected string GetCoordinateFromCellIndex(int cellIndex, int rowSize)
    {
        string _coordinate = "";
        _coordinate += alphabet[GetColumnFromCellIndex(cellIndex, rowSize)];
        _coordinate += (GetRowFromCellIndex(cellIndex, rowSize) + 1).ToString();

        return _coordinate;
    }

    /// <summary> Move around a grid while taking into account Void cells. All 8 directions are supported for movements. 
    /// Returns the number of Voided Cells crossed. </summary>
    protected VoidMovementData MoveAroundGridWithVoid(MovementDirection movementDirection, Array grid, ref int currentIndexInGrid, int rowSize, bool shouldLoopAround)
    {
        VoidMovementData _voidMovementData = new VoidMovementData();
        int _whileFailsafe = 0;
        bool searchingForAnswer = true;
        while (searchingForAnswer)
        {
            _whileFailsafe++;
            if (_whileFailsafe > 25)
            {
                summoningModule.ModuleLog(moduleId, "Reached 25 iterations trying to move around... Send Help!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                summoningModule.ModuleLog(moduleId, "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                return _voidMovementData;
            }

            // Move (if allowed)
            switch (movementDirection)
            {
                case MovementDirection.Up:
                    // Verify Looping: We aren't at the topmost row
                    if (currentIndexInGrid >= rowSize)
                    {
                        currentIndexInGrid -= rowSize;
                    }
                    // Else, if we can, loop around to the bottom row
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += grid.Length - rowSize;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    // Else we are at the top and can't loop around, so don't move
                    break;

                case MovementDirection.Down:
                    // Verify Looping: We aren't at the bottommost row
                    if (currentIndexInGrid + rowSize < grid.Length)
                    {
                        currentIndexInGrid += rowSize;
                    }
                    // Else, if we can, loop around to the top row
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - grid.Length;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    // Else we are at the bottom and can't loop around, so don't move
                    break;

                case MovementDirection.Left:
                    // Verify Looping: We aren't at the leftmost column
                    if (GetColumnFromCellIndex(currentIndexInGrid, rowSize) != 0)
                    {
                        currentIndexInGrid -= 1;
                    }
                    // Else, if we can, loop around to the right column
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - 1;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    // Else we are at the left and can't loop around, so don't move
                    break;

                case MovementDirection.Right:
                    // Verify Looping: We aren't at the rightmost column
                    if (GetColumnFromCellIndex(currentIndexInGrid, rowSize) != rowSize - 1)
                    {
                        currentIndexInGrid += 1;
                    }
                    // Else, if we can, loop around to the left column
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += 1 - rowSize;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    // Else we are at the right and can't loop around, so don't move
                    break;

                case MovementDirection.UpLeft:

                    // Copy of Up and Left sequences

                    if (currentIndexInGrid >= rowSize)
                    {
                        currentIndexInGrid -= rowSize;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += grid.Length - rowSize;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    if (GetColumnFromCellIndex(currentIndexInGrid, rowSize) != 0)
                    {
                        currentIndexInGrid -= 1;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - 1;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    break;

                case MovementDirection.UpRight:

                    // Copy of Up and Right sequences

                    if (currentIndexInGrid >= rowSize)
                    {
                        currentIndexInGrid -= rowSize;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += grid.Length - rowSize;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    if (GetColumnFromCellIndex(currentIndexInGrid, rowSize) != rowSize - 1)
                    {
                        currentIndexInGrid += 1;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += 1 - rowSize;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    break;

                case MovementDirection.DownLeft:

                    // Copy of Down and Left sequences

                    if (currentIndexInGrid + rowSize < grid.Length)
                    {
                        currentIndexInGrid += rowSize;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - grid.Length;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    if (GetColumnFromCellIndex(currentIndexInGrid, rowSize) != 0)
                    {
                        currentIndexInGrid -= 1;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - 1;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    break;

                case MovementDirection.DownRight:

                    // Copy of Down and Right sequences

                    if (currentIndexInGrid + rowSize < grid.Length)
                    {
                        currentIndexInGrid += rowSize;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - grid.Length;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }

                    if (GetColumnFromCellIndex(currentIndexInGrid, rowSize) != rowSize - 1)
                    {
                        currentIndexInGrid += 1;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += 1 - rowSize;
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    else
                    {
                        _voidMovementData.ranIntoGridEdges = true;
                    }
                    break;
            }


            // If we are NOT on a Voided cell, keep going!
            if (!voidedCellsIndices.Contains(currentIndexInGrid))
            {
                searchingForAnswer = false;
            }
            else // Else we ARE on a Voided cell, keep going but increment!
            {
                _voidMovementData.NumberOfPassedVoidCells ++;
            }
        }

        return _voidMovementData;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Global Math Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Digital Root code from FaultyDigitalRoot's module, credit to eXish
    protected int DigitalRoot(int number)
    {
        string _stringResult = "" + number;
        int _result;
        int i;

        while (_stringResult.Length > 1)
        {
            _result = 0;
            for (i = 0; i < _stringResult.Length; i++)
            {
                _result += int.Parse(_stringResult.Substring(i, 1));
            }

            _stringResult = "" + _result;
        }

        return int.Parse(_stringResult);
    }

    /// <summary> Converts a character into an Int. For String, try int.Parse(string) </summary>
    protected int CharToInt(char _char)
    {
        // Converting a char to an int apparently requires you to subtract a char that happens to represent a number
        // Not add, specifically subtract
        return _char - '0';
    }




    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Per-Plate implementation of the Twitch Plays command </summary>
    public abstract IEnumerator ProcessTwitchCommand(string command);

    /// <summary> Per-Plate implementation of the Twitch Plays Force Solve </summary>
    public abstract IEnumerator TwitchHandleForcedSolve();
}
