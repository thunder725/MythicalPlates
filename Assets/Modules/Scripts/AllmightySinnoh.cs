using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class AllmightySinnoh : SummoningModule {

    /// <summary>
    /// Plates are ordered in the same order as the documentation .txt:
    /// BLANK - FIST - SKY - TOXIC - EARTH - STONE - INSECT - SPOOKY - FLAME
    /// SPLASH - IRON - MEADOW - ZAP - MIND - ICICLE - DRACO - DREAD - PIXIE
    /// </summary>
    [SerializeField] GameObject[] allPlatesPrefabs = new GameObject[18];

    enum Plates { Blank, Fist, Sky, Toxic, Earth, Stone, Insect, Spooky, Flame, Splash, Iron, Meadow, Zap, Mind, Icicle, Draco, Dread, Pixie};
    string[] misspelledPlateNames = new string[18]{
        "BLAMK", "FISP", "SKIE", "TOXIK", "EARF", "SLONE", "INSEKT", "SPOOK", "FLOOM",
        "SPLISH", "ERON", "MEABOW", "ZAD", "MIMD", "ICYCLE", "DRAGO", "DRED", "RIXIE"
    };

    int numberOfPlatesSolved;


    // Visual Data
    [SerializeField] float platesRotationSpeed, platesRotationCircleRadius, visualPlatesScaleFactor;
    [SerializeField] float platesWaveSpeed;
    [SerializeField][Range(0, 1)] float timePlateSpeed;
    [SerializeField] Transform RotationCenter;
    [SerializeField] Transform SolvablePlateSpawnPoint;
    GameObject[] visualPlatesParents = new GameObject[18];
    GameObject[] visualPlates = new GameObject[18];
    Vector3 previousFrameTimePlateLocation;
    [SerializeField] TextMesh individualPlateNameText;
    [SerializeField] AudioClip[] platePressedSounds;


    // Marks
    int initialTimeMark, initialSpaceMark, initialAntimaterMark;
    int finalTimeMark, finalSpaceMark, finalAntimaterMark;
    int markToPress;


    // Universal Logging Data
    static int moduleIdCounter = 1;
    int moduleId;

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Vanilla Unity Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Buttons gathering and GetComponents
    protected override void Awake()
    {
        base.Awake();

        moduleId = moduleIdCounter++;

        EmptyPlateName();
        InitializeAllmightySinnohTwitchHelpMessage();
    }

    protected override void Start()
    {
        base.Start();

        SpawnEighteenVisualPlates();
    }


    protected override void Update()
    {
        base.Update();

        RotatePlatesOnUpdate();
        LagTimePlateBehind();


        // Send Updates to the summoned Plate
        if (currentSummonedPlateScript != null)
        {
            currentSummonedPlateScript.UpdateModule();
        }
    }

    protected override void OnModuleActivate()
    {
        base.OnModuleActivate();

        InitializePuzzle();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Interaction & Solvable Plate Spawning
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Called when one of the 18 Visual Plates is pressed </summary>
    void PressedMarkedPlate(int plateIndex)
    {
        if (markToPress > 2)
        { return; }


        casingPressableButton.AddInteractionPunch();
        PlayPlatePressSound();


        AllmightySinnohModuleLog(moduleId, "Pressed {0} Plate for Mark of {1} submission.",
            GetPlateNameFromIndex(plateIndex), markToPress == 0 ? "Time" : markToPress == 1 ? "Space" : "Antimatter");

        int expectedPlate = 0;

        switch (markToPress)
        {
            case 0:
                expectedPlate = finalTimeMark;
                break;

            case 1:
                expectedPlate = finalSpaceMark;
                break;

            case 2:
                expectedPlate = finalAntimaterMark;
                break;
        }


        if (plateIndex == expectedPlate)
        { 
            PressedValidMarkedPlate(); 
        }
        else
        { 
            PressedInvalidMarkedPlate(expectedPlate); 
        }
    }

    void PressedValidMarkedPlate()
    {
        AllmightySinnohModuleLog(moduleId, "That is valid.");
        markToPress++;

        if (markToPress == 3)
        {
            RemoveVisualPlateSelectables();
            SpawnSolvablePlate(finalTimeMark);
        }
    }

    void PressedInvalidMarkedPlate(int expectedPlate)
    {
        AllmightySinnohModuleLog(moduleId, "That is invalid. Expected {0} Plate.", GetPlateNameFromIndex(expectedPlate));
        ReceiveStrike();
    }

    /// <summary> Remove the buttons from the Visual Plates to prevent any issue (they do cause issues) </summary>
    void RemoveVisualPlateSelectables()
    {
        // Remove the KMSelectable, Highlight and Collider
        // Colliders prevent the press of the SINNOH casing button, which isn't really fun
        for (int i = 0; i < 18; i ++)
        {
            Destroy(visualPlatesParents[i].GetComponent<KMSelectable>());
            Destroy(visualPlatesParents[i].GetComponent<KMHighlightable>());
            Destroy(visualPlatesParents[i].GetComponent<BoxCollider>());
        }

        // Update the data for this Selectable
        // It's gonna be updated at soon as a Plate is spawned (so its children is just CasingPressableButton + The Plate's Buttons) but still
        // Better safe than sorrry
        moduleSelectable.Children = new KMSelectable[1] { casingPressableButton };
        moduleSelectable.UpdateChildren();
    }


    void PlayPlatePressSound()
    {
        PlaySound(platePressedSounds.PickRandom());
    }



    void SpawnSolvablePlate(int plateIndex)
    {
        if (currentSummonedPlateObject != null)
        {
            DestroyPreviousSolvablePlate();
        }

        AllmightySinnohModuleLog(moduleId, "Summoning next Plate: {0} Plate.", GetPlateNameFromIndex(plateIndex));

        // "Instantiate" summons in World Space and THEN attaches, locations are NOT in localspace contrary to what I thought
        currentSummonedPlateObject = Instantiate(allPlatesPrefabs[plateIndex], SolvablePlateSpawnPoint.position, SolvablePlateSpawnPoint.rotation, SolvablePlateSpawnPoint);
        currentSummonedPlateScript = currentSummonedPlateObject.GetComponent<PlateBase>();

        // Initialize the Summoned Plate Module
        TransmitInformationToPlate();

        // We receive its buttons in its Awake
        currentSummonedPlateScript.InitializeModuleAwake();

        // It generates its puzzle in Start
        currentSummonedPlateScript.InitializeModuleStart();

        // Now wait for reception of the Solve!
    }

    void DestroyPreviousSolvablePlate()
    {
        Destroy(currentSummonedPlateObject);

        currentSummonedPlateObject = null;
        currentSummonedPlateScript = null;
    }





    /// <summary> Solvable Plates will initialize their own Twitch Help Message on spawn;
    /// but Allmighty Sinnoh has its own beforehand! </summary>
    void InitializeAllmightySinnohTwitchHelpMessage()
    {
        ReceiveTwitchHelpMessage("Press the three Marked Plates using “!{0} Submit Meadow Iron Pixie”. Show the 18 names using “!{0} shownames”. Wiggle the bomb using “!{0} wiggle”.");
    }

    public override void ReceiveSolve()
    {
        // This doesn't outright solve Allmighty Sinnoh since it does need to solve 3 of them in a row.
        numberOfPlatesSolved++;

        switch (numberOfPlatesSolved)
        {
            case 0:
                AllmightySinnohModuleLogError(moduleId, "Received Solve but numberOfPlatesSolved is still at 0! Please report this to thunder725");
                break;

            case 1:
                AllmightySinnohModuleLog(moduleId, "Received solve from the summoned {0}, Marked by Time. Summoning the Space-Marked one next.", currentSummonedPlateScript.fullPlateName);
                SpawnSolvablePlate(finalSpaceMark);
                break;

            case 2:
                AllmightySinnohModuleLog(moduleId, "Received solve from the summoned {0}, Marked by Space. Summoning the Antimatter-Marked one next.", currentSummonedPlateScript.fullPlateName);
                SpawnSolvablePlate(finalAntimaterMark);
                break;

            case 3:
                AllmightySinnohModuleLog(moduleId, "Received solve from the summoned {0}, Marked by Antimatter. All Plates have been solved. Solving Module.", currentSummonedPlateScript.fullPlateName);
                DestroyPreviousSolvablePlate();
                SolveAllmightySinnoh();
                break;
        }
    }

    public override void ReceiveStrike()
    {
        AllmightySinnohModuleLog(currentSummonedPlateScript.moduleId, "!! STRIKE !!");
        thisModule.HandleStrike();
    }

    void SolveAllmightySinnoh()
    {
        thisModule.HandlePass();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Visual Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void SpawnEighteenVisualPlates()
    {
        // Prepare memory region for future variables
        KMSelectable[] alreadyPresentPlateSelectables;
        BoxCollider alreadyPresentPlateBlockerCollider;
        BoxCollider _addedBoxCollider;
        KMSelectable _addedPlateSelectable;
        KMHighlightable _addedHighlightable;

        KMSelectable[] eighteenPlateSelectables = new KMSelectable[18];

        // Prepare constant data
        Vector3 _boxColliderSize = new Vector3(0.1f, 0.01f, 0.15f);

        // For all 18 plates
        for (int i = 0; i < 18; i ++)
        {
            // We cannot just attach a collider, highlightable and selectable to the Plate
            // Because when the plate rotates, its Selectable is not selectable from the back!
            // So we need Plates to have a parent that contains the Selectable & Hitbox
            // And this parent has the Plate and Highlightable as a child
            visualPlatesParents[i] = new GameObject();

            // Attach that parent to this module
            visualPlatesParents[i].transform.parent = summonedPlateParentTransform.transform;
            visualPlatesParents[i].transform.localScale *= visualPlatesScaleFactor;



            // Spawn the Plate then Attaches it
            visualPlates[i] = Instantiate(allPlatesPrefabs[i], visualPlatesParents[i].transform.position, visualPlatesParents[i].transform.rotation, visualPlatesParents[i].transform);

            // Remove all of its selectables' gameobjects,
            // so it becomes just a visual plate without any Buttons
            alreadyPresentPlateSelectables = visualPlates[i].GetComponentsInChildren<KMSelectable>();
            foreach (KMSelectable _plateButton in alreadyPresentPlateSelectables)
            {
                Destroy(_plateButton.gameObject);
            }

            // Remove that Plate's already existing collider, if one exists
            // If it exists, that is the "Plate Collision Blocker", an invisible non-Selectable
            // collider here to block the mouse selection of the casing button. Not all Plates have it.
            alreadyPresentPlateBlockerCollider = visualPlates[i].GetComponentInChildren<BoxCollider>();
            if (alreadyPresentPlateBlockerCollider != null )
            {
                Destroy(alreadyPresentPlateBlockerCollider.gameObject);
            }

            // Remove that Plate's script
            Destroy(visualPlates[i].GetComponent<PlateBase>());



            // Attach a box collider with correct sizes TO THE PARENT
            _addedBoxCollider = visualPlatesParents[i].AddComponent<BoxCollider>();
            _addedBoxCollider.size = _boxColliderSize;
            _addedBoxCollider.center = Vector3.zero;

            // Attach a KM Highlightable to the Plate
            _addedHighlightable = visualPlates[i].AddComponent<KMHighlightable>();



            // Attach a KM Selectable TO THE PARENT
            _addedPlateSelectable = visualPlatesParents[i].AddComponent<KMSelectable>();

            // Assign to it the highlight and collider
            _addedPlateSelectable.Highlight = _addedHighlightable;
            _addedPlateSelectable.SelectableColliders = new Collider[1] { _addedBoxCollider };

            // Assign to it its parent (Allmighty Sinnoh selectable)
            _addedPlateSelectable.Parent = moduleSelectable;

            // Initialize the selectable's "Children" array; otherwise it causes a NullReferenceException
            _addedPlateSelectable.Children = new KMSelectable[0];
            _addedPlateSelectable.UpdateChildren();

            // Record that Selectable in an array
            eighteenPlateSelectables[i] = _addedPlateSelectable;
        }


        // Use that Array of all Plate Selectable and add them as children of the main selectable
        // Do not forget to keep the Casing Pressable Button (SINNOH)
        List<KMSelectable> pressableChildren = new List<KMSelectable>() { casingPressableButton };
        pressableChildren.AddRange(eighteenPlateSelectables);
        moduleSelectable.Children = pressableChildren.ToArray();
        moduleSelectable.UpdateChildren();


        // Add interactions to the plates (Highlight name show + Mark Press)
        // As always, can't just use a for loop because the delegate would keep the latest value of i
        // So everything would then ask for ShowPlateName(18)
        foreach (KMSelectable _plateSelectable in eighteenPlateSelectables)
        {
            _plateSelectable.OnHighlight += delegate () { ShowPlateName(Array.IndexOf(eighteenPlateSelectables, _plateSelectable)); };
            _plateSelectable.OnHighlightEnded += EmptyPlateName;
            _plateSelectable.OnInteract += delegate () { PressedMarkedPlate(Array.IndexOf(eighteenPlateSelectables, _plateSelectable)); return false; };
        }

    }

    void RotatePlatesOnUpdate()
    {
        float _normalizedRotationTime = (Time.time * platesRotationSpeed) % 1;
        float _localRotationTime;

        // Data used for rotating around the center
        Vector3 _localRotationCenter = RotationCenter.localPosition;
        Vector3 _localPlateParentPosition = Vector3.zero;
        Vector3 _localPlateParentRotation = Vector3.zero;
        Vector3 _localPlateRotation = Vector3.zero;

        const float eighteenthRadianRotation = 0.34906585f;
        const float eighteenthDegreesRotation = 20f;
        const float eighteenth = 0.05555555f;


        // Get a pseudo-normalized Time for the wave from 0 to 2
        // 0, 1 and 2 represent the same location around the circle, but this tracks up to
        // two rotations around the plates, since we want them to rotate 180° twice 
        float _normalizedWaveTime = (Time.time * platesWaveSpeed) % 2;
        float _normalizedLocalWaveTime;

        // Rotate each plate individually
        for (int i = 0; i < 18; i ++)
        {
            // Set the plate's Horizontal and Vertical locations
            // Offset the time by 1/18th of a circle turn (in Radians),
            // so that each plate is visually offset in a nice circle. Otherwise they'd all rotate at the same spot!
            _localRotationTime = 6.283185f * _normalizedRotationTime + i * eighteenthRadianRotation;
            _localPlateParentPosition.x = Mathf.Cos(_localRotationTime) * platesRotationCircleRadius;
            _localPlateParentPosition.z = Mathf.Sin(_localRotationTime) * platesRotationCircleRadius;


            // Set the plate's depth wiggle to avoid Z-Fighting
            _localPlateParentPosition.y = Mathf.Sin(i + i + i) * 0.002f;
            
            // Set the plate's yaw so they point inwards and everything is prettier
            _localPlateParentRotation.y = 90 - i * eighteenthDegreesRotation - (_normalizedRotationTime * 360);



            // Obtain a single 0-to-2 wave moving through the plates
            // The offset using "i * eighteenth" is so that every plate has its own personal "wave"
            // Code fixed thanks to arcorann, thanks to them!!
            _normalizedLocalWaveTime = (_normalizedWaveTime + i * eighteenth) % 2;

            if (i == initialSpaceMark)
            {
                _localPlateRotation.z = 0;
            }
            else
            {
                // Then, use the Floor of the Wave Time to get either 0 or 1
                // Which will keep the "default" rotation to either 0 or 180°
                // Then the Easing of the time MODULO 1 will handle the smooth rotation
                // Use a sharp easing function to flatten the parts close to 0 and 1 and make the transition as invisible as possible
                _localPlateRotation.z = 180 * (Mathf.Floor(_normalizedLocalWaveTime) + Easing.InOutExpo(_normalizedLocalWaveTime % 1, 0, 1, 1));
            }

            // Move the PlateParent
            // Rotate the Plate Parent's Yaw (since that's linked to position)
            // But individually rotate the Plate for the Wave
            visualPlatesParents[i].transform.localPosition = _localRotationCenter + _localPlateParentPosition;
            visualPlatesParents[i].transform.localEulerAngles = _localPlateParentRotation;
            visualPlates[i].transform.localEulerAngles = _localPlateRotation;
        }
    }

    void LagTimePlateBehind()
    {
        // Interp towards the goal of the "currentLocation"
        visualPlatesParents[initialTimeMark].transform.position = Vector3.Lerp(previousFrameTimePlateLocation, visualPlatesParents[initialTimeMark].transform.position, timePlateSpeed);

        // Save to know a "Previous Frame"
        previousFrameTimePlateLocation = visualPlatesParents[initialTimeMark].transform.position;
    }

    void ShowPlateName(int plateIndex)
    {
        individualPlateNameText.text = plateIndex == initialAntimaterMark ? misspelledPlateNames[plateIndex] : GetPlateNameFromIndex(plateIndex).ToUpper();
    }

    void EmptyPlateName()
    {
        individualPlateNameText.text = string.Empty;
    }

    string GetPlateNameFromIndex(int plateIndex)
    {
        return ((Plates)plateIndex).ToString();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        DetermineInitialMarks();
        ApplyMarkSteps();
    }

    void DetermineInitialMarks()
    {
        int[] _numbers = new int[18]{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }.Shuffle();
        initialTimeMark = _numbers[0];
        initialSpaceMark = _numbers[1];
        initialAntimaterMark = _numbers[2];

        AllmightySinnohModuleLog(moduleId, "Initial Time Mark is upon {0} Plate, Space Mark is upon {1}, Antimatter Mark is upon {2}",
            (Plates)initialTimeMark, (Plates)initialSpaceMark, (Plates)initialAntimaterMark);
    }

    void ApplyMarkSteps()
    {
        // Initialize "final" Marks
        finalTimeMark = initialTimeMark;
        finalSpaceMark = initialSpaceMark;
        finalAntimaterMark = initialAntimaterMark;


        // Gather Serial Number, Ports, Plates and Batteries
        string _bombSerialNumber = bombInfo.GetSerialNumber();

        bool _bombHasAaBatteries = bombInfo.GetBatteryCount(Battery.AA) > 0;
        bool _bombHasDBatteries = bombInfo.GetBatteryCount(Battery.D) > 0;

        IEnumerable<string> _bombIndicators = bombInfo.GetIndicators();
        int _bombNumberOfLits = bombInfo.GetOnIndicators().Count();
        int _bombNumberOfUnlits = bombInfo.GetOffIndicators().Count();

        IEnumerable<string> _bombPorts = bombInfo.GetPorts();



        // Step One
        // The Serial Number contains a Prime and a non-Prime Number
        if (Regex.IsMatch(_bombSerialNumber, "/[2357]/") && Regex.IsMatch(_bombSerialNumber, "/[014689]/"))
        {
            AllmightySinnohModuleLog(moduleId, "Step One should be applied.");
            MoveMarks(3, 12, 5);
        }


        // Step Two
        // An Initial Mark landed on Iron, Splash or Spooky Plates
        // Iron index is 10
        // Splash index is 9
        // Spooky index is 7
        int[] _ironSplashOrSpookyIndices = new int[3] { 10, 9, 7 };
        int[] _initialMarks = new int[3] { initialTimeMark, initialSpaceMark, initialSpaceMark };
        if (_ironSplashOrSpookyIndices.Intersect(_initialMarks).Count() > 0)
        {
            AllmightySinnohModuleLog(moduleId, "Step Two should be applied.");
            MoveMarks(8, 8, 7);
        }


        // Step Three
        // A Stereo RCA port is present
        if (_bombPorts.Contains("StereoRCA"))
        {
            AllmightySinnohModuleLog(moduleId, "Step Three should be applied.");
            MoveMarks(3, 10, 2);
        }


        // Step Four
        // An RJ-45 port is present
        if (_bombPorts.Contains("RJ45"))
        {
            AllmightySinnohModuleLog(moduleId, "Step Four should be applied.");
            MoveMarks(3, 1, 6);
        }


        // Step Five
        // A CLR indicator is present
        if (_bombIndicators.Contains("CLR"))
        {
            AllmightySinnohModuleLog(moduleId, "Step Five should be applied.");
            MoveMarks(5, 5, 5);
        }


        // Step Six
        // This module is vertically or horizontally aligned to the Timer
        // It is forced to be on the same side as it, because it can spawn Timer-dependant
        // Plates such as Zap or Iron

        // Get the Bomb (KMBomb is valid only if called within OnActivate, not within Start()!!)
        KMBomb bomb = transform.root.GetComponent<KMBomb>();

        if (bomb != null)
        {
            // From that, get the Timer
            TimerModule _timerScript = bomb.GetComponentInChildren<TimerModule>();

            if (_timerScript != null)
            {
                // Gather world locations of timer & this module
                Vector3 timerPos = _timerScript.transform.position;
                Vector3 modulePos = transform.position;

                // Dot product between this module's Forward (blue) and the vector to the Timer
                Vector3 toTimer = (timerPos - modulePos).normalized;
                float dot = Vector3.Dot(transform.forward, toTimer);
                AllmightySinnohModuleLog(moduleId, "timerPos = {0}; modulePos = {1}; vectorToTimer = {2}; transform.up = {3}; dot = {4}",
                    timerPos, modulePos, toTimer, transform.forward, dot);

                // That Dot Product will be 1 if the timer is above this module,
                // 0 if it is horizontally adjacent
                // -1 if it is below this module
                // Or some other place otherwise

                // Take the absolute value
                // Then round it to 3 decimal places, to avoid float imprecision
                dot = Mathf.Abs(dot);
                dot = Mathf.Round(dot * 1000) / 1000;

                // If 1, vertically aligned
                // If 0, horizontally aligned
                // If anything else, discard
                if (dot == 0)
                {
                    AllmightySinnohModuleLog(moduleId, "Step Six should be applied. This module is horizontally aligned with the timer.");
                    MoveMarks(15, 2, 5);
                }
                else if (dot == 1)
                {
                    AllmightySinnohModuleLog(moduleId, "Step Six should be applied. This module is vertically aligned with the timer.");
                    MoveMarks(15, 2, 5);
                }
            }
            else
            {
                AllmightySinnohModuleLogError(moduleId, "TIMER IS INVALID!! Please report this to thunder725");
            }
        }
        else
        {
            AllmightySinnohModuleLogError(moduleId, "BOMB IS INVALID!! Please report this to thunder725");
        }


        // Step Seven
        // There are more Lit indicators than Unlits
        if (_bombNumberOfLits > _bombNumberOfUnlits)
        {
            AllmightySinnohModuleLog(moduleId, "Step Seven should be applied.");
            MoveMarks(5, 4, 3);
        }


        // Step Eight
        // There is at least 1 AA battery and 1 D battery
        if (_bombHasAaBatteries && _bombHasDBatteries)
        {
            AllmightySinnohModuleLog(moduleId, "Step Eight should be applied.");
            MoveMarks(2, 6, 0);
        }


        // Step Nine
        // There are more Unlits than Lits
        if (_bombNumberOfLits < _bombNumberOfUnlits)
        {
            AllmightySinnohModuleLog(moduleId, "Step Nine should be applied.");
            MoveMarks(3, 4, 5);
        }


        // Step Ten
        // There are Mythical Plate modules
        if (bomb != null)
        {
            if (bomb.GetComponentInChildren<IndividualPlateModule>() != null)
            {
                AllmightySinnohModuleLog(moduleId, "Step Ten should be applied.");
                MoveMarks(4, 9, 3);
            }
        }
        else
        {
            AllmightySinnohModuleLogError(moduleId, "BOMB IS INVALID!! Please report this to thunder725");
        }
    }

    void MoveMarks(int TimeMovement, int SpaceMovement, int AntimatterMovement)
    {
        bool _uniqueOffsetNeeded = false;

        // Move the mark by the correct amount
        // With % 18 because it loops around
        finalTimeMark = (finalTimeMark + TimeMovement) % 18;

        // Offset it until it is unique
        while (finalTimeMark == finalSpaceMark ||  finalTimeMark == finalAntimaterMark)
        {
            _uniqueOffsetNeeded = true;
            finalTimeMark = (finalTimeMark + 1) % 18;
        }


        // Repeat for Space
        finalSpaceMark = (finalSpaceMark + SpaceMovement) % 18;
        while (finalSpaceMark == finalTimeMark || finalSpaceMark == finalAntimaterMark)
        {
            _uniqueOffsetNeeded = true;
            finalSpaceMark = (finalSpaceMark + 1) % 18;
        }


        // And for Antimatter
        finalAntimaterMark = (finalAntimaterMark + AntimatterMovement) % 18;
        while (finalAntimaterMark == finalTimeMark || finalAntimaterMark == finalSpaceMark)
        {
            _uniqueOffsetNeeded = true;
            finalAntimaterMark = (finalAntimaterMark + 1) % 18;
        }

        AllmightySinnohModuleLog(moduleId, "Time Mark ended on {0} Plate. Space Mark ended on {1} Plate. Antimatter Mark ended on {2} Plate.{3}",
            (Plates)finalTimeMark, (Plates)finalSpaceMark, (Plates)finalAntimaterMark, _uniqueOffsetNeeded ? " At least one Mark had to be offset once to avoid collisions." : "");
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //   Logging
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Reroute the module logs
    // But add something to know if a Plate is the one sending a message or not!
    public override void ModuleLog(int moduleId, string message, params object[] args)
    {
        string addendum = "";
        if (currentSummonedPlateScript != null) { addendum = string.Format("[{0}]: ", currentSummonedPlateScript.fullPlateName); }
        base.ModuleLog(moduleId, addendum + message, args);
    }
    void AllmightySinnohModuleLog(int moduleId, string message, params object[] args)
    {
        base.ModuleLog(moduleId, message, args);
    }



    public override void ModuleLogWarning(int moduleId, string message, params object[] args)
    {
        string addendum = "";
        if (currentSummonedPlateScript != null) { addendum = string.Format("[{0}]: ", currentSummonedPlateScript.fullPlateName); }
        base.ModuleLogWarning(moduleId, addendum + message, args);
    }



    public override void ModuleLogError(int moduleId, string message, params object[] args)
    {
        string addendum = "";
        if (currentSummonedPlateScript != null) { addendum = string.Format("[{0}]: ", currentSummonedPlateScript.fullPlateName); }
        base.ModuleLogError(moduleId, addendum + message, args);
    }
    void AllmightySinnohModuleLogError(int moduleId, string message, params object[] args)
    {
        base.ModuleLogError(moduleId, message, args);
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //   Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Called by Plates when they are summoned to set their custom Twitch Help Message </summary>
    public override void ReceiveTwitchHelpMessage(string message)
    {
        TwitchHelpMessage = message;
    }

    protected override IEnumerator ProcessTwitchCommand(string command)
    {
        throw new System.NotImplementedException();

        // Command for wiggling the bomb?
        // Command for highlighting all 18 plates one after another

        // There can be commands for Allmighty Sinnoh itself
        // Otherwise
        // return currentSummonedPlateScript.ProcessTwitchCommand(command);
    }

    protected override IEnumerator TwitchHandleForcedSolve()
    {
        // Do something so that they appear then solve then appear then solve?
        throw new System.NotImplementedException();
    }
}
