using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


public class PixiePlate : PlateBase {

    /// <summary> Dictionary to convert a 5-digit binary into one of the 32 characters that can be displayed on the Plate </summary>
    readonly Dictionary<string, string> ConversionTableBelow = new Dictionary<string, string>()
    { {"00000", "A"}, {"00001", "B"}, {"00010", "C"}, {"00011", "D"}, {"00100", "E"}, {"00101", "F"}, {"00110", "G"}, {"00111", "H"},
      {"01000", "I"}, {"01001", "J"}, {"01010", "K"}, {"01011", "L"}, {"01100", "M"}, {"01101", "N"}, {"01110", "O"}, {"01111", "P"},
      {"10000", "Q"}, {"10001", "R"}, {"10010", "S"}, {"10011", "T"}, {"10100", "U"}, {"10101", "V"}, {"10110", "W"}, {"10111", "X"},
      {"11000", "Y"}, {"11001", "Z"}, {"11010", "+"}, {"11011", "/"}, {"11100", "?"}, {"11101", "&"}, {"11110", "#"}, {"11111", "%"}};


    /// <summary> Structure to represent the starting states of Preset Playfield Puzzles </summary>
    [System.Serializable] public struct PresetPlayfieldPuzzle
    {
        /// <summary> Unique ID of the starting Playfield, for debugging purposes </summary>
        public int puzzleId;
        /// <summary> Array of ID of each Demon, from 0 to 8, in order. </summary>
        public int[] demonIds;
        /// <summary> Starting Location of each Demon, from 00 to 73, in order. </summary>
        public string[] demonLocations;
        /// <summary> Array of ID of each Pixie, from 0 to 9, in order. </summary>
        public int[] pixieIds;
        /// <summary> Location of all Void Cells, from 00 to 73, in order. </summary>
        public string[] voidCellLocations;
        /// <summary> Intended Solution, placement of each Pixie in order from 00 to 73 that should 100% solve the module. </summary>
        public string[] intendedPixiePlacementSolution;
    }
    
    /// <summary> Structure to represent the current Playfield, with all Pixies and Demons </summary>
    [System.Serializable] public struct Playfield
    {
        /// <summary> Array of all Demons in the Playfield. </summary>
        public Demon[] demons;
        /// <summary> Array of all Pixies in the Playfield. </summary>
        public Pixie[] pixies;
        /// <summary> Array reprensentation of the Field, to make it easier to debug and see at a glance where everybody is.
        /// 0 is empty, -1 is Void, and >0 represents a Creature.
        /// Each Creature's array representation goes from 1 for the first Demon, to (DemonCount + PixieCount) for the last Pixie.</summary>
        public int[] playfieldRepresentation;
        /// <summary> Boolean to check if a Vassal-Goh is present, so a Summon Check should be done </summary>
        public bool hasVassalGohPresent;
        /// <summary> Boolean to check if a Y-morGre is present, so Demon Damage Bonus Checks should be done </summary>
        public bool hasYmorGrePresent;
        /// <summary> Boolean to check if a Zagan the Trickster is present, so Pixies need to verify their lanes before attacking </summary>
        public List<int> rowsWithZaganTheTricksterPresent;
        /// <summary> List of where Vale-Forts are present, so Demons can get Health Bonuses </summary>
        public List<int> rowsWithValeFortPresent;
        /// <summary> Boolean to check if a Vashakapa is present, so a Pixie Damage Bonus Check should be done before attacks </summary>
        public bool hasVashakapaPresent;
    }

    /// <summary> Structure to represent a Demon's current data on the Playfield </summary>
    [System.Serializable] public struct Demon
    {
        /// <summary> ID for this Demon's Archetype. This is how Special Abilities are determined. </summary>
        public int demonArchetypeId;
        /// <summary> Unique Number associated with that Demon, to track it in the Playfield. </summary>
        public int demonPlayfieldNumber;
        public string debugFriendlyName;
        /// <summary> Location of the Demon, between 0 and 31 as this is just the index in the table. </summary>
        public int gridLocationIndex;
        public int currentHealth;
        public int currentDamage;
        /// <summary> Movement Speed is how many cells the Demon moves per Timestep. For 1 per 2, the written value is 0 </summary>
        public int movementSpeed;
        /// <summary> True if the Demon is allowed to move this timestep </summary>
        public bool canMove;
        /// <summary> True if the Demon is allowed to attack this timestep </summary>
        public bool canAttack;
        /// <summary> True if the Demon has received a Health Boost from a Vale-Fort Demon </summary>
        public bool hasReceivedHealthBoost;
        /// <summary> True if the Demon is under the influence of a Damage Boost from a Y-morGre Demon </summary>
        public bool hasReceivedDamageBoost;
    }

    /// <summary> Structure to represent a Pixie's current data on the Playfield </summary>
    [System.Serializable] public struct Pixie
    {
        /// <summary> ID for this Pixie's Archetype. This is how Special Abilities and Attack Ranges are determined. </summary>
        public int pixieArchetypeId;
        /// <summary> Unique Number associated with that Pixie, to track it in the Playfield. </summary>
        public int pixiePlayfieldNumber;
        public string debugFriendlyName;
        /// <summary> Location of the Pixie, between 0 and 31 as this is just the index in the table. </summary>
        public int gridLocationIndex;
        public int currentHealth;
        public int currentDamage;
        /// <summary> True if the Pixie has received a Damage Boost from a Vashakapa Pixie </summary>
        public bool hasReceivedDamageBoost;
        /// <summary> True if the Pixie has been frozen by Bifrovst this timestep and can't attack </summary>
        public bool hasBeenFrozenByBifrovst;
    }

    /// <summary> Current location of the Player on the Plate </summary>
    int currentPlayerPointerLocation;
    int currentPixieIndexToPlace;

    // Helper stat for ciphering data to the Defuser
    string fiveDigitBinarySumOfSerialNumber;


    // Plate Visualization Reference
    [SerializeField] TextMesh topPlateDemonInscription;
    [SerializeField] TextMesh bottomPlatePixieInscription;
    [SerializeField] TextMesh middlePlateVoidInscription;

    // Sounds for player feedback
    [SerializeField] AudioClip readyToSubmitSound;
    Coroutine warningVibrationCoroutine;

    // Preset Puzzles
    [SerializeField] TextAsset puzzleListJson;
    PresetPlayfieldPuzzle[] presetPuzzleLists;
    PresetPlayfieldPuzzle selectedPresetPuzzle;

    /// <summary> Current Timestep. 0 is before any movement. First actions start at Timestep 1 </summary>
    int currentTimestepIndex;
    Playfield currentPlayfield;
    bool isSimulatingPlay;


    /// <summary> If this boolean is true and a Demon reaches the left, don't give a Strike as this is just a test, not player input </summary>
    bool isInIntegrityVisualizationMode;

    // Universal Logging Data
    static int moduleIdCounter = 1;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake() 
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        // Parse the preset Playfield Puzzles
        presetPuzzleLists = JsonConvert.DeserializeObject<PresetPlayfieldPuzzle[]>(puzzleListJson.text);

        // Bind to Buttons on the Plate
        platePressableButtons[0].OnInteract += delegate () { PlateButtonsGetsPressed(0); return false; };
        platePressableButtons[1].OnInteract += delegate () { PlateButtonsGetsPressed(1); return false; };
        platePressableButtons[2].OnInteract += delegate () { PlateButtonsGetsPressed(2); return false; };
        platePressableButtons[3].OnInteract += delegate () { PlateButtonsGetsPressed(3); return false; };
        platePressableButtons[4].OnInteract += delegate () { PlateButtonsGetsPressed(4); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        // Debug Method to verify Preset Playfield Puzzles Integrity, and make sure all of them are in a valid state;
        // VerifyPresetPlayfieldPuzzleIntegrity();

        SelectPresetPlayfieldPuzzle();
        InitializePlayfieldFromPresetPuzzle(false, selectedPresetPuzzle);


        ComputeSumOfSerialNumbersIntoBinary();
        CompressAndShowDemonDataOnPlate();
        CompressAndShowPixieDataOnPlate();
        ShowVoidCellLocationsOnPlate();
        LogIntendedSolution();

        currentPixieIndexToPlace = 0;
        currentPlayerPointerLocation = 0;
        isInIntegrityVisualizationMode = false;
    }

    // public override void UpdateModule() { base.UpdateModule(); }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void PlateButtonsGetsPressed(int buttonTypeIndicator)
    {
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved) { return; }

        // Button Type Indicator:
        // 0123 is Up Down Left Right Movement to coincide with MovementDirection enum
        // 4 is Center (submit)


