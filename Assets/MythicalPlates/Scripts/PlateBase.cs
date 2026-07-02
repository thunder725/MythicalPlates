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
    [SerializeField] protected AudioClip[] platePressedSounds;


    // Information that gets transmitted by the Summoning Module
    [HideInInspector] public KMBombModule thisModule;
    [HideInInspector] public KMBombInfo bombInfo;
    [HideInInspector] public SummoningModule summoningModule;
    [HideInInspector] public KMSelectable casingPressableButton;

    KMSelectable.OnInteractHandler casingButtonPressedDelegate;

    // Visual Data
    float plateIdleRotationFrequency = 0.25f;
    float plateIdleRotationIntensity = 1.2f;
    float startingPlateIdleRotationOffset;

    /// <summary> For child classes to add a shake or rotation on top of the idle one </summary>
    protected Vector3 additivePlateRotation = Vector3.zero;

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
            // Save delegate for the CasingButton being pressed to remove it later
            casingButtonPressedDelegate = casingPressableButton.OnInteract += delegate () { CasingTextButtonGetsPressed(); return false; };
        }

        

        summoningModule.ReceiveTwitchHelpMessage(customTwitchHelpMessage);
    }
    /// <summary> Called by the Module after Start() is called. Used for puzzle initialization. </summary>
    public virtual void InitializeModuleStart()
    {
        voidedCellsIndices = new List<int>();
        startingPlateIdleRotationOffset = UnityEngine.Random.Range(0f, 10f);
    }
    /// <summary> Called by the Module every tick. </summary>
    public virtual void UpdateModule()
    {
        UpdatePlateIdleRotation();
    }

    // Remove the Delegates
    // Specifically useful for AllmightySinnoh to avoid phantom code and logs
    public void RemoveDelegates()
    {
        if (casingPressableButton != null)
        {
            casingPressableButton.OnInteract -= casingButtonPressedDelegate;
        }
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Global Table Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    /// <summary> Method so that individual plates can do something when the Casing Text Button gets pressed. Usually resetting submissions. </summary>
    protected abstract void CasingTextButtonGetsPressed();


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Visual & Sound Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void UpdatePlateIdleRotation()
    {
        float scaledTime = UnityEngine.Time.time * plateIdleRotationFrequency + startingPlateIdleRotationOffset;

        float sin1 = Mathf.Sin(scaledTime);
        float sin2 = Mathf.Sin(scaledTime - 1);
        float cos1 = Mathf.Cos(scaledTime + 2);
        float cos2 = Mathf.Cos(scaledTime + 1);

        transform.localEulerAngles = additivePlateRotation + new Vector3(sin1 * cos2 - cos1, cos1 + cos2, sin1 * cos1 + sin2) * plateIdleRotationIntensity;
    }

    public void PlayPlatePressSound()
    {
        summoningModule.PlaySound(platePressedSounds.PickRandom());
    }

    protected IEnumerator VibratePlate(float intensity)
    {
        float timer = 0;

        while (timer < 0.4f)
        {
            yield return null;
            timer += Time.deltaTime;

            additivePlateRotation = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)) * intensity;
        }

        additivePlateRotation = Vector3.zero;
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
    protected VoidMovementData MoveAroundGridWithVoid(MovementDirection movementDirection, int gridLength, ref int currentIndexInGrid, int rowSize, bool shouldLoopAround)
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
                        currentIndexInGrid += gridLength - rowSize;
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
                    if (currentIndexInGrid + rowSize < gridLength)
                    {
                        currentIndexInGrid += rowSize;
                    }
                    // Else, if we can, loop around to the top row
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - gridLength;
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
                        currentIndexInGrid += gridLength - rowSize;
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
                        currentIndexInGrid += gridLength - rowSize;
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

                    if (currentIndexInGrid + rowSize < gridLength)
                    {
                        currentIndexInGrid += rowSize;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - gridLength;
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

                    if (currentIndexInGrid + rowSize < gridLength)
                    {
                        currentIndexInGrid += rowSize;
                    }
                    else if (shouldLoopAround)
                    {
                        currentIndexInGrid += rowSize - gridLength;
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


            // If we ran into the Grid Edges but we can't loop, just return immediately
            if (_voidMovementData.ranIntoGridEdges)
            {
                if (shouldLoopAround == false)
                {
                    return _voidMovementData;
                }
            }
        }

        return _voidMovementData;
    }

    protected MovementDirection GetOppositeMovementDirection(MovementDirection inputMovementDirection)
    {
        switch (inputMovementDirection)
        {
            case MovementDirection.Up:
                return MovementDirection.Down;

            case MovementDirection.Down:
                return MovementDirection.Up;

            case MovementDirection.Left:
                return MovementDirection.Right;

            case MovementDirection.Right:
                return MovementDirection.Left;

            case MovementDirection.UpLeft:
                return MovementDirection.DownRight;

            case MovementDirection.UpRight:
                return MovementDirection.DownLeft;

            case MovementDirection.DownLeft:
                return MovementDirection.UpRight;

            case MovementDirection.DownRight:
                return MovementDirection.UpLeft;
        }

        return MovementDirection.Up;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Global Math Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Digital Root is apparently exactly equivalent to taking the %9 of the number!!
    // But be careful to return a 9 if you get a 0 (18 digital root is 9, not 0)
    protected int DigitalRoot(int number)
    {
        int _result = number % 9;
        return _result == 0 ? 9 : _result;
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