        if (buttonTypeIndicator == 4) // Submit Button pressed
        {
            PressedCentralButton();
        }
        else
        {
            PressedMovementButton((MovementDirection)buttonTypeIndicator);
        }
    }

    protected override void CasingTextButtonGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        summoningModule.ModuleLog(moduleId, "Resetting the Grid due to PIXIE Button pressed.");
        ResetGridAndPlayerInputs();
    }

    void PressedCentralButton()
    {
        // Submit a Pixie Placement
        if (currentPixieIndexToPlace < currentPlayfield.pixies.Length)
        {
            // Verify Pixie Placement is okay
            if (IsPlayfieldCellEmpty(currentPlayerPointerLocation))
            {
                // Set the Pixie in the Playfield
                currentPlayfield.playfieldRepresentation[currentPlayerPointerLocation] = currentPlayfield.pixies[currentPixieIndexToPlace].pixiePlayfieldNumber;

                // Give to the Pixie its own Location
                currentPlayfield.pixies[currentPixieIndexToPlace].gridLocationIndex = currentPlayerPointerLocation;

                summoningModule.ModuleLog(moduleId, "Placed {0}, Pixie with Grid Number {1}, at location {2}",
                    currentPlayfield.pixies[currentPixieIndexToPlace].debugFriendlyName,
                    currentPlayfield.pixies[currentPixieIndexToPlace].pixiePlayfieldNumber,
                    ConvertGridIndexToPresetPuzzleLocation(currentPlayfield.pixies[currentPixieIndexToPlace].gridLocationIndex));

                // Go to the next Pixie
                currentPixieIndexToPlace++;

                // If we just placed the last pixie
                if (currentPixieIndexToPlace ==  currentPlayfield.pixies.Length)
                {
                    summoningModule.PlaySound(readyToSubmitSound);
                    if (warningVibrationCoroutine != null) { StopCoroutine(warningVibrationCoroutine); }
                    warningVibrationCoroutine = StartCoroutine(VibratePlate(0.5f));
                }
            }
            else
            {
                summoningModule.ModuleLog(moduleId, "Can't place Pixie at location {0} because something's already there!",
                    ConvertGridIndexToPresetPuzzleLocation(currentPlayerPointerLocation));
                
                if (warningVibrationCoroutine != null) { StopCoroutine(warningVibrationCoroutine); }
                warningVibrationCoroutine = StartCoroutine(VibratePlate(4f));
            }
        }
        // Or sumbit everything and run the simulation
        else if (currentPixieIndexToPlace == selectedPresetPuzzle.pixieIds.Length)
        {
            summoningModule.ModuleLog(moduleId, "Pressed the center button: Simulating play.");
            SimulatePlay();
        }
        // Or that is a weird case to happen
        else
        {
            summoningModule.ModuleLog(moduleId, "Center Button pressed at an incorrect Index: {0}. Please contact thunder725 on Discord!", currentPixieIndexToPlace);
        }
    }

    void PressedMovementButton(MovementDirection movementDirection)
    {
        platePressableButtons[0].AddInteractionPunch(0.5f);
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }


        // Movement
        // We can't use MoveAroundGridWithVoid because we shouldn't use Voided Cells for moving the pointer cell
        switch (movementDirection)
        {
            case MovementDirection.Up:
                // Verify Edges: We aren't at the topmost row
                if (currentPlayerPointerLocation >= 8)
                {
                    currentPlayerPointerLocation -= 8;
                }
                else
                {
                    if (warningVibrationCoroutine != null) { StopCoroutine(warningVibrationCoroutine); }
                    warningVibrationCoroutine = StartCoroutine(VibratePlate(4f));
                }

                break;

            case MovementDirection.Down:
                // Verify Edges: We aren't at the bottommost row
                if (currentPlayerPointerLocation < 24)
                {
                    currentPlayerPointerLocation += 8;
                }
                else
                {
                    if (warningVibrationCoroutine != null) { StopCoroutine(warningVibrationCoroutine); }
                    warningVibrationCoroutine = StartCoroutine(VibratePlate(4f));
                }

                break;

            case MovementDirection.Left:
                // Verify Edges: We aren't at the leftmost column
                if (GetColumnFromCellIndex(currentPlayerPointerLocation, 8) != 0)
                {
                    currentPlayerPointerLocation -= 1;
                }
                else
                {
                    if (warningVibrationCoroutine != null) { StopCoroutine(warningVibrationCoroutine); }
                    warningVibrationCoroutine = StartCoroutine(VibratePlate(4f));
                }

                break;

            case MovementDirection.Right:
                // Verify Edges: We aren't at the rightmost column
                if (GetColumnFromCellIndex(currentPlayerPointerLocation, 8) != 7)
                {
                    currentPlayerPointerLocation += 1;
                }
                else
                {
                    if (warningVibrationCoroutine != null) { StopCoroutine(warningVibrationCoroutine); }
                    warningVibrationCoroutine = StartCoroutine(VibratePlate(4f));
                }

                break;
        }
    }

    void ResetGridAndPlayerInputs()
    {
        summoningModule.ModuleLog(moduleId, "Resetting the Playfield, all placed Pixies, and the current player location to 0-0");

        InitializePlayfieldFromPresetPuzzle(false, selectedPresetPuzzle);
        currentPixieIndexToPlace = 0;
        isSimulatingPlay = false;
        currentPlayerPointerLocation = 0;
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    JSON Reading Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void SelectPresetPlayfieldPuzzle()
    {
        selectedPresetPuzzle = presetPuzzleLists.PickRandom();
        summoningModule.ModuleLog(moduleId, "Selected Preset Puzzle with ID {0}.", selectedPresetPuzzle.puzzleId);
    }

    void InitializePlayfieldFromPresetPuzzle(bool placePixiesAtIntendedSolution, PresetPlayfieldPuzzle passedPresetPuzzle)
    {
        // Log
        if (placePixiesAtIntendedSolution)
        {
            summoningModule.ModuleLog(moduleId, "Initializing Playfield from Preset Puzzle. Placing Pixies at Intended Solution for Verification.");
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Initializing Playfield from Preset Puzzle.");
        }


        // Create void Playfield
        currentPlayfield = new Playfield();


        currentPlayfield.rowsWithValeFortPresent = new List<int>();
        currentPlayfield.rowsWithZaganTheTricksterPresent = new List<int>();

        // Initialize all Demons
        Demon[] _playfieldDemons = new Demon[passedPresetPuzzle.demonIds.Length];

        // Loop and Initialize all Demons one by one
        for (int i = 0; i < _playfieldDemons.Length; i ++)
        {
            _playfieldDemons[i].demonArchetypeId = passedPresetPuzzle.demonIds[i];
            _playfieldDemons[i].demonPlayfieldNumber = i + 1; // 0 is reserved for empty cells, so this is Demon number 1
            _playfieldDemons[i].debugFriendlyName = GetDemonDebugFriendlyName(_playfieldDemons[i].demonArchetypeId);
            _playfieldDemons[i].gridLocationIndex = ConvertPresetPuzzleLocationToGridIndex(passedPresetPuzzle.demonLocations[i]);
            _playfieldDemons[i].currentHealth = GetDefaultDemonHealth(_playfieldDemons[i].demonArchetypeId);
            _playfieldDemons[i].currentDamage = GetDefaultDemonDamage(_playfieldDemons[i].demonArchetypeId);
            _playfieldDemons[i].movementSpeed = GetDefaultDemonSpeed(_playfieldDemons[i].demonArchetypeId);
            _playfieldDemons[i].canMove = true;
            _playfieldDemons[i].canAttack = true;
            _playfieldDemons[i].hasReceivedHealthBoost = false;
            _playfieldDemons[i].hasReceivedDamageBoost = false;

            // Initialize data depending on the Archetypes we have
            switch (_playfieldDemons[i].demonArchetypeId)
            {
                // Y-morGre => Damage Boost on Attacks:
                case 1:
                    currentPlayfield.hasYmorGrePresent = true;
                    break;

                // Zagan the Trickster => Leftmost Pixie cannot attack:
                case 3:
                    currentPlayfield.rowsWithZaganTheTricksterPresent.Add(GetRowFromCellIndex(_playfieldDemons[i].gridLocationIndex, 8));
                    break;

                // Vale-Fort => Give Health Boost:
                case 6:
                    currentPlayfield.rowsWithValeFortPresent.Add(GetRowFromCellIndex(_playfieldDemons[i].gridLocationIndex, 8));
                    break;

                // Vassal-Goh => Summon Basel:
                case 8:
                    currentPlayfield.hasVassalGohPresent = true;
                    break;
            }
        }



        // Initialize all Pixies
        Pixie[] _playfieldPixies = new Pixie[passedPresetPuzzle.pixieIds.Length];

        for (int i = 0; i < _playfieldPixies.Length; i++)
        {
            _playfieldPixies[i].pixieArchetypeId = passedPresetPuzzle.pixieIds[i];
            _playfieldPixies[i].pixiePlayfieldNumber = _playfieldDemons.Length + i + 1;
            _playfieldPixies[i].debugFriendlyName = GetPixieDebugFriendlyName(_playfieldPixies[i].pixieArchetypeId);
            if (placePixiesAtIntendedSolution)
            {
                _playfieldPixies[i].gridLocationIndex = ConvertPresetPuzzleLocationToGridIndex(passedPresetPuzzle.intendedPixiePlacementSolution[i]);
            }
            else
            {
                // Pixies are initialized without any location as the Player must place them
                _playfieldPixies[i].gridLocationIndex = -1;
            }
            _playfieldPixies[i].currentHealth = GetDefaultPixieHealth(_playfieldPixies[i].pixieArchetypeId);
            _playfieldPixies[i].currentDamage = GetDefaultPixieDamage(_playfieldPixies[i].pixieArchetypeId);
            _playfieldPixies[i].hasReceivedDamageBoost = false;

            // Flag a Vashakapa as present if one is needed, for Damage Boost Check
            if (_playfieldPixies[i].pixieArchetypeId == 6)
            { currentPlayfield.hasVashakapaPresent = true; }
        }


        // Initialize Void
        voidedCellsIndices.Clear();
        foreach(string _voidLocation in passedPresetPuzzle.voidCellLocations)
        {
            voidedCellsIndices.Add(ConvertPresetPuzzleLocationToGridIndex(_voidLocation));
        }


        // Apply all values
        currentPlayfield.demons = _playfieldDemons;
        currentPlayfield.pixies = _playfieldPixies;
        ConstructPlayfieldGridRepresentation();


        // Special case for Vale-Fort, to give Health Boosts
        // Since no Demons can move rows or go more to the left than others,
        // we just need to check at the initialization, and upon Basel Summons
        if (currentPlayfield.rowsWithValeFortPresent.Count > 0)
        {
            int _leftMostInRow = 0;
            for (int _demonIndexInPlayfieldArray = 0; _demonIndexInPlayfieldArray < currentPlayfield.demons.Length; _demonIndexInPlayfieldArray ++)
            {
                // No multiple Health Boosts
                if (currentPlayfield.demons[_demonIndexInPlayfieldArray].hasReceivedHealthBoost)
                { continue; }

                // Ignore Demons on other Rows
                if (currentPlayfield.rowsWithValeFortPresent.Contains(GetRowFromCellIndex(currentPlayfield.demons[_demonIndexInPlayfieldArray].gridLocationIndex, 8)) == false)
                { continue; }

                // If the Demon is on the same Row as a Vale-Fort, verify all cells TO ITS LEFT
                // This will both verify Vale-Fort "to-the-right" condition as well as ensure one doesn't buff itself
                _leftMostInRow = GetRowFromCellIndex(currentPlayfield.demons[_demonIndexInPlayfieldArray].gridLocationIndex, 8) * 8;
                for (int _cellIndex = _leftMostInRow; _cellIndex < currentPlayfield.demons[_demonIndexInPlayfieldArray].gridLocationIndex; _cellIndex++)
                {
                    // If the cell contains a Demon
                    if (DoesPlayfieldCellContainDemon(_cellIndex))
                    {
                        // If the Demon is a Vale-Fort
                        if (GetDemonFromPlayfieldCellIndex(_cellIndex).demonArchetypeId == 6)
                        {
                            currentPlayfield.demons[_demonIndexInPlayfieldArray].hasReceivedHealthBoost = true;
                            currentPlayfield.demons[_demonIndexInPlayfieldArray].currentHealth += 1;
                        }
                    }
                }
            }

        }


        LogPlayfield();
    }

    /// <summary> Convert a Location as described in the PresetPuzzleLocation (00 - 73) to a Grid Index [0-31] </summary>
    int ConvertPresetPuzzleLocationToGridIndex(string puzzlePresetLocation)
    {
        // First digit is the column [0-7] and second is the row [0-3]
        // We shouldn't need to do any verification as the VerifyPresetPlayfieldPuzzleIntegrity() method already makes sure all the data is correct
        int _newGridIndex = CharToInt(puzzlePresetLocation[0]) + CharToInt(puzzlePresetLocation[1]) * 8;

        return _newGridIndex;
    }

    /// <summary> Convert a grid index [0-31] to a location in the PresetPuzzleLocation (0-0 - 7-3) </summary>
    string ConvertGridIndexToPresetPuzzleLocation(int gridIndex)
    {
        return GetColumnFromCellIndex(gridIndex, 8).ToString() + "-" + GetRowFromCellIndex(gridIndex, 8).ToString();
    }

    string GetDemonDebugFriendlyName(int demonId)
    {
        switch(demonId) {
            case 0: return "Basel";
            case 1: return "Y-morGre";
            case 2: return "Dashaz";
            case 3: return "Zagan the Trickster";
            case 4: return "Fléauros";
            case 5: return "Bifrovst";
            case 6: return "Vale-Fort";
            case 7: return "Sabnlock";
            case 8: return "Vassal-Goh"; }
        summoningModule.ModuleLog(moduleId, "Didn't find any Demon with ID {0}", demonId); return "Unknown";
    }
    int GetDefaultDemonHealth(int demonId)
    {
        switch (demonId)
        {
            case 0: return 2;
            case 1: return 3;
            case 2: return 2;
            case 3: return 2;
            case 4: return 4;
            case 5: return 4;
            case 6: return 10;
            case 7: return 2;
            case 8: return 4;
        }
        summoningModule.ModuleLog(moduleId, "Didn't find any Demon with ID {0}", demonId); return 1;
    }
    int GetDefaultDemonDamage(int demonId)
    {
        switch (demonId)
        {
            case 0: return 1;
            case 1: return 2;
            case 2: return 1;
            case 3: return 1;
            case 4: return 3;
            case 5: return 2;
            case 6: return 1;
            case 7: return 2;
            case 8: return 1;
        }
        summoningModule.ModuleLog(moduleId, "Didn't find any Demon with ID {0}", demonId); return 1;
    }
    int GetDefaultDemonSpeed(int demonId)
    {
        switch (demonId)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 2;
            case 3: return 1;
            case 4: return 1;
            case 5: return 1;
            case 6: return 1;
            case 7: return 1;
            case 8: return 1;
        }
        summoningModule.ModuleLog(moduleId, "Didn't find any Demon with ID {0}", demonId); return 1;
    }

    string GetPixieDebugFriendlyName(int pixieId)
    {
        switch (pixieId) {
            case 0: return "Nirmiti";
            case 1: return "Cryolite";
            case 2: return "Tryptophamia the All-Poweful";
            case 3: return "Glomie";
            case 4: return "Imhullu";
            case 5: return "Welkwing";
            case 6: return "Vashakapa";
            case 7: return "Mancie";
            case 8: return "Ifle-Sym";
            case 9: return "Arribon"; }
        summoningModule.ModuleLog(moduleId, "Didn't find any Pixie with ID {0}", pixieId); return "Unknown";
    }
    int GetDefaultPixieHealth(int pixieId)
    {
        switch (pixieId)
        {
            case 0: return 1;
            case 1: return 5;
            case 2: return 3;
            case 3: return 2;
            case 4: return 1;
            case 5: return 1;
            case 6: return 1;
            case 7: return 1;
            case 8: return 1;
            case 9: return 2;
        }
        summoningModule.ModuleLog(moduleId, "Didn't find any Pixie with ID {0}", pixieId); return 1;
    }
    int GetDefaultPixieDamage(int pixieId)
    {
        switch (pixieId)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 4;
            case 3: return 2;
            case 4: return 1;
            case 5: return 1;
            case 6: return 1;
            case 7: return 3;
            case 8: return 5;
            case 9: return 1;
        }
        summoningModule.ModuleLog(moduleId, "Didn't find any Pixie with ID {0}", pixieId); return 1;
    }

    void LogIntendedSolution()
    {
        string _intendedPixiePlacements = "";

        for (int i = 0; i < selectedPresetPuzzle.intendedPixiePlacementSolution.Length; i ++)
        {
            if (i != 0)
            {
                if (i == selectedPresetPuzzle.intendedPixiePlacementSolution.Length - 1)
                {
                    _intendedPixiePlacements += " and ";
                }
                else
                {
                    _intendedPixiePlacements += ", ";
                }
            }

            _intendedPixiePlacements += selectedPresetPuzzle.intendedPixiePlacementSolution[i][0] + "-" + selectedPresetPuzzle.intendedPixiePlacementSolution[i][1];
        }

        summoningModule.ModuleLog(moduleId, "For information, one intended solution is to place the Pixies in cells {0} in order.", _intendedPixiePlacements);
    }

    void ConstructPlayfieldGridRepresentation()
    {
        // Initialize all default values to 0
        currentPlayfield.playfieldRepresentation = new int[32];

        // For each cell in the grid
        for (int i = 0; i < 32; i++)
        {
            // If void, become -1
            if (voidedCellsIndices.Contains(i))
            {
                currentPlayfield.playfieldRepresentation[i] = -1;
                continue;
            }

            // If Demon, show the Demon's Number 
            foreach (Demon _demon in currentPlayfield.demons)
            {
                if (_demon.gridLocationIndex == i)
                {
                    currentPlayfield.playfieldRepresentation[i] = _demon.demonPlayfieldNumber;

                    // Usage of GoTo to break out of this foreach and continue without going to the Pixie verification
                    goto EndFor;
                }
            }


            // If Pixie, show the Pixie's Number 
            foreach (Pixie _pixie in currentPlayfield.pixies)
            {
                if (_pixie.gridLocationIndex == i)
                {
                    currentPlayfield.playfieldRepresentation[i] = _pixie.pixiePlayfieldNumber;
                    break;
                }
            }


        EndFor:
            continue;
        }
    }

    bool IsPlayfieldCellEmpty(int cellToVerify)
    {
        return currentPlayfield.playfieldRepresentation[cellToVerify] == 0;
    }

    bool DoesPlayfieldCellContainCreature(int cellToVerify)
    {
        return currentPlayfield.playfieldRepresentation[cellToVerify] > 0;
    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    // Playfield Timestep Functions - Main Minigame Logic
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void SimulatePlay()
    {
        // Here we freaking go:
        currentTimestepIndex = 0;

        // Enable Simulation
        isSimulatingPlay = true;

        summoningModule.ModuleLog(moduleId, "Starting Simulation of Demons & Pixies:", currentTimestepIndex);
        

        // Literally main while loop... Is this a program?
        while (isSimulatingPlay)
        {
            // Increment Timestep, first actions happen at 1
            currentTimestepIndex++;

            summoningModule.ModuleLog(moduleId, "=-=-=-=-=-= TIMESTEP {0} =-=-=-=-=-=", currentTimestepIndex);

            LogPlayfield();

            // Failsafe to avoid infinite loops in case something bad happens
            if (currentTimestepIndex >= 100)
            {
                isSimulatingPlay = false;
                summoningModule.ModuleLog(moduleId, "Arrived at 100 Timesteps... What the hell did you even do? You get a pass this time, but be careful! (this shouldn't ever happen)");
                ModuleShouldSolve();
            }


            // If we have a Vassal-Goh, try to summon a Basel on 3rd Timestep start
            if (currentTimestepIndex == 3 && currentPlayfield.hasVassalGohPresent)
            {
                TrySummonFromVassalGoh();
            }


            // First, Demon Moves
            // If statement is there to catch Strikes, which happen when it returns true
            if (MoveAllDemonsForward())
            {
                if (isInIntegrityVisualizationMode)
                { 
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 20: !i!i!i!i! Intended Solution doesn't work for Puzzle ID {0}",
                        selectedPresetPuzzle.puzzleId); 
                }
                else
                {  ModuleShouldStrike(); }
                return;
            }


            // Then, Everyone Attacks
            ProcessAttackPhase();

            // Then now, kill creatures at 0 Health
            // If this returns true, it has solved the module
            if (ProcessDeathPhase())
            {
                if (isInIntegrityVisualizationMode == false)
                { 
                    ModuleShouldSolve(); 
                }
                return;
            }

            // And Repeat, until a DemonMovement arrives left, or an Attack Phase kills the last Demon!
        }

    }


    /// <summary> Move all Demons to the left. Returns true if it has struck </summary>
    bool MoveAllDemonsForward()
    {

        // Apply Demon movements from left to right to prevent blockers.
        // We can just step through the playfield linearly, that's good enough.
        for (int _cellIndex = 0; _cellIndex < 32; _cellIndex++)
        {

            // If cell Empty or Void, ignore
            if (DoesPlayfieldCellContainCreature(_cellIndex) == false)
            { continue; }

            // If cell has a Pixie, ignore
            if (DoesPlayfieldCellContainPixie(_cellIndex))
            { continue; }

            Demon _demonToMove = GetDemonFromPlayfieldCellIndex(_cellIndex);

            switch (_demonToMove.movementSpeed)
            {
                // One every Other
                case 0:
                    // Can it move?
                    if (_demonToMove.canMove)
                    {
                        // If statement is there to return in case of Strike
                        if (MoveSingularDemonLeft(ref _demonToMove))
                        {
                            return true;
                        }
                    }

                    // Flip its movement flag
                    _demonToMove.canMove = !_demonToMove.canMove;
                    ApplyAndUpdateDemonValuesInArray(_demonToMove);
                    break;

                case 1:
                    // Can it move?
                    if (_demonToMove.canMove)
                    {
                        // If statement is there to return in case of Strike
                        if (MoveSingularDemonLeft(ref _demonToMove))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Movement Stuns are a result of Glomie, and last a single turn;
                        _demonToMove.canMove = true;
                        ApplyAndUpdateDemonValuesInArray(_demonToMove);
                    }

                    break;

                case 2:
                    // Can it move?
                    if (_demonToMove.canMove)
                    {
                        // Try to move once, but if it succeeds then move again
                        // If statement is there to return in case of Strike
                        if (MoveSingularDemonLeft(ref _demonToMove, true))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // Movement Stuns are a result of Glomie, and last a single turn;
                        _demonToMove.canMove = true;
                        ApplyAndUpdateDemonValuesInArray(_demonToMove);
                    }

                    break;
            }
        }

        return false;
    }

    /// <summary> Move a singular Demon left. Returns true if a Strike Happened </summary>
    bool MoveSingularDemonLeft(ref Demon demonToMove, bool canDoubleMove = false)
    {
        int _originalDemonCellIndex = demonToMove.gridLocationIndex;
        int _newDemonCellIndex = _originalDemonCellIndex;

        // Move left, ignoring Void, and see what we land on
        // If .ranIntoGridEdges is true, the Demon left the playfield and we lost
        if (MoveAroundGridWithVoid(MovementDirection.Left, currentPlayfield.playfieldRepresentation.Length, ref _newDemonCellIndex, 8, false).ranIntoGridEdges)
        {
            summoningModule.ModuleLog(moduleId, "Demon with Creature Number {0} just left the grid from the left. Your solution is therefore incorrect. STRIKE!!",
                demonToMove.demonPlayfieldNumber);
            return true;
        }

        // If cell isn't free, don't move
        if (IsPlayfieldCellEmpty(_newDemonCellIndex) == false)
        {
            summoningModule.ModuleLog(moduleId, "Demon with Creature Number {0} can't move as its path is blocked.", demonToMove.demonPlayfieldNumber);
            return false;
        }

        // Apply location to this grid
        demonToMove.gridLocationIndex = _newDemonCellIndex;
        currentPlayfield.playfieldRepresentation[_newDemonCellIndex] = demonToMove.demonPlayfieldNumber;
        currentPlayfield.playfieldRepresentation[_originalDemonCellIndex] = 0;

        // Apply the Demon's position and flags and stuff
        // Since it's passed as a Ref, it can still be re-applied later on as the changes follow back up the function chain :D
        ApplyAndUpdateDemonValuesInArray(demonToMove);

        summoningModule.ModuleLog(moduleId, "Demon with Creature Number {0} has moved to location {1}.",
            demonToMove.demonPlayfieldNumber, ConvertGridIndexToPresetPuzzleLocation(_newDemonCellIndex));

        // If the movement was successful and a DoubleMove is allowed, double move!
        if (canDoubleMove)
        {
            if (MoveSingularDemonLeft(ref demonToMove))
            { return true; }
        }

        return false;
    }

    void ProcessAttackPhase()
    {
        // Apply all Damages, but don't kill yet

        // Because of Bifrovst, we need to step in reverse order because it needs to prevent Pixies from attacking
        for (int _cellIndex = 31; _cellIndex >= 0; _cellIndex--)
        {
            // If cell Empty or Void, ignore
            if (DoesPlayfieldCellContainCreature(_cellIndex) == false)
            { continue; }

            // Otherwise, we're dealing with a Creature, Pixie or Demon
            // Save it
            if (DoesPlayfieldCellContainPixie(_cellIndex))
            {
                HandleSinglePixieAttack(GetPixieFromPlayfieldCellIndex(_cellIndex));
            }
            else
            {
                HandleSingleDemonAttack(GetDemonFromPlayfieldCellIndex(_cellIndex));
            }
        }
    }

    /// <summary> Return true if all Demons are dead and the module solved </summary>
    bool ProcessDeathPhase()
    {
        Pixie _pixieToKill;
        Demon _demonToKill;
        bool hasKilledDemon = false;

        // We can step in forward order in this case :D
        for (int _cellIndex = 0; _cellIndex < 32; _cellIndex++)
        {

            // If cell Empty or Void, ignore
            if (DoesPlayfieldCellContainCreature(_cellIndex) == false)
            { continue; }

            // Otherwise, we're dealing with a Creature, Pixie or Demon
            // Check its health, and if needed remove it from the Playfield.
            // There is no need to do stuff to the Creature in its state, or to remove it from the Demon or Pixie's arrays,
            // as this would mess up counts and playfield numbers a lot and hurt readability
            if (DoesPlayfieldCellContainPixie(_cellIndex))
            {
                _pixieToKill = GetPixieFromPlayfieldCellIndex(_cellIndex);

                // Don't kill if the Pixie's alive
                if (_pixieToKill.currentHealth > 0)
                { continue; }


                // Log
                summoningModule.ModuleLog(moduleId, "Pixie with Creature Number {0} died because of the damage it received.",
                    _pixieToKill.pixiePlayfieldNumber);


                // In the case of a Glomie, it prevents movements for 1 timestep
                if (_pixieToKill.pixieArchetypeId == 3)
                {
                    GlomieExplosionOnDeath(_pixieToKill.gridLocationIndex);
                }

                // Remove it from the Playfield
                currentPlayfield.playfieldRepresentation[_cellIndex] = 0;
                
            }
            // We have a Demon
            else
            {
                _demonToKill = GetDemonFromPlayfieldCellIndex(_cellIndex);

                // Don't kill if the Demon's alive
                if (_demonToKill.currentHealth > 0)
                { continue; }

                // Log
                summoningModule.ModuleLog(moduleId, "Demon with Creature Number {0} died because of the damage it received.",
                    _demonToKill.demonPlayfieldNumber);

                // Remove it from the playfield
                currentPlayfield.playfieldRepresentation[_cellIndex] = 0;

                // Mark that we killed a Demon, to check if it was the last one
                hasKilledDemon = true;
            }
        }


        // After all that, if ALL Demons have died, Solve.
        if (hasKilledDemon)
        {
            if (CheckIfAllDemonsAreDead())
            {
                return true;
            }
        }
        return false;
    }

    bool CheckIfAllDemonsAreDead()
    {
        bool areAllDead = true;

        // Check them all individually
        foreach (Demon _demon in currentPlayfield.demons)
        {
            if (_demon.currentHealth > 0)
            {
                areAllDead = false;
                break;
            }
        }

        // If the boolean is still true, then they all are Dead
        if (areAllDead)
        {
            summoningModule.ModuleLog(moduleId, "All Demons are Dead! Congratulations on defending yourself and mastering Demons & Pixies!");
            return true;
        }

        return false;
    }

    /// <summary> Makes a singular Demon attack, using Scratch Demon as its input, and DemonIndex as the index of this Demon in Playfield.Demons </summary>
    void HandleSingleDemonAttack(Demon _demon)
    {
        // Should only happen for the Fléauros, but prevent its attack for one Timestep
        if (_demon.canAttack == false)
        {
            _demon.canAttack = true;
            ApplyAndUpdateDemonValuesInArray(_demon);
        }

        int _attackTargetCellIndex = _demon.gridLocationIndex;

        // Move left until we reach a valid cell
        MoveAroundGridWithVoid(MovementDirection.Left, currentPlayfield.playfieldRepresentation.Length, ref _attackTargetCellIndex, 8, false);

        // If this is empty, or a Demon, ignore
        if (DoesPlayfieldCellContainPixie(_attackTargetCellIndex) == false)
        { return; }


        // Else, we landed on a Pixie, so save it
        Pixie _targettedPixie = GetPixieFromPlayfieldCellIndex(_attackTargetCellIndex);
        // Deal Damage, including potential Bonus Damage
        _targettedPixie.currentHealth -= (_demon.currentDamage + GetYmorGreBonusDamage(_demon.gridLocationIndex) );
        

        // Special Ability Switch on valid attack
        switch (_demon.demonArchetypeId)
        {
            // Fléauros can't move nor attack next timstep
            case 4:
                _demon.canMove = false;
                _demon.canAttack = false;
                ApplyAndUpdateDemonValuesInArray(_demon);
                break;

            // Bifrovst prevent the targetted Pixie from attacking
            case 5:
                _targettedPixie.hasBeenFrozenByBifrovst = true;
                break;

        }

        // Set the Pixie back
        ApplyAndUpdatePixieValuesInArray(_targettedPixie);

        // Log
        summoningModule.ModuleLog(moduleId, "Demon with Creature Number {0} attacked Pixie with Creature Number {1}, who is left with {2} Health.",
            _demon.demonPlayfieldNumber, _targettedPixie.pixiePlayfieldNumber, _targettedPixie.currentHealth);
    }

    /// <summary> Makes a singular Pixie attack, using Scratch Pixie as its input, and PixieIndex as the index of this Pixie in Playfield.Pixies </summary>
    void HandleSinglePixieAttack(Pixie pixie)
    {
        // Cryolite cannot ever attack
        if (pixie.pixieArchetypeId == 1)
        { return; }

        // Can't attack if blocked by Demons
        if (IsPixieBlockedFromAttacking(pixie))
        { return; }

        int _attackTargetCellIndices;

        // Ranges are all over the place, so we have to do all the stuff in a switch...
        switch (pixie.pixieArchetypeId)
        {
            // Nirmiti, basic Pixie
            case 0:
                DealWithSingularStraightPixieAttack(MovementDirection.Right, 4, pixie);
                break;


            // Tryptophamia the All-Powerful hurts itself after attacking
            case 2:
                if (DealWithSingularStraightPixieAttack(MovementDirection.Right, 2, pixie))
                {
                    pixie.currentHealth -= 2;
                    ApplyAndUpdatePixieValuesInArray(pixie);
                }

                break;


            // Glomie attacks ALL Demons in a 3x3 square
            case 3:
                for (int _direction = 0; _direction < 8; _direction ++)
                {
                    // Value to move around
                    _attackTargetCellIndices = pixie.gridLocationIndex;

                    // Move around
                    MoveAroundGridWithVoid((MovementDirection)_direction, currentPlayfield.playfieldRepresentation.Length, ref _attackTargetCellIndices, 8, false);

                    // Is the landed cell occupied by a Demon?
                    if (DoesPlayfieldCellContainDemon(_attackTargetCellIndices))
                    {
                        // Save Demon
                        Demon _targetDemon = GetDemonFromPlayfieldCellIndex(_attackTargetCellIndices);

                        // Check for Sabnlock as it can't be hit from other rows
                        if (_targetDemon.demonArchetypeId == 7)
                        {
                            if (GetRowFromCellIndex(_targetDemon.gridLocationIndex, 8) != GetRowFromCellIndex(pixie.gridLocationIndex, 8))
                            {
                                continue;
                            }
                        }

                        PixieDamagesDemon(pixie, _targetDemon);
                    }
                }
                break;


            // Imhullu tries to move left once after attacking
            case 4:
                if (DealWithSingularStraightPixieAttack(MovementDirection.Right, 2, pixie))
                {
                    int _imhulluFinalMovementCellIndex = pixie.gridLocationIndex;

                    // If didn't hit edges
                    if (MoveAroundGridWithVoid(MovementDirection.Left, currentPlayfield.playfieldRepresentation.Length, ref _imhulluFinalMovementCellIndex, 8, false).ranIntoGridEdges == false)
                    {
                        // If cell is empty and available
                        if (IsPlayfieldCellEmpty(_imhulluFinalMovementCellIndex))
                        {
                            // Move to new location
                            currentPlayfield.playfieldRepresentation[_imhulluFinalMovementCellIndex] = pixie.pixiePlayfieldNumber;

                            // Set old location to Empty
                            currentPlayfield.playfieldRepresentation[pixie.gridLocationIndex] = 0;

                            // Save Location
                            pixie.gridLocationIndex = _imhulluFinalMovementCellIndex;

                            // Apply Pixie
                            ApplyAndUpdatePixieValuesInArray(pixie);
                        }
                    }
                    
                }
                
                break;


            // Welkwing tries to attack in a 3x3 in front of it
            case 5:
                // Since it can only attack the first Demon in the line,
                // the philosophy will be to do 3 individual attacks on the 3 lanes

                DealWithSingularStraightPixieAttack(MovementDirection.Right, 3, pixie, -1);
                DealWithSingularStraightPixieAttack(MovementDirection.Right, 3, pixie, 0);
                DealWithSingularStraightPixieAttack(MovementDirection.Right, 3, pixie, 1);
                break;


            // Vashakapa
            case 6:
                DealWithSingularStraightPixieAttack(MovementDirection.Right, 3, pixie);
                break;


            // Mancie attacks in a 3x3 range
            case 7:
                // Cannot attack until 5th Timestep
                if (currentTimestepIndex < 5)
                { break; }

                // Attack in a 3x3 Range
                for (int _direction = 0; _direction < 8; _direction++)
                {
                    // Value to move around
                    _attackTargetCellIndices = pixie.gridLocationIndex;

                    // Move around
                    MoveAroundGridWithVoid((MovementDirection)_direction, currentPlayfield.playfieldRepresentation.Length, ref _attackTargetCellIndices, 8, false);

                    // Is the landed cell occupied by a Demon?
                    if (DoesPlayfieldCellContainDemon(_attackTargetCellIndices))
                    {
                        // Save Demon
                        Demon _targetDemon = GetDemonFromPlayfieldCellIndex(_attackTargetCellIndices);

                        // Check for Sabnlock as it can't be hit from other rows
                        if (_targetDemon.demonArchetypeId == 7)
                        {
                            if (GetRowFromCellIndex(_targetDemon.gridLocationIndex, 8) != GetRowFromCellIndex(pixie.gridLocationIndex, 8))
                            {
                                continue;
                            }
                        }

                        PixieDamagesDemon(pixie, _targetDemon);
                    }
                }

                break;


            // Ifle-Sym loses Attack Damage after each attack
            case 8:
                if (DealWithSingularStraightPixieAttack(MovementDirection.Right, 3, pixie))
                {
                    pixie.currentDamage -= 2;
                    ApplyAndUpdatePixieValuesInArray(pixie);
                }
                

                break;

            // Aribbon attack Backwards, to the Left
            case 9:
                DealWithSingularStraightPixieAttack(MovementDirection.Left, 5, pixie);
                break;
        }
    }

    /// <summary> Returns true if there was a successful attack. If the Welkwing Offset is other than 0, then it will offset the attacked row
    /// by that number, and also check for Sabnlock since it can't be attacked outside of its row.</summary> 
    bool DealWithSingularStraightPixieAttack(MovementDirection direction, int maximumRange, Pixie attackingPixie, int welkwingOffset = 0)
    {
        // Save the location from which the attack is launched. Welkwing can offset it up or down.
        int _attackTargetCellLocation = attackingPixie.gridLocationIndex + (8 * welkwingOffset);

        // Check for Welkwing offsets to avoid attacking outside of the Playfield
        if (_attackTargetCellLocation < 0 || _attackTargetCellLocation > 31)
        { return false; }


        // Check forward up to the max range
        for (int i = 0; i < maximumRange; i++)
        {
            // At each step, if it got out of the edges just cancel attack
            if (MoveAroundGridWithVoid(direction, currentPlayfield.playfieldRepresentation.Length, ref _attackTargetCellLocation, 8, false).ranIntoGridEdges)
            { return false; }


            // Shouldn't ever happen, but if it lands on a Void we have big issues
            if (currentPlayfield.playfieldRepresentation[_attackTargetCellLocation] == -1)
            {
                summoningModule.ModuleLog(moduleId, "Stopped on a Void Cell at {1} while trying to attack with Pixie Number {0}!!!! THIS IS BAD",
                    attackingPixie.pixiePlayfieldNumber, _attackTargetCellLocation);
                return false;
            }

            // If it lands on anything other than a Demon, continue
            if (DoesPlayfieldCellContainDemon(_attackTargetCellLocation) == false)
            { continue; }
           



            // Else, we found our Demon Target!
            Demon _targetDemon = GetDemonFromPlayfieldCellIndex(_attackTargetCellLocation);

            // Was an Welkwing attacking?
            if (welkwingOffset != 0)
            {
                // Is it a Sabnlock?
                if (_targetDemon.demonArchetypeId == 7)
                {
                    // You can't attack it if the Welkwing is on a different row!
                    if (GetRowFromCellIndex(_targetDemon.gridLocationIndex, 8) != GetRowFromCellIndex(attackingPixie.gridLocationIndex, 8))
                    { return false; }
                }
            }


            // Damage it!
            // No need to check for Sabnlock rules as this is guaranteed to be in the same lane
            PixieDamagesDemon(attackingPixie, _targetDemon);


            // Quit the loop
            return true;
        }

        // This return happens only if all cells in the range are empty
        return false;
    }

    void PixieDamagesDemon(Pixie pixie, Demon demon)
    {
        // Vashakapa can buff other Pixies!
        // Because pixies can move around and stuff, it's easier to check manually before every attack.
        demon.currentHealth -= pixie.currentDamage + GetVashakapaBonusDamage(pixie.gridLocationIndex);
        ApplyAndUpdateDemonValuesInArray(demon);

        // Log
        summoningModule.ModuleLog(moduleId, "Pixie with Creature Number {0} attacked Demon with Creature Number {1}, who is left with {2} Health.",
            pixie.pixiePlayfieldNumber, demon.demonPlayfieldNumber, demon.currentHealth);
    }

    int GetVashakapaBonusDamage(int pixieLocation)
    {
        // No Vashakapa? No Bonus!
        if (currentPlayfield.hasVashakapaPresent == false)
        { return 0; }

        int _temporaryLocation = pixieLocation;

        // Check one cell up...
        MoveAroundGridWithVoid(MovementDirection.Up, currentPlayfield.playfieldRepresentation.Length, ref _temporaryLocation, 8, false);
        if (DoesPlayfieldCellContainPixie(_temporaryLocation))
        {
            if (GetPixieFromPlayfieldCellIndex(_temporaryLocation).pixieArchetypeId == 6)
            {
                return 1;
            }
        }


        // ...and one cell down!
        _temporaryLocation = pixieLocation;
        MoveAroundGridWithVoid(MovementDirection.Down, currentPlayfield.playfieldRepresentation.Length, ref _temporaryLocation, 8, false);
        if (DoesPlayfieldCellContainPixie(_temporaryLocation))
        {
            if (GetPixieFromPlayfieldCellIndex(_temporaryLocation).pixieArchetypeId == 6)
            {
                return 1;
            }
        }

        // If code execution lands here, nothing has been found!
        return 0;
    }

    bool IsPixieBlockedFromAttacking(Pixie pixie)
    {
        // Check if Pixie has been frozen by Bifrovst
        // Since we handle attacks in reverse Playfield order, the Bifrovst attacks before the Pixie
        if (pixie.hasBeenFrozenByBifrovst)
        {
            // Unfreeze the Pixie as freezing only lasts one Timestep
            pixie.hasBeenFrozenByBifrovst = false;

            // Apply change
            ApplyAndUpdatePixieValuesInArray(pixie);

            // Log
            summoningModule.ModuleLog(moduleId, "Pixie with Creature Number {0} has been frozen by a Bifrovst and can't attack",
                pixie.pixiePlayfieldNumber);
            return true;
        }


        // No Zagan? Nothing can block it anymore
        if (currentPlayfield.rowsWithZaganTheTricksterPresent.Count == 0)
        { return false; }

        // If there is a Zagan but it's on another row, don't bother checking anything
        if (currentPlayfield.rowsWithZaganTheTricksterPresent.Contains(GetRowFromCellIndex(pixie.gridLocationIndex, 8)) == false)
        { return false; }


        // If Zagan is (OR WAS!!) on this row, check it
        // We still end up checking rows where there WAS a Zagan The Trickster, even if there isn't anymore;
        // But in the majority of cases this will already prevent checking rows 3/4th of the time
        int _leftmostIndex = Mathf.FloorToInt(pixie.gridLocationIndex / 8);
        bool _foundZagan = false;

        // Start at the rightmost column, going left
        for (int _cellToCheckForZagan = _leftmostIndex + 7; _cellToCheckForZagan >= _leftmostIndex ; _cellToCheckForZagan--)
        {
            // If we encounter any OTHER Pixie, we're safe from Zagan
            if (DoesPlayfieldCellContainPixie(_cellToCheckForZagan))
            {
                // Make sure the Pixie isn't us
                if (_cellToCheckForZagan != pixie.gridLocationIndex)
                {
                    return false;
                }
            }

            // Else, if we find a Demon
            if (DoesPlayfieldCellContainDemon(_cellToCheckForZagan))
            {
                // Is this a Zagan?
                if (GetDemonFromPlayfieldCellIndex(_cellToCheckForZagan).demonArchetypeId == 3)
                {
                    _foundZagan = true;
                    
                    // Don't return, as we might find a Zagan early but still find a Pixie to block its curse, so we need to find something
                }
            }


            // Have we gone past the current Pixie (without seeing another one) and found a Zagan? We know this Pixie is blocked!
            if (_foundZagan && _cellToCheckForZagan < pixie.gridLocationIndex)
            {
                // Log
                summoningModule.ModuleLog(moduleId, "Pixie with Creature Number {0} is blocked from attacking because of a Zagan The Trickster",
                    pixie.pixiePlayfieldNumber);
                return true;
            }
        }

        // At the very end, if code execution arrives here, we just return whether we found a Zagan or Not

        if (_foundZagan)
        {
            // Log
            summoningModule.ModuleLog(moduleId, "Pixie with Creature Number {0} is blocked from attacking because of a Zagan The Trickster",
                pixie.pixiePlayfieldNumber);
        }
        return _foundZagan;
    }

    int GetYmorGreBonusDamage(int DemonLocation)
    {
        // No Y-morGre? No Bonus!
        if (currentPlayfield.hasYmorGrePresent == false)
        { return 0; }

        int _temporaryLocation;

        // Check all 8 directions
        for (int i = 0; i < 8; i++)
        {
            // Reset to demon location
            _temporaryLocation = DemonLocation;

            // Move once in the direction
            MoveAroundGridWithVoid((MovementDirection)i, currentPlayfield.playfieldRepresentation.Length, ref _temporaryLocation, 8, false);

            // Is this a Demon?
            if (DoesPlayfieldCellContainDemon(_temporaryLocation))
            {
                // Is this Demon an Y-morGre?
                if (GetDemonFromPlayfieldCellIndex(_temporaryLocation).demonArchetypeId == 1)
                {
                    // Return directly: the Demon can only get +1 Damage so no need to check the rest
                    return 1;
                }
            }
        }

        // If we land here, none of the 8 directions were Y-morGres!
        return 0;
    }
    
    void TrySummonFromVassalGoh()
    {
        int _potentialSpawningCellIndex;

        // We might have multiple of them!
        foreach (Demon _potentialVassalGoh in currentPlayfield.demons)
        {
            // Don't do anything for non-Vassal-Goh Demons, obviously
            if (_potentialVassalGoh.demonArchetypeId != 8)
            { continue;  }


            // If the Demon is on the right of the board, ignore
            if (GetColumnFromCellIndex(_potentialVassalGoh.gridLocationIndex, 8) == 7)
            {
                summoningModule.ModuleLog(moduleId, "The Vassal-Goh with number {0} couldn't spawn as it is on the right side of the playfield.",
                    _potentialVassalGoh.demonPlayfieldNumber);
                continue;
            }

            // Save current location
            _potentialSpawningCellIndex = _potentialVassalGoh.gridLocationIndex;

            // Move to the left until we find something that is non-Void!
            // Also, ignore if we left of the Playfield
            if (MoveAroundGridWithVoid(MovementDirection.Right, currentPlayfield.playfieldRepresentation.Length, ref _potentialSpawningCellIndex, 8, false).ranIntoGridEdges)
            {
                continue;
            }


            // Ignore if the Cell is occupied by something else
            if (DoesPlayfieldCellContainCreature(_potentialSpawningCellIndex))
            {
                summoningModule.ModuleLog(moduleId, "The Vassal-Goh with number {0} couldn't spawn as its right cell is occupied.",
                        _potentialVassalGoh.demonPlayfieldNumber);
                continue;
            }
            

            // Spawn Basel
            Demon _spawnedBasel = new Demon();
            _spawnedBasel.demonArchetypeId = 0;
            // Playfield Number is equal to number of Demons.
            // Since the Array hasn't been resized yet, currentPlayfield.demons.Length is equal to the Number of the previous latest Demon
            _spawnedBasel.demonPlayfieldNumber = currentPlayfield.demons.Length + 1;
            _spawnedBasel.debugFriendlyName = GetDemonDebugFriendlyName(0);
            _spawnedBasel.gridLocationIndex = _potentialSpawningCellIndex;
            _spawnedBasel.currentHealth = GetDefaultDemonHealth(0);
            _spawnedBasel.currentDamage = GetDefaultDemonDamage(0);
            _spawnedBasel.movementSpeed = GetDefaultDemonSpeed(0);
            _spawnedBasel.canMove = true;
            _spawnedBasel.canAttack = true;
            _spawnedBasel.hasReceivedHealthBoost = false;
            _spawnedBasel.hasReceivedDamageBoost = false;

            // Offset each Pixie Creature Number by one
            for (int i = 0; i < currentPlayfield.pixies.Length; i ++)
            {
                currentPlayfield.pixies[i].pixiePlayfieldNumber++;
            }
            // Update the Playfield Representation by offsetting the Pixies too
            for (int i = 0; i < 32; i ++)
            {
                if (DoesPlayfieldCellContainPixie(i))
                {
                    currentPlayfield.playfieldRepresentation[i]++;
                }
            }

            // Special case for Vale-Fort, to give Health Boosts
            if (currentPlayfield.rowsWithValeFortPresent.Count > 0)
            {
                // Verify if the Basel is on the same row as a Vale-Fort
                if (currentPlayfield.rowsWithValeFortPresent.Contains(GetRowFromCellIndex(_spawnedBasel.gridLocationIndex, 8)))
                {
                    // If it is on the same Row as a Vale-Fort, verify all cells TO ITS LEFT
                    // This will both verify Vale-Fort's condition
                    for (int _cellIndex = GetRowFromCellIndex(_spawnedBasel.gridLocationIndex, 8) * 8; _cellIndex < _spawnedBasel.gridLocationIndex; _cellIndex++)
                    {
                        // If the cell contains a Demon
                        if (DoesPlayfieldCellContainDemon(_cellIndex))
                        {
                            // If the Demon is a Vale-Fort
                            if (GetDemonFromPlayfieldCellIndex(_cellIndex).demonArchetypeId == 6)
                            {
                                _spawnedBasel.hasReceivedHealthBoost = true;
                                _spawnedBasel.currentHealth += 1;
                            }
                        }
                    }
                }
            }

            // Add Basel to Demon List
            Array.Resize(ref currentPlayfield.demons, currentPlayfield.demons.Length +1);
            currentPlayfield.demons[currentPlayfield.demons.Length - 1] = _spawnedBasel;

            // Update playfieldRepresentation
            currentPlayfield.playfieldRepresentation[_spawnedBasel.gridLocationIndex] = _spawnedBasel.demonPlayfieldNumber;

            // Log
            summoningModule.ModuleLog(moduleId, "The Vassal-Goh with number {0} spawned a Basel at location {1}. This Basel has been assigned Creature Number {2}.",
                _potentialVassalGoh.demonPlayfieldNumber, ConvertGridIndexToPresetPuzzleLocation(_spawnedBasel.gridLocationIndex), _spawnedBasel.demonPlayfieldNumber);
            
        }
    }

    void GlomieExplosionOnDeath(int GlomieCellIndex)
    {
        int _finalCell;
        for (int _directions = 0; _directions < 8; _directions++)
        {
            // Start at Glomie Location
            _finalCell = GlomieCellIndex;

            // Move in direction
            MoveAroundGridWithVoid((MovementDirection)_directions, currentPlayfield.playfieldRepresentation.Length, ref _finalCell, 8, false);

            // Do we land on a Demon?
            if (DoesPlayfieldCellContainDemon(_finalCell))
            {
                // Get the Demon and stun it for a turn
                Demon _stunnedDemon = GetDemonFromPlayfieldCellIndex(_finalCell);
                _stunnedDemon.canMove = false;
                ApplyAndUpdateDemonValuesInArray(_stunnedDemon);
            }
        }
    }



    // Bunch of Utility Functions to avoid typos and code repeat
    bool DoesPlayfieldCellContainDemon(int cellIndex)
    {
        int _cellContent = currentPlayfield.playfieldRepresentation[cellIndex];

        // summoningModule.ModuleLog(moduleId, "I say with certitude that the cell in {0} contains {1}.", ConvertGridIndexToPresetPuzzleLocation(cellIndex), _cellContent);
        return _cellContent > 0 && _cellContent <= currentPlayfield.demons.Length;
    }

    bool DoesPlayfieldCellContainPixie(int cellIndex)
    {
        int _cellContent = currentPlayfield.playfieldRepresentation[cellIndex];

        // summoningModule.ModuleLog(moduleId, "I say with certitude that the cell in {0} contains {1}.", ConvertGridIndexToPresetPuzzleLocation(cellIndex), _cellContent);
        return _cellContent > currentPlayfield.demons.Length;
    }

    Demon GetDemonFromPlayfieldCellIndex(int cellIndex)
    {
        if (DoesPlayfieldCellContainDemon(cellIndex) == false)
        {
            summoningModule.ModuleLog(moduleId, "Got an error trying to access Demon in cell {0}.", ConvertGridIndexToPresetPuzzleLocation(cellIndex));
            return currentPlayfield.demons[0];
        }
        return currentPlayfield.demons[currentPlayfield.playfieldRepresentation[cellIndex] - 1];
    }

    void ApplyAndUpdateDemonValuesInArray(Demon _demon)
    {
        currentPlayfield.demons[_demon.demonPlayfieldNumber - 1] = _demon;
    }

    Pixie GetPixieFromPlayfieldCellIndex(int cellIndex)
    {
        if (DoesPlayfieldCellContainPixie(cellIndex) == false)
        {
            summoningModule.ModuleLog(moduleId, "Got an error trying to access Pixie in cell {0}.", ConvertGridIndexToPresetPuzzleLocation(cellIndex));
            return currentPlayfield.pixies[0];
        }
        return currentPlayfield.pixies[currentPlayfield.playfieldRepresentation[cellIndex] - currentPlayfield.demons.Length - 1];
    }

    void ApplyAndUpdatePixieValuesInArray(Pixie _pixie)
    {
        currentPlayfield.pixies[_pixie.pixiePlayfieldNumber - currentPlayfield.demons.Length - 1] = _pixie;
    }

    void ModuleShouldSolve()
    {
        LogPlayfield();

        isSimulatingPlay = false;
        summoningModule.ReceiveSolve();
    }

    void ModuleShouldStrike()
    {
        // No need to Log the Playfield because it is in the same state as at the start of the current Timestep.
        // LogPlayfield();

        isSimulatingPlay = false;
        summoningModule.ReceiveStrike();

        ResetGridAndPlayerInputs();
    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Playfield Data Plate Inscription
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void ComputeSumOfSerialNumbersIntoBinary()
    {
        int _sum = bombInfo.GetSerialNumberNumbers().ToArray().Sum();
        fiveDigitBinarySumOfSerialNumber = numbersAsBinary[_sum];

        summoningModule.ModuleLog(moduleId, "Sum of Serial Number Digits is {0}, its 5-digit binary representation is {1}", _sum, fiveDigitBinarySumOfSerialNumber);
    }

    void CompressAndShowDemonDataOnPlate()
    {
        // Demon data is its ID and starting Location
        // It ends up as 2 character per Demon, first being ID and second being encrypted position

        // Decompression:
        // First character is the uncompressed ID of the Demon
        // Take the third character, decompress into a 5-bit number
        // First 3 bits is the column index, last 2 is the row index

        // Compression then becomes:
        // Location as described in the Preset Playfield Puzzle is combined into a 5-bit binary number
        // Convert that binary number to a character using the dictionary


        string _compressedDemonData = "";
        string _binaryDemonLocation = string.Empty;
        string _encryptedLocationCharacter = string.Empty;


        for (int i = 0; i < selectedPresetPuzzle.demonIds.Length; i ++)
        {
            // 3 Demons per line, so the 4th data starts at a new line
            if (i == 3) { _compressedDemonData += "\n"; }
            // Else, add a space for readability
            else if (i > 0) { _compressedDemonData += " "; }


            // Add the Demon's ID
            _compressedDemonData += selectedPresetPuzzle.demonIds[i];

            summoningModule.ModuleLog(moduleId, "Next Demon has ID {0}, it is a {1}.",
                selectedPresetPuzzle.demonIds[i], GetDemonDebugFriendlyName(selectedPresetPuzzle.demonIds[i]));




            // Take the location and convert it to a 5-bit binary puzzle in format cccrr for Column and Row
            // "numbersAsBinary" contains the first 5-bit digits
            // Column
            _binaryDemonLocation = numbersAsBinary[CharToInt(selectedPresetPuzzle.demonLocations[i][0])].Substring(2, 3);
            // Row
            _binaryDemonLocation += numbersAsBinary[CharToInt(selectedPresetPuzzle.demonLocations[i][1])].Substring(3, 2);
            
            // Convert to a single character
            ConversionTableBelow.TryGetValue(_binaryDemonLocation, out _encryptedLocationCharacter);

            // Add it to the Demon Data
            _compressedDemonData += _encryptedLocationCharacter;

            // Log
            summoningModule.ModuleLog(moduleId, "It has Location {0}. This is compressed as binary {1} and converted to character {2}",
                selectedPresetPuzzle.demonLocations[i][0] + "-" + selectedPresetPuzzle.demonLocations[i][1], _binaryDemonLocation, _encryptedLocationCharacter);
        }


        topPlateDemonInscription.text = _compressedDemonData;
    }

    void CompressAndShowPixieDataOnPlate()
    {
        // Pixie data is its ID only
        // It ends up as 1 character per Pixie

        // There is no Compression/Decompression, it's just a string of numbers


        string _compressedPixieData = "";


        for (int i = 0; i < selectedPresetPuzzle.pixieIds.Length; i++)
        {
            // 4 Pixies per line, so the 5th data starts at a new line
            if (i == 4) { _compressedPixieData += "\n"; }
            // Else, add a space for readability
            else if (i > 0) { _compressedPixieData += " "; }


            // Add it to the Pixie Data
            _compressedPixieData += selectedPresetPuzzle.pixieIds[i];

            summoningModule.ModuleLog(moduleId, "Next Pixie has ID {0}, it is a {1}.",
                selectedPresetPuzzle.pixieIds[i], GetPixieDebugFriendlyName(selectedPresetPuzzle.pixieIds[i]));
        }


        bottomPlatePixieInscription.text = _compressedPixieData;
    }

    void ShowVoidCellLocationsOnPlate()
    {
        middlePlateVoidInscription.text = selectedPresetPuzzle.voidCellLocations.Join();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Debug & Validation & Logs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Debug Method that verifies the data integrity of ALL Preset Playfield Puzzles to ensure they are valid. </summary>
    void VerifyPresetPlayfieldPuzzleIntegrity()
    {
        summoningModule.ModuleLog(moduleId, "Starting verification of Playfield Puzzles Integrity...");

        List<int> allEncounteredIds = new List<int>();
        List<string> allEncounteredDemonLocations = new List<string>();
        List<string> allEncounteredVoidLocations = new List<string>();
        List<string> allIndendedSolutionLocations = new List<string>();

        foreach (PresetPlayfieldPuzzle _puzzle in presetPuzzleLists)
        {

            selectedPresetPuzzle = _puzzle;

            summoningModule.ModuleLog(moduleId, "Starting verification of Puzzle ID: {0}...", _puzzle.puzzleId);

            // Start of Puzzle ID Tests
            // Test 1: All IDs must be Unique
            if (allEncounteredIds.Contains(_puzzle.puzzleId))
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error 01: !i!i!i!i! Found non-unique Playfield Puzzle ID: {0}", _puzzle.puzzleId);
            }
            allEncounteredIds.Add(_puzzle.puzzleId);


            // Test 2: All IDs must be exclusively Positive
            if (_puzzle.puzzleId < 0)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 02: !i!i!i!i! Found non-positive Playfield Puzzle ID: {0}", _puzzle.puzzleId);
            }
            // End of Puzzle ID Tests


            // Start of Demon ID Tests
            // Test 3: Demon ID Array must not be empty
            if (_puzzle.demonIds.Length == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 03: !i!i!i!i! Found empty Demon ID Array for Puzzle ID {0}", _puzzle.puzzleId);
            }


            // Test 4: Demon ID and Demon Location Array must have the same length
            if (_puzzle.demonIds.Length != _puzzle.demonLocations.Length)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 04: !i!i!i!i! Found non-equal length between Demon IDs {1} and Demon Locations {2} for Puzzle ID {0}",
                    _puzzle.puzzleId, _puzzle.demonIds.Length, _puzzle.demonLocations.Length);
            }


            foreach (int _demonId in _puzzle.demonIds)
            {
                // Test 5: Demon ID Array values must be within 0 and 8
                if (_demonId < 0 || _demonId > 8)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 05: !i!i!i!i! Found Demon ID {1} outside of the range [0-8] for Puzzle ID {0}", _puzzle.puzzleId, _demonId);
                }
            }
            // End of Demon ID Tests


            // Start of Demon Location Tests
            allEncounteredDemonLocations.Clear();

            foreach (string _demonLocation in  _puzzle.demonLocations)
            {
                // Test 6: Demon Locations must be A1-H4 only
                if (_demonLocation.Length != 2)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 06-1: !i!i!i!i! Found non-2-length Demon Location {1} for Puzzle ID {0}", _puzzle.puzzleId, _demonLocation);
                }
                // First character is a number that is between 0 and 7 inclusive
                else if (CharToInt(_demonLocation[0]) < 0 || CharToInt(_demonLocation[0]) > 7)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 06-2: !i!i!i!i! Found Demon Location {1} whose first character is not a number inside range [0-7] for Puzzle ID {0}", _puzzle.puzzleId, _demonLocation);
                }
                // Second character is a number that is between 0 and 3 inclusive
                else if (CharToInt(_demonLocation[1]) < 0 || CharToInt(_demonLocation[1]) > 3)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 06-4: !i!i!i!i! Found Demon Location {1} whose second character is not a number inside range [0-3] for Puzzle ID {0}", _puzzle.puzzleId, _demonLocation);
                }
                // Test 21: Demon Locations cannot be on the leftmost column
                else if (CharToInt(_demonLocation[0]) < 1)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 21: !i!i!i!i! Found Demon Location {1} who spawns on the leftmost column for Puzzle ID {0}", _puzzle.puzzleId, _demonLocation);
                }

                
                // Test 7: Demon Locations must all be unique
                if (allEncounteredDemonLocations.Contains(_demonLocation))
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 07: !i!i!i!i! Found Demon Location {1} which is a duplicate within Puzzle ID {0}", _puzzle.puzzleId, _demonLocation);
                }

                allEncounteredDemonLocations.Add(_demonLocation);
            }
            // End of Demon Location Tests



            // Start of Pixie ID Tests
            // Test 8: Pixie IDs array must not be empty
            if (_puzzle.pixieIds.Length == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 08: !i!i!i!i! Found empty Pixie ID Array for Puzzle ID {0}", _puzzle.puzzleId);
            }


            // Test 9: Pixie IDs must be within 0 and 9
            foreach (int _pixieID in _puzzle.pixieIds)
            {
                if (_pixieID < 0 || _pixieID > 9)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 09: !i!i!i!i! Found Pixie ID {1} outside of [0-3] for Puzzle ID {0}", _puzzle.puzzleId, _pixieID);
                }
            }
            // End of Pixie ID Tests




            // Start of Void Cell Tests
            // Test 10: Void Cell Locations array must not be empty
            if (_puzzle.voidCellLocations.Length == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 10: !i!i!i!i! Found empty Void Cell Locations Array for Puzzle ID {0}", _puzzle.puzzleId);
            }

            
            
            allEncounteredVoidLocations.Clear();
            foreach (string _voidLocation  in _puzzle.voidCellLocations)
            {
                // Test 11: Void Cell Locations must all be unique
                if (allEncounteredVoidLocations.Contains(_voidLocation))
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 11: !i!i!i!i! Found Void Cell Location {1} which is a duplicate within Puzzle ID {0}", _puzzle.puzzleId, _voidLocation);
                }
                allEncounteredVoidLocations.Add(_voidLocation);


                // Test 12: Void Cell Locations must all be A1-H4
                if (_voidLocation.Length != 2)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 12-1: !i!i!i!i! Found non-2-length Void Cell Location {1} for Puzzle ID {0}", _puzzle.puzzleId, _voidLocation);
                }
                // First character is a number that is between 0 and 7 inclusive
                else if (CharToInt(_voidLocation[0]) < 0 || CharToInt(_voidLocation[0]) > 7)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 12-2: !i!i!i!i! Found Void Cell Location {1} whose first character is not a number inside range [0-7] for Puzzle ID {0}", _puzzle.puzzleId, _voidLocation);
                }
                // Second character is a number that is between 0 and 3 inclusive
                else if (CharToInt(_voidLocation[1]) < 0 || CharToInt(_voidLocation[1]) > 3)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 12-4: !i!i!i!i! Found Void Cell Location {1} whose second character is not a number inside range [0-3] for Puzzle ID {0}", _puzzle.puzzleId, _voidLocation);
                }


                // Test 13: Void Cell Locations must not be the same as Demon Locations
                if (_puzzle.demonLocations.Contains(_voidLocation))
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 13: !i!i!i!i! Found Void Cell Location {1} who's already the location of a Demon for Puzzle ID {0}", _puzzle.puzzleId, _voidLocation);
                }
            }
            // End of Void Cell Locations



            // Start of Intended Solution Tests
            // Test 14: Void Cell Locations array must not be empty
            if (_puzzle.intendedPixiePlacementSolution.Length == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 14: !i!i!i!i! Found empty Intended Solution Array for Puzzle ID {0}", _puzzle.puzzleId);
            }


            // Test 19: Intended Solution and Pixie ID Array must have the same length
            if (_puzzle.intendedPixiePlacementSolution.Length != _puzzle.pixieIds.Length)
            {
                summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 19: !i!i!i!i! Found non-equal length between Pixie IDs {1} and Intended Solution Locations {2} for Puzzle ID {0}",
                    _puzzle.puzzleId, _puzzle.pixieIds.Length, _puzzle.intendedPixiePlacementSolution.Length);
            }



            allIndendedSolutionLocations.Clear();
            foreach (string _intendedSolutions in _puzzle.intendedPixiePlacementSolution)
            {
                // Test 15: Intended Solution Locations must all be unique
                if (allIndendedSolutionLocations.Contains(_intendedSolutions))
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 15: !i!i!i!i! Found Intended Solution Location {1} which is a duplicate within Puzzle ID {0}", _puzzle.puzzleId, _intendedSolutions);
                }
                allIndendedSolutionLocations.Add(_intendedSolutions);


                // Test 16: Intended Solution Locations must all be A1-H4
                if (_intendedSolutions.Length != 2)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 16-1: !i!i!i!i! Found non-2-length Intended Solution Location {1} for Puzzle ID {0}", _puzzle.puzzleId, _intendedSolutions);
                }
                // First character is a number that is between 0 and 7 inclusive
                else if (CharToInt(_intendedSolutions[0]) < 0 || CharToInt(_intendedSolutions[0]) > 7)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 16-2: !i!i!i!i! Found Intended Solution Location {1} whose first character is not a number inside range [0-7] for Puzzle ID {0}", _puzzle.puzzleId, _intendedSolutions);
                }
                // Second character is a number that is between 0 and 3 inclusive
                else if (CharToInt(_intendedSolutions[1]) < 0 || CharToInt(_intendedSolutions[1]) > 3)
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 16-4: !i!i!i!i! Found Intended Solution Location {1} whose second character is not a number inside range [0-3] for Puzzle ID {0}", _puzzle.puzzleId, _intendedSolutions);
                }


                // Test 17: Intended Solution Locations must not be the same as Demon Locations
                if (_puzzle.demonLocations.Contains(_intendedSolutions))
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 17: !i!i!i!i! Found Intended Solution Location {1} who's already the location of a Demon for Puzzle ID {0}", _puzzle.puzzleId, _intendedSolutions);
                }

                // Test 18: Intended Solution Locations must not be the same as Void Locations
                if (_puzzle.voidCellLocations.Contains(_intendedSolutions))
                {
                    summoningModule.ModuleLogError(moduleId, "Puzzle List Error Code 18: !i!i!i!i! Found Intended Solution Location {1} who's already the location of a Void Cell for Puzzle ID {0}", _puzzle.puzzleId, _intendedSolutions);
                }
            }
            // End of Void Cell Locations


            // Test 20: Intended Solution must be valid
            InitializePlayfieldFromPresetPuzzle(true, _puzzle);
            isInIntegrityVisualizationMode = true;
            SimulatePlay();

            summoningModule.ModuleLog(moduleId, "Puzzle ID {0} finished all of its tests!!", _puzzle.puzzleId);
        }

        summoningModule.ModuleLog(moduleId, "Verifications done!!");
    }

    void LogPlayfield()
    {
        // Remember that Creature Number can change over time as Basels might be summoned!
        summoningModule.ModuleLog(moduleId, "Here is the current state of the Playfield:");

        string _lineToLog;
        for (int _row = 0; _row < 4; _row ++)
        {
            _lineToLog = string.Empty;

            for (int _col = 0; _col < 8;  _col ++)
            {
                _lineToLog += currentPlayfield.playfieldRepresentation[8 * _row + _col];
                if (_col != 7)
                {
                    _lineToLog += " | ";
                }
            }

            summoningModule.ModuleLog(moduleId, _lineToLog);
        }


        summoningModule.ModuleLog(moduleId, "0 is empty, -1 is Void, Numbers >0 represent a Creature Number:");

        // Loop through all Demons
        foreach (Demon _demon in currentPlayfield.demons)
        {
            summoningModule.ModuleLog(moduleId, "{0} represents the {1} with current Health {2} and Damage {3}.",
                _demon.demonPlayfieldNumber, _demon.debugFriendlyName, _demon.currentHealth, _demon.currentDamage);
        }
        foreach (Pixie _pixie in currentPlayfield.pixies)
        {
            summoningModule.ModuleLog(moduleId, "{0} represents the {1} with current Health {2} and Damage {3}.",
            _pixie.pixiePlayfieldNumber, _pixie.debugFriendlyName, _pixie.currentHealth, _pixie.currentDamage);
        }
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        summoningModule.ModuleLog(moduleId, "Received the Twitch Plays command “{0}”", command);

        if (command.Length == 0)
        {
            yield return "sendtochaterror {0} Received empty command!";
            yield break;
        }

        // Put to lowercase and remove spaces
        command = command.ToLowerInvariant().Replace(" ", "").Replace(",","");

        // Pressing PIXIE on the center of the plate
        if (command == "p" || command == "pixie")
        {
            casingPressableButton.OnInteract();
            yield return "sendtochat {0} Successfully press PIXIE and reset the board. You are back in the top-left corner.";
            yield break;
        }


        foreach (char _individualCommand in command)
        {
            yield return new WaitForSeconds(0.1f);

            switch (_individualCommand)
            {
                // Button Type Indicator:
                // 0123 is Up Down Left Right Movement to coincide with MovementDirection enum
                // 4 is Center (submit)

                case 'u':
                    platePressableButtons[0].OnInteract();
                    break;              
                                        
                case 'd':
                    platePressableButtons[1].OnInteract();
                    break;              

                case 'l':
                    platePressableButtons[2].OnInteract();
                    break;

                case 'r':
                    platePressableButtons[3].OnInteract();
                    break;


                // Accept both "c" and "s" as Center and Submit
                case 'c':
                    platePressableButtons[4].OnInteract();
                    break;

                case 's':
                    platePressableButtons[4].OnInteract();
                    break;

                default:
                    string _stringToSend = string.Format("sendtochaterror {0} Received unknown character: “{1}”. To reset send the command “pixie”. You currently are in {2} and have placed {3} Pixies.",
                        "{0}", _individualCommand, ConvertGridIndexToPresetPuzzleLocation(currentPlayerPointerLocation), currentPixieIndexToPlace);
                    yield return _stringToSend;
                    yield break;
            }
        }

        yield break;
    }
        

    public override IEnumerator TwitchHandleForcedSolve()
    {
        // There could be a way to write an auto-solver that clears the Playfield and then
        // depending on the selected PresetPuzzle moves around and places Pixies... But I can't be bothered at this point in time :D
        ModuleShouldSolve();

        yield break;
    }

}


/*
=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
           THE SHADOW REALM
=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    Here lies code that was removed from nerfs and changes.
    It is still kept as a note on how to code them if I need those mechanics for future modules;
    and for history's sake too.





    // DEMON ID ENCRYPTION //

    /// <summary> Dictionary to convert an integer representing an UTC Offset to a string containing Country Code Pairs </summary>
    Dictionary<int, string> UtcOffsetToCountryCodes = new Dictionary<int, string>()
    { {0, "IEIS"}, {1, "ALADBEDKLI"}, {2, "BGGRCYRO"}, {3, "UGTRQA"}, {4, "LCGDDMTT"}, {5, "JMUZMVBS"}, {6, "CRNIBD"}, {7, "KHTHVN"}, {8, "CNMOPHSG"}};

    // Demon data is its ID and starting Location
    // It ends up as 3 character per Demon, first two being ID and last being position

    // Decompression:
    // First two character, get the country shown by the ISO two-letter code
    // Take the UTC time offset of that country, removing minus sign
    // This is the ID of the Demon
    // Take the third character, decompress into a 5-bit number
    // Add all Serial Number digits together, convert to binary and take only the 5 least significant digits
    // XOR the two 5-bit numbers together.
    // First 3 bits is the column index, last 2 is the row index

    // Compression then becomes:
    // ID of the Demon converted to a two-letter code based on a curated dictionary of possibilities
    // Location as described in the Preset Playfield Puzzle is combined into a 5-bit binary number
    // Sum of SN Digits is used to XOR the binary number
    // Convert that binary number to a character using the dictionary
   
    string _compressedDemonData = "";
    string _scratchStringValue = string.Empty;


    for (int i = 0; i < selectedPresetPuzzle.demonIds.Length; i ++)
    {
        // 3 Demons per line, so the 4th data starts at a new line
        if (i == 3) { _compressedDemonData += "\n"; }



        // Convert the Demon ID to a country code based on the curated dictionary
        UtcOffsetToCountryCodes.TryGetValue(selectedPresetPuzzle.demonIds[i], out _scratchStringValue);

        // We get multiple country codes, only keep one of them (a pair of 2 characters)
        // Initial Index is 0, 2, 4, 6, etc. So that's Random [0; x] * 2  where x is the number of Pairs (because inclusive)
        int _countryCodeStartingIndex = UnityEngine.Random.Range(0, _scratchStringValue.Length / 2) * 2;
        _scratchStringValue = _scratchStringValue.Substring(_countryCodeStartingIndex, 2);
        _compressedDemonData += _scratchStringValue;

        summoningModule.ModuleLog(moduleId, "Next Demon has ID {0}, it is a {1}. Compressed into a Country Code {2}",
            selectedPresetPuzzle.demonIds[i], GetDemonDebugFriendlyName(selectedPresetPuzzle.demonIds[i]), _scratchStringValue);




        // Take the location and convert it to a 5-bit binary puzzle in format cccrr for Column and Row
        // "numbersAsBinary" contains the first 5-bit digits
        // Column
        _scratchStringValue = numbersAsBinary[CharToInt(selectedPresetPuzzle.demonLocations[i][0])].Substring(2, 3);
        // Row
        _scratchStringValue += numbersAsBinary[CharToInt(selectedPresetPuzzle.demonLocations[i][1])].Substring(3, 2);
        summoningModule.ModuleLog(moduleId, "It has Location {0}. This is compressed as binary {1}",
            selectedPresetPuzzle.demonLocations[i][0] + "-" + selectedPresetPuzzle.demonLocations[i][1], _scratchStringValue);

        // XOR with the SN Digits sum
        for (int _bit = 0; _bit < 5; _bit++)
        {
            if (_scratchStringValue[_bit] == fiveDigitBinarySumOfSerialNumber[_bit])
            {
                // Instead of overwriting, add the bits to the end, and we'll only keep the last 5 bits after the XOR
                _scratchStringValue += '0';
            }
            else
            {
                _scratchStringValue += '1';
            }
        }
        // Only get the last 5 bits
        _scratchStringValue = _scratchStringValue.Substring(5, 5);

        // Convert to a single character
        ConversionTableBelow.TryGetValue(_scratchStringValue, out _scratchStringValue);

        // Add it to the Demon Data
        _compressedDemonData += _scratchStringValue;

        summoningModule.ModuleLog(moduleId, "After XOR with Sum of Serial Number Digits, this converts to character {0}", _scratchStringValue);
    }





    // PIXIE ID ENCRYPTION //
        // Pixie data is its ID only
        // It ends up as 1 character per Pixie

        // Decompression:
        // Convert character into 5-digit binary using The Table Below
        // Add all Serial Number digits together, convert to binary and take only the 5 least significant digits
        // XOR the two 5-bit numbers together.
        // 0s become dots, 1s become dashes
        // Convert to a number using Morse Code

        // Compression then becomes:
        // ID of the Pixie is transformed to Morse Code
        // Dots are changed to 0, dashes to 1
        // XOR with the binary SN sum
        // Convert into character


        string _compressedPixieData = "";
        string _scratchStringValue = string.Empty;


        for (int i = 0; i < selectedPresetPuzzle.pixieIds.Length; i++)
        {
            // 6 Pixies per line, so the 7th data starts at a new line
            if (i == 6) { _compressedPixieData += "\n"; }



            // Convert the Pixie ID to morse
            _scratchStringValue = morseCodeNumbers[selectedPresetPuzzle.pixieIds[i]];

            // Convert Dots to 0 and Dashes to 1
            _scratchStringValue = _scratchStringValue.Replace('.', '0').Replace('-', '1');

            summoningModule.ModuleLog(moduleId, "Next Pixie has ID {0}, it is a {1}. Compressed into Binary {2} after Morse Transformation",
                selectedPresetPuzzle.pixieIds[i], GetPixieDebugFriendlyName(selectedPresetPuzzle.pixieIds[i]), _scratchStringValue);

            // XOR with the SN Digits sum
            for (int _bit = 0; _bit < 5; _bit++)
            {
                if (_scratchStringValue[_bit] == fiveDigitBinarySumOfSerialNumber[_bit])
                {
                    // Instead of overwriting, add the bits to the end, and we'll only keep the last 5 bits after the XOR
                    _scratchStringValue += '0';
                }
                else
                {
                    _scratchStringValue += '1';
                }
            }
            // Only get the last 5 bits
            _scratchStringValue = _scratchStringValue.Substring(5, 5);

            // Convert to a single character
            ConversionTableBelow.TryGetValue(_scratchStringValue, out _scratchStringValue);

            // Add it to the Demon Data
            _compressedPixieData += _scratchStringValue;

            summoningModule.ModuleLog(moduleId, "After XOR with Sum of Serial Number Digits, this converts to character {0}", _scratchStringValue);
        }


        bottomPlatePixieInscription.text = _compressedPixieData;

 */