using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;


public class AllmightySinnoh : SummoningModule {

    /// <summary>
    /// Plates are ordered in the same order as the documentation .txt:
    /// BLANK - FIST - SKY - TOXIC - EARTH - STONE - INSECT - SPOOKY - FLAME
    /// SPLASH - IRON - MEADOW - ZAP - MIND - ICICLE - DRACO - DREAD - PIXIE
    /// </summary>
    [SerializeField] GameObject[] allPlatesPrefabs = new GameObject[18];

    /// <summary> Names of each plate, for logging purposes mainly </summary>
    readonly string[] plateNames = new string[18]{
        "Blank", "Fist", "Sky", "Toxic", "Earth", "Stone", "Insect", "Spooky", "Flame",
        "Splash", "Iron", "Meadow", "Zap", "Mind", "Icicle", "Draco", "Dread", "Pixie"
    };
    /// <summary> Misspelled name of each Plate, for use with the Mark of Antimatter </summary>
    readonly string[] misspelledPlateNames = new string[18]{
        "BLAMK", "FISP", "SKIE", "TOXIK", "EARF", "SLONE", "INSEKT", "SPOOK", "FLOOM",
        "SPLISH", "ERON", "MEABOW", "ZAD", "MIMD", "ICYCLE", "DRAGO", "DRED", "RIXIE"
    };

    /// <summary> Number of Plates Solved. Initializes at 0 and gets incremented to 1, 2 then 3. </summary>
    int numberOfPlatesSolved;


    // Visual Plates
    /// <summary> Rotation speed of the Visual Plates. Negative so they rotate counter-clockwise </summary>
    [SerializeField] float visualPlatesRotationSpeed;
    /// <summary> Radius / Spacing of the Visual Plates from the center of their circle </summary>
    [SerializeField] float visualPlatesRotationCircleRadius;
    /// <summary> Scale factor of the Visual Plates, so they appear smaller and can fit in the module. </summary>
    [SerializeField] float visualPlatesScaleFactor;
    /// <summary> How fast the "Flip Wave" moves along the Plates. </summary>
    [SerializeField] float platesWaveSpeed;
    /// <summary> Cumulative time used to compute the location of Visual Plates around the circle </summary>
    float visualPlatesRotationTime;
    /// <summary> Center of Rotation of the Plates </summary>
    [SerializeField] Transform RotationCenter;
    /// <summary> Parent GameObject containing the Visual Plates.
    /// This Parent contains the Box Collider and the KMSelectable, it serves as a Hitbox
    /// while the Plate meshes are the visual that can flip around </summary>
    GameObject[] visualPlatesParents = new GameObject[18];
    /// <summary> Array containing the Meshes of the Visual Plates, that can flip on themselves
    /// without risk of messing up the hit detection of the KMSelectables</summary>
    GameObject[] visualPlates = new GameObject[18];
    /// <summary> Array with all KMSelectables for the Visual Plates </summary>
    KMSelectable[] eighteenVisualPlateSelectables = new KMSelectable[18];

    // Mark of Time lag
    /// <summary> How fast the Plate Marked by Time should follow its "wanted" position </summary>
    [SerializeField][Range(0, 1)] float timeMarkedPlateLagSpeed;
    Vector3 previousFrameTimePlateLocation;

    // Solvable Plate
    /// <summary> Spawn Point and Parent for the Solvable Plates </summary>
    [SerializeField] Transform SolvablePlateSpawnPoint;
    Coroutine SolvablePlateApparitionCoroutine;
    Coroutine SolvablePlateDisparitionCoroutine;

    /// <summary> Reference to the Bomb, to gather information about the Timer, other Modules
    /// or its rotation for Twitch Plays' Wiggle command </summary>
    KMBomb bombReference;


    /// <summary> TextMesh where the Individual Plates will have their names written </summary>
    [SerializeField] TextMesh individualPlateNameText;
    /// <summary> Sounds for when pressing a Plate </summary>
    [SerializeField] AudioClip[] platePressedSounds;

    /// <summary> Sound for when a Solvable Plate is successfully solved </summary>
    [SerializeField] AudioClip SolvablePlateSolvedSound;
    /// <summary> Sound for when the entire Allmighty Sinnoh Module is solved </summary>
    [SerializeField] AudioClip AllmightySinnohSolveSound;


    // Marks
    int initialTimeMark, initialSpaceMark, initialAntimaterMark;
    int finalTimeMark, finalSpaceMark, finalAntimaterMark;
    /// <summary> Index determining which Mark is expected to be pressed by the defuser.
    /// 0 means Mark of Time, 1 is Space and 2 is Antimatter. </summary>
    int markToPress;


    // Universal Logging Data
    static int moduleIdCounter = 1;
    int allmightySinnohModuleId;

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Vanilla Unity Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Buttons gathering and GetComponents
    protected override void Awake()
    {
        base.Awake();

        allmightySinnohModuleId = moduleIdCounter++;

        EmptyVisualPlateName();
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


    // OnModuleActivate is used instead of Start because KMBomb is not available on Start
    // and we need it to initialize the Marks correctly
    protected override void OnModuleActivate()
    {
        base.OnModuleActivate();

        InitializePuzzle();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Interaction
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Called when one of the 18 Visual Plates is pressed </summary>
    void OnVisualPlatePressed(int plateIndex)
    {
        // Feedback
        casingPressableButton.AddInteractionPunch();
        PlaySound(platePressedSounds.PickRandom());

        // No code expected once the Marks have been pressed
        if (markToPress > 2)
        { return; }

        // Determine which Plate was expected to be pressed
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

        // Verify that information
        if (plateIndex == expectedPlate)
            // Correct Visual Plate => Pass, even potentially to the next step!!
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Pressed {0} Plate for Mark of {1} submission. That is Valid",
                GetPlateNameFromIndex(plateIndex), markToPress == 0 ? "Time" : markToPress == 1 ? "Space" : "Antimatter");

            markToPress++;

            // Feedback to make the Plates rotate faster
            StartCoroutine(IncreaseVisualRotationSpeedForFeedback());

            // Verify if we need to start Spawning Solvable Plates
            if (markToPress == 3)
            {
                RemoveVisualPlateSelectables();
                SpawnSolvablePlate(finalTimeMark);
            }
        }
        else
        // Incorrect Visual Plate => Strike!!
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "That is invalid. Expected {0} Plate.", GetPlateNameFromIndex(expectedPlate));
            ReceiveStrike();
        }
    }




    public override void ReceiveSolve()
    {
        // This doesn't outright solve Allmighty Sinnoh since it does need to solve the 3 Solvable Plates in a row.
        numberOfPlatesSolved++;

        // Visual Feedback to make the Plates spin faster
        StartCoroutine(IncreaseVisualRotationSpeedForFeedback());

        // Different text for each "Stage"
        switch (numberOfPlatesSolved)
        {
            case 0:
                AllmightySinnohModuleLogError(allmightySinnohModuleId, "Received Solve but numberOfPlatesSolved is still at 0! Please report this to thunder725");
                break;

            case 1:
                AllmightySinnohModuleLog(allmightySinnohModuleId, "Received solve from the summoned {0}, Marked by Time. Summoning the Space-Marked one next.", currentSummonedPlateScript.fullPlateName);
                SpawnSolvablePlate(finalSpaceMark);

                // Play sound only on NOT solve, because solve has a different sound
                PlaySound(SolvablePlateSolvedSound);
                break;

            case 2:
                AllmightySinnohModuleLog(allmightySinnohModuleId, "Received solve from the summoned {0}, Marked by Space. Summoning the Antimatter-Marked one next.", currentSummonedPlateScript.fullPlateName);
                SpawnSolvablePlate(finalAntimaterMark);

                // Play sound only on NOT solve, because solve has a different sound
                PlaySound(SolvablePlateSolvedSound);
                break;

            case 3:
                // Solve the module
                AllmightySinnohModuleLog(allmightySinnohModuleId, "Received solve from the summoned {0}, Marked by Antimatter. All Plates have been solved. Solving Module.", currentSummonedPlateScript.fullPlateName);

                // Nothing to spawn, just despawn!
                StartCoroutine(SolvablePlateDisparitionAnimation(currentSummonedPlateScript, currentSummonedPlateObject));
                SolveAllmightySinnoh();
                break;
        }
    }

    public override void ReceiveStrike()
    {
        base.ReceiveStrike();

        AllmightySinnohModuleLog(allmightySinnohModuleId, "!! STRIKE !!");
        thisModule.HandleStrike();
    }

    void SolveAllmightySinnoh()
    {
        isModuleSolved = true;

        PlaySound(AllmightySinnohSolveSound);

        thisModule.HandlePass();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Plate Spawning (visual & Solvable)
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Big method handling the spawn of the 18 circling Visual Plates that are used for the Marks </summary>
    void SpawnEighteenVisualPlates()
    {
        // Prepare memory region for future variables
        KMSelectable[] alreadyPresentPlateSelectables;
        BoxCollider alreadyPresentPlateBlockerCollider;
        BoxCollider _addedBoxCollider;
        KMSelectable _addedPlateSelectable;
        KMHighlightable _addedHighlightable;

       

        // Prepare constant data
        Vector3 _boxColliderSize = new Vector3(0.1f, 0.01f, 0.15f);

        // For all 18 plates
        for (int i = 0; i < 18; i++)
        {
            // We cannot just attach a collider, highlightable and selectable to the Plate
            // Because when the plate rotates, its KMSelectable is not selectable from the back!
            // So we need Plates to have a parent that contains the Selectable & Hitbox
            // And this parent has the Plate and Highlightable (that rotate) as a child
            visualPlatesParents[i] = new GameObject();

            // Attach that parent to this module
            visualPlatesParents[i].transform.parent = summonedPlateParentTransform.transform;

            // Spawn the Visual Plate then attach it to its Parent
            visualPlates[i] = Instantiate(allPlatesPrefabs[i], visualPlatesParents[i].transform.position, visualPlatesParents[i].transform.rotation, visualPlatesParents[i].transform);

            // Scale Down post-attachment so that the scale of the Module doesn't matter, it's all relative
            visualPlatesParents[i].transform.localScale = visualPlatesScaleFactor * Vector3.one;


            // Remove all of its selectables' gameobjects,
            // so it becomes just a visual plate without any buttons
            alreadyPresentPlateSelectables = visualPlates[i].GetComponentsInChildren<KMSelectable>();
            foreach (KMSelectable _plateButton in alreadyPresentPlateSelectables)
            {
                Destroy(_plateButton.gameObject);
            }

            // Remove that Plate's already existing collider, if one exists
            // If it exists, that is the "Plate Collision Blocker", an invisible non-Selectable
            // collider here to block the mouse selection of the casing button. Not all Plates have it.
            alreadyPresentPlateBlockerCollider = visualPlates[i].GetComponentInChildren<BoxCollider>();
            if (alreadyPresentPlateBlockerCollider != null)
            {
                Destroy(alreadyPresentPlateBlockerCollider.gameObject);
            }

            // Remove that Plate's script to prevent any and all issues
            Destroy(visualPlates[i].GetComponent<PlateBase>());



            // Attach a box collider with correct sizes TO THE PARENT
            _addedBoxCollider = visualPlatesParents[i].AddComponent<BoxCollider>();
            _addedBoxCollider.size = _boxColliderSize;
            _addedBoxCollider.center = Vector3.zero;

            // Attach a KM Highlightable to the Plate directly, we want it to rotate
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
            _addedPlateSelectable.UpdateChildrenProperly();

            // Record that Selectable in an array
            eighteenVisualPlateSelectables[i] = _addedPlateSelectable;
        }


        // Use that Array of all Plate Selectable and add them as children of the main selectable
        // Do not forget to keep the Casing Pressable Button (SINNOH)
        List<KMSelectable> pressableChildren = new List<KMSelectable>() { casingPressableButton };
        pressableChildren.AddRange(eighteenVisualPlateSelectables);
        moduleSelectable.Children = pressableChildren.ToArray();
        moduleSelectable.UpdateChildrenProperly();


        // Add interactions to the plates (Highlight name show + Mark Press)
        // As always, can't just use a for loop because the delegate would keep the latest value of i
        // So everything would then ask for ShowPlateName(18)
        foreach (KMSelectable _plateSelectable in eighteenVisualPlateSelectables)
        {
            _plateSelectable.OnHighlight += delegate () { ShowVisualPlateName(Array.IndexOf(eighteenVisualPlateSelectables, _plateSelectable)); };
            _plateSelectable.OnHighlightEnded += EmptyVisualPlateName;
            _plateSelectable.OnInteract += delegate () { OnVisualPlatePressed(Array.IndexOf(eighteenVisualPlateSelectables, _plateSelectable)); return false; };
        }

    }



    /// <summary> Remove the buttons from the Visual Plates to prevent any issue (they prevent you from clicking the SINNOH text) </summary>
    void RemoveVisualPlateSelectables()
    {
        // Remove the Highlight and Collider
        // Colliders prevent the press of the SINNOH casing button, which isn't really fun
        for (int i = 0; i < 18; i++)
        {
            // The KMSelectable itself is not deleted, its hitbox and highlights are both just disabled
            // Destroying the three of them causes a hard-lock of the game where you cannot interact nor click with anything
            // So instead we disable them

            // Destroy(visualPlatesParents[i].GetComponent<KMSelectable>());
            visualPlates[i].GetComponent<KMHighlightable>().enabled = false;
            visualPlatesParents[i].GetComponent<BoxCollider>().enabled = false;
        }

        // Update the data for this Selectable
        // It's gonna be updated at soon as a Plate is spawned (so its children is just CasingPressableButton + The Plate's Buttons) but still
        // Better safe than sorrry
        moduleSelectable.Children = new KMSelectable[1] { casingPressableButton };
        moduleSelectable.UpdateChildrenProperly();
    }

    /// <summary> Method to spawn the next Solvable Plate, after the Marks have been pressed </summary>
    void SpawnSolvablePlate(int plateIndex)
    {
        // Potentially Destroy the previous Plate
        // It is autonomous and will get rid of everything itself
        if (currentSummonedPlateObject != null)
        {
            SolvablePlateDisparitionCoroutine = StartCoroutine(SolvablePlateDisparitionAnimation(currentSummonedPlateScript, currentSummonedPlateObject));
        }

        AllmightySinnohModuleLog(allmightySinnohModuleId, "Summoning next Plate: {0} Plate.", GetPlateNameFromIndex(plateIndex));

        // "Instantiate" summons in World Space and THEN attaches, locations are NOT in localspace contrary to what I thought
        currentSummonedPlateObject = Instantiate(allPlatesPrefabs[plateIndex], SolvablePlateSpawnPoint.position, SolvablePlateSpawnPoint.rotation, SolvablePlateSpawnPoint);
        currentSummonedPlateScript = currentSummonedPlateObject.GetComponent<PlateBase>();

        // No need to reset its LocalScale to 1 because the apparition animation already does that!

        // Initialize the Summoned Plate Module
        TransmitInformationToPlate();

        // We receive its buttons in its Awake
        currentSummonedPlateScript.InitializeModuleAwake();

        // It generates its puzzle in Start
        currentSummonedPlateScript.InitializeModuleStart();

        // Now wait for reception of the Solve!


        // Animate apparition
        // We might solve the Plate before it appears, in the case of the TwitchAutosolve for example
        // So just in case
        if (SolvablePlateApparitionCoroutine != null)
        { StopCoroutine(SolvablePlateApparitionCoroutine); }
        SolvablePlateApparitionCoroutine = StartCoroutine(SolvablePlateApparitionAnimation());
    }

    void DestroyPreviousSolvablePlate(PlateBase _plateScript, GameObject _plateObject)
    {
        // Ask the Plate to remove its delegate to the casingButton.OnInteract delegate
        // Otherwise, we get phantom scripts, not attached to anything since their Plate has been Destroyed
        // but they still do code and log elements!!
        _plateScript.RemoveDelegates();

        // Destroy Script
        Destroy(_plateScript);
        Destroy(_plateObject);


        // Clean Selectables ONLY if this was the last plate
        // Otherwise, we might clean the selectables of the current plate!!
        if (numberOfPlatesSolved == 3)
        {
            moduleSelectable.Children = new KMSelectable[1] { casingPressableButton };
            moduleSelectable.UpdateChildrenProperly();
        }
    }

    /// <summary> Coroutine to make the Solvable Plate's apparition smooth and juicy </summary>
    IEnumerator SolvablePlateApparitionAnimation()
    {
        // Bring the plate from 0 to 1 scale
        currentSummonedPlateObject.transform.localScale = Vector3.zero;

        float _progress = 0f;

        // Increase its size using some smooth easing
        while (_progress < 1)
        {
            // Safety check because the Twitch Force Solve can mess with this Coroutine
            if (currentSummonedPlateObject == null)
            { StopCoroutine(SolvablePlateApparitionCoroutine); yield break; }

            // Make the Plate grow
            _progress += Time.deltaTime;
            currentSummonedPlateObject.transform.localScale = Vector3.one * Easing.InOutCubic(_progress, 0, 1, 1);

            yield return new WaitForEndOfFrame();
        }

        // Once it's done, set it to a vanilla 1 scale
        currentSummonedPlateObject.transform.localScale = Vector3.one;
    }


    IEnumerator SolvablePlateDisparitionAnimation(PlateBase _plateScript, GameObject _plateObject)
    {
        AllmightySinnohModuleLog(allmightySinnohModuleId, "Starting Previous Plate Disparition Animation");


        // Avoid the Colliders being scared of negative or close-to-zero sizes
        foreach (Collider _collider in _plateObject.GetComponentsInChildren<Collider>())
        {
            _collider.gameObject.SetActive(false);
        }


        // Bring the plate from current to 0 scale
        float _startingScale = _plateObject.transform.localScale.x;

        float _progress = 0f;

        // Increase its size using some smooth easing
        while (_progress < 1f)
        {
            // Safety check because the Twitch Force Solve can mess with this Coroutine
            if (_plateObject == null)
            { StopCoroutine(SolvablePlateApparitionCoroutine); yield break; }

            // Make the Plate shrink
            _progress += Time.deltaTime;
            _plateObject.transform.localScale = Vector3.one * Easing.InCubic(_progress, _startingScale, 0, 1f);

            yield return new WaitForEndOfFrame();
        }

        // Once it's done, set it to nearly-zero
        _plateObject.transform.localScale = Vector3.zero;

        DestroyPreviousSolvablePlate(_plateScript, _plateObject);
    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Visual Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    /// <summary> Make every Plate rotate and flip for aesthetic and gameplay purposes </summary>
    void RotatePlatesOnUpdate()
    {
        // Use an incremental time instead of computing it directly with Time.time to allow
        // for dynamic speed changes upon feedback
        visualPlatesRotationTime = (visualPlatesRotationTime + (Time.deltaTime * visualPlatesRotationSpeed)) % 1;
        float _localRotationTime;


        // Data used for rotating around the center
        Vector3 _localRotationCenter = RotationCenter.localPosition;
        Vector3 _localPlateParentPosition = Vector3.zero;
        Vector3 _localPlateParentRotation = Vector3.zero;
        Vector3 _localPlateRotation = Vector3.zero;

        // Load constants that are all variations of 1/18
        const float eighteenthRadianRotation = 0.34906585f;
        const float eighteenthDegreesRotation = 20f;
        const float eighteenth = 0.05555555f;


        // Get a pseudo-normalized Time for the "Flip Wave" from 0 to 2, representing it circling around the Plates
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
            _localRotationTime = 6.283185f * visualPlatesRotationTime + i * eighteenthRadianRotation;
            _localPlateParentPosition.x = Mathf.Cos(_localRotationTime) * visualPlatesRotationCircleRadius;
            _localPlateParentPosition.z = Mathf.Sin(_localRotationTime) * visualPlatesRotationCircleRadius;


            // Set the plate's depth wiggle to avoid Z-Fighting
            _localPlateParentPosition.y = Mathf.Sin(i + i + i) * 0.002f;
            
            // Set the plate's yaw so they point inwards and everything is prettier
            _localPlateParentRotation.y = 90 - i * eighteenthDegreesRotation - (visualPlatesRotationTime * 360);



            // Obtain a single 0-to-2 wave moving through the plates
            // The offset using "i * eighteenth" is so that every plate has its own delayed "wave" timer
            // Code fixed thanks to arcorann, thanks to them!!
            _normalizedLocalWaveTime = (_normalizedWaveTime + i * eighteenth) % 2;

            // The Marked by Space Plate does not flip
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

            // Move the PlateParent around the circle
            // Rotate the Plate Parent's Yaw (since that's linked to position)
            visualPlatesParents[i].transform.localPosition = _localRotationCenter + _localPlateParentPosition;
            visualPlatesParents[i].transform.localEulerAngles = _localPlateParentRotation;
            // But individually rotate the Plate for the Wave, so it doesn't affect its hitbox
            visualPlates[i].transform.localEulerAngles = _localPlateRotation;
        }
    }

    /// <summary> Called upon correct Marked Plate presses or Solvable Plate Solve.
    /// Give a burst of speed to the circle to make it more juicy </summary>
    IEnumerator IncreaseVisualRotationSpeedForFeedback()
    {
        float _timer = 0;
        const float _burstDuration = 2;

        while (_timer < _burstDuration)
        {
            _timer += Time.deltaTime;

            // Add a burst of rotation that diminishes with time
            visualPlatesRotationTime += Easing.InQuad(_timer, -0.01f, 0, _burstDuration);

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    /// <summary> Visual Plate Marked by Time lags behind when the Bomb gets moved or rotated </summary>
    void LagTimePlateBehind()
    {
        // Interp towards the goal of the "currentLocation"
        visualPlatesParents[initialTimeMark].transform.position = Vector3.Lerp(previousFrameTimePlateLocation, visualPlatesParents[initialTimeMark].transform.position, timeMarkedPlateLagSpeed);

        // Save to know a "Previous Frame"
        previousFrameTimePlateLocation = visualPlatesParents[initialTimeMark].transform.position;
    }


    void ShowVisualPlateName(int plateIndex)
    {
        individualPlateNameText.text = plateIndex == initialAntimaterMark ? misspelledPlateNames[plateIndex] : GetPlateNameFromIndex(plateIndex).ToUpper();
    }
    void EmptyVisualPlateName()
    {
        individualPlateNameText.text = string.Empty;
    }


    string GetPlateNameFromIndex(int plateIndex)
    {
        return plateNames[plateIndex];
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        DetermineInitialMarks();
        ApplyMarkSteps();
    }

    /// <summary> Randomly select the 3 Plates that will get Marked </summary>
    void DetermineInitialMarks()
    {
        // This allows for three random numbers within [0-17] with no repeats
        int[] _numbers = new int[18]{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }.Shuffle();
        initialTimeMark = _numbers[0];
        initialSpaceMark = _numbers[1];
        initialAntimaterMark = _numbers[2];

        AllmightySinnohModuleLog(allmightySinnohModuleId, "Initial Time Mark is upon {0} Plate, Space Mark is upon {1}, Antimatter Mark is upon {2}",
            GetPlateNameFromIndex(initialTimeMark), GetPlateNameFromIndex(initialSpaceMark), GetPlateNameFromIndex(initialAntimaterMark));
    }

    /// <summary> Move the Marks around the circle of Plates depending on the different Steps </summary>
    void ApplyMarkSteps()
    {
        // Initialize "final" Marks without overriding the initial ones
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


        // All Steps of Allmighty Sinnoh are inspired by Arceus' Learnset in its original Generation

        // Step One
        // Level 10 => Gravity => Prime Numbers
        // The Serial Number contains a Prime and a non-Prime Number
        // Regex patterns should NOT contain any /
        bool _foundPrime = Regex.IsMatch(_bombSerialNumber, "[2357]");
        bool _foundNonPrime = Regex.IsMatch(_bombSerialNumber, "[014689]");

        if (_foundPrime && _foundNonPrime)
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step One should be applied.");
            MoveMarks(3, 12, 5);
        }


        // Step Two
        // Level 20 => Earth Power => Power of the three divinities that created the Earth and the Universe
        // An Initial Mark landed on Iron, Splash or Spooky Plates
        // Iron index is 10
        // Splash index is 9
        // Spooky index is 7
        int[] _ironSplashOrSpookyIndices = new int[3] { 10, 9, 7 };
        int[] _initialMarks = new int[3] { initialTimeMark, initialSpaceMark, initialSpaceMark };
        if (_ironSplashOrSpookyIndices.Intersect(_initialMarks).Count() > 0)
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Two should be applied.");
            MoveMarks(8, 8, 7);
        }


        // Step Three
        // Level 30 => Hyper Voice => The Port that is linked to Sound
        // A Stereo RCA port is present
        if (_bombPorts.Contains("StereoRCA"))
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Three should be applied.");
            MoveMarks(3, 10, 2);
        }


        // Step Four
        // Level 40 => Extreme Speed => The Port that transports internet through fiber wire, using Light
        // An RJ-45 port is present
        if (_bombPorts.Contains("RJ45"))
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Four should be applied.");
            MoveMarks(3, 1, 6);
        }


        // Step Five
        // Level 50 => Refresh => CLEAR indicator since you CLEAR statuses
        // A CLR indicator is present
        if (_bombIndicators.Contains("CLR"))
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Five should be applied.");
            MoveMarks(5, 5, 5);
        }


        // Step Six
        // Level 60 => Future Sight => Being aligned with the Timer to See the Future
        // This module is vertically or horizontally aligned to the Timer
        // It is forced to be on the same side as it, because it can spawn Timer-dependant Plates such as Zap or Iron

        // Get the Bomb (KMBomb is valid only if called within OnActivate, not within Start()!!)
        bombReference = GetComponentInParent<KMBomb>();
        if (bombReference != null)
        {
            // From that, get the Timer's position
            Vector3 timerPos = Vector3.zero;



            // The Timer is of type "TimerModule" in the Test Harness, so we use that for checking the Timer
            // However, it is of type "TimerComponent" in the real game, so we use that instead when built
            // Thanks to Qkrisi for that syntax!
            timerPos = bombReference.GetComponentInChildren<
#if UNITY_EDITOR
                TimerModule
#else
                TimerComponent
#endif
                >().transform.position;

            // Gather world locations of timer & this module
            Vector3 modulePos = transform.position;

            // Dot product between this module's Forward (blue) and the vector to the Timer
            Vector3 toTimer = (timerPos - modulePos).normalized;
            float dot = Vector3.Dot(transform.forward, toTimer);
            AllmightySinnohModuleLog(allmightySinnohModuleId, "timerPos = {0}; modulePos = {1}; vectorToTimer = {2}; transform.up = {3}; dot = {4}",
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
                AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Six should be applied. This module is horizontally aligned with the timer.");
                MoveMarks(15, 2, 5);
            }
            else if (dot == 1)
            {
                AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Six should be applied. This module is vertically aligned with the timer.");
                MoveMarks(15, 2, 5);
            }
        }
        else
        {
            AllmightySinnohModuleLogError(allmightySinnohModuleId, "BOMB IS INVALID!! Please report this to thunder725");
        }


        // Step Seven
        // Level 70 => Recover => Healing symbolized by more Lits than Unlits
        // There are more Lit indicators than Unlits
        if (_bombNumberOfLits > _bombNumberOfUnlits)
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Seven should be applied.");
            MoveMarks(5, 4, 3);
        }


        // Step Eight
        // Level 80 => Hyper Beam => Powerful attack, so check for the Bomb's Power
        // There is at least 1 AA battery and 1 D battery
        if (_bombHasAaBatteries && _bombHasDBatteries)
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Eight should be applied.");
            MoveMarks(2, 6, 0);
        }


        // Step Nine
        // Level 90 => Perish Song => Dying symbolized by more Unlits than Lits
        // There are more Unlits than Lits
        if (_bombNumberOfLits < _bombNumberOfUnlits)
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Nine should be applied.");
            MoveMarks(3, 4, 5);
        }


        // Step Ten
        // Level 100 => Judgement => Arceus' Signature move, rendered stronger by the presence of its Plates
        // There are Mythical Plate modules
        if (bombReference != null)
        {
            if (bombReference.GetComponentInChildren<IndividualPlateModule>() != null)
            {
                AllmightySinnohModuleLog(allmightySinnohModuleId, "Step Ten should be applied.");
                MoveMarks(4, 9, 3);
            }
        }
        else
        {
            AllmightySinnohModuleLogError(allmightySinnohModuleId, "BOMB IS INVALID!! Please report this to thunder725");
        }
    }


    /// <summary> Method for moving the Marks around a specific amount </summary>
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

        // Different log if an Offset-By-One has been needed, for clarity's sake
        AllmightySinnohModuleLog(allmightySinnohModuleId, "Time Mark ended on {0} Plate. Space Mark ended on {1} Plate. Antimatter Mark ended on {2} Plate.{3}",
            GetPlateNameFromIndex(finalTimeMark), GetPlateNameFromIndex(finalSpaceMark), GetPlateNameFromIndex(finalAntimaterMark),
            _uniqueOffsetNeeded ? " At least one Mark had to be offset once to avoid collisions." : "");
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //   Logging
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Reroute the module logs
    // This is for example to keep [Allmighty Sinnoh #2] instead of the Plate's name and moduleID
    // Also add something to know if a Plate is the one sending a message or not!
    public override void ModuleLog(int passedModuleId, string message, params object[] args)
    {
        string addendum = "";
        if (currentSummonedPlateScript != null) { addendum = string.Format("[{0}]: ", currentSummonedPlateScript.fullPlateName); }
        base.ModuleLog(allmightySinnohModuleId, addendum + message, args);
    }
    void AllmightySinnohModuleLog(int passedModuleId, string message, params object[] args)
    {
        base.ModuleLog(allmightySinnohModuleId, message, args);
    }


    public override void ModuleLogWarning(int passedModuleId, string message, params object[] args)
    {
        string addendum = "";
        if (currentSummonedPlateScript != null) { addendum = string.Format("[{0}]: ", currentSummonedPlateScript.fullPlateName); }
        base.ModuleLogWarning(allmightySinnohModuleId, addendum + message, args);
    }


    public override void ModuleLogError(int passedModuleId, string message, params object[] args)
    {
        string addendum = "";
        if (currentSummonedPlateScript != null) { addendum = string.Format("[{0}]: ", currentSummonedPlateScript.fullPlateName); }
        base.ModuleLogError(allmightySinnohModuleId, addendum + message, args);
    }
    void AllmightySinnohModuleLogError(int passedModuleId, string message, params object[] args)
    {
        base.ModuleLogError(allmightySinnohModuleId, message, args);
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //   Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    /// <summary> Solvable Plates will initialize their own Twitch Help Message on spawn;
    /// but Allmighty Sinnoh has its own beforehand! </summary>
    void InitializeAllmightySinnohTwitchHelpMessage()
    {
        ReceiveTwitchHelpMessage("Press the three Marked Plates using “!{0} Submit Meadow Iron Pixie”. Show the 18 names for 1 second each using “!{0} shownames”. Wiggle the bomb to check for Marked by Time using “!{0} wiggle”. Press the SINNOH casing button using “!{0} sinnoh”.");
    }

    /// <summary> Called by Plates when they are summoned to set their custom Twitch Help Message </summary>
    public override void ReceiveTwitchHelpMessage(string message)
    {
        TwitchHelpMessage = message;
    }




    protected override IEnumerator ProcessTwitchCommand(string command)
    {
        if (isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length == 0)
        {
            yield return "sendtochaterror {0} Received empty command!";
            yield break;
        }

        // Pressing the SINNOH text button
        if (commandParts.Length == 1 && commandParts[0] == "sinnoh")
        {
            casingPressableButton.OnInteract();
            yield break;
        }

        // Showing all 18 names in order
        if (commandParts.Length == 1 && commandParts[0] == "shownames")
        {
            for (int i = 0; i < 18; i ++)
            {

                ShowVisualPlateName(i);

                yield return new WaitForSeconds(1f);
            }

            EmptyVisualPlateName();
            yield break;
        }

        // Wiggling the bomb to check which Plate is Marked by Time
        if (commandParts.Length == 1 && commandParts[0] == "wiggle")
        {
            // Wait 1s to make sure the bomb is settled on a fixed rotation
            yield return new WaitForSeconds(1);

            // Save the current BombRotation
            Quaternion _startingBombRotation = bombReference.transform.rotation;

            // Prepare variables
            float timer = 0;

            float rotationX, rotationZ;
            const float rotationFrequency = 25f;
            const float rotationAmplitude = 5f;


            // Rotate for 3 seconds
            while (timer < 3)
            {
                // Increase timer
                timer += Time.deltaTime;

                // Compute cos & sin
                // Move the bomb side to side while bringing it up a bit
                rotationX = Mathf.Sin(timer) * rotationAmplitude;
                rotationZ = Mathf.Cos(timer * rotationFrequency) * rotationAmplitude;

                // Return that to rotate the bomb
                yield return _startingBombRotation * Quaternion.Euler(rotationX, 0, rotationZ);

                yield return new WaitForEndOfFrame();
            }

            // Reset the bomb rotation though
            yield return _startingBombRotation;

            yield break;
        }



        // Then apart from those send the command to the plate
        if (markToPress > 2)
        {
            // Incredibly obscure behaviour that River and Emik were able to inform me about (huge thanks to them!)
            // Link to the reference: https://github.com/Emik03/wawa/blob/main/wawa.TwitchPlays/Source/Twitch%7BTMod%7D.cs#L216-L221
            // You cannot just yield return the IEnumerator itself, you have to Move through it
            // because the IEnumerator is a type that iterates through an IEnumerable.

            // In this case, the ProcessTwitchCommand() method returns an IEnumerator.
            // Twitch Plays receives that IEnumerator and executes its code
            // But in our case we are a method that already is an IEnumerator...
            // just "yield return"ing the Plate's IEnumerator doesn't do anything, no code gets executed!
            // So we need to get that IEnumerator and execute it ourselves, just like what Twitch Plays does

            // The code reference above uses lots of other stuff, like OnYield or ?.Value and stuff like that
            // This is part of the greater "wawa" piece of work, written in C# 9 (I'm in C# 6)
            // For my purpose just returning the Current while MovingNext is enough!

            IEnumerator processedCommand = currentSummonedPlateScript.ProcessTwitchCommand(command);
            // IEnumerator starts initialized with an "index" of -1
            // So we first must MoveNext to get to the first yield return
            // MoveNext() returns true while there's valid code to be executed left
            while (processedCommand.MoveNext())
            {
                // Then yield return what the TwitchCommand wants to do
                yield return processedCommand.Current;
            }

            yield break;
        }

        // Otherwise that's meant to press the Marked Plates
        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Please use keyword Press or just p to submit an answer";
            yield break;
        }


        if (commandParts.Length != 4)
        {
            yield return "sendtochaterror {0} Please submit 3 plate names";
            yield break;
        }



        // transform the Plate Names so that they have a capitalized first letter
        for (int i = 1; i < 4; i ++)
        {
            // Credit https://stackoverflow.com/questions/4135317/make-first-letter-of-a-string-upper-case-with-maximum-performance
            commandParts[i] = commandParts[i].First().ToString().ToUpper() + commandParts[i].Substring(1);
        }


        // Verify existence of the 3 submitted plates
        if (plateNames.Contains(commandParts[1]) == false)
        {
            yield return "sendtochaterror {0} Plate name " + commandParts[1] + " is not recognized!";
            yield break;
        }
        if (plateNames.Contains(commandParts[2]) == false)
        {
            yield return "sendtochaterror {0} Plate name " + commandParts[2] + " is not recognized!";
            yield break;
        }
        if (plateNames.Contains(commandParts[3]) == false)
        {
            yield return "sendtochaterror {0} Plate name " + commandParts[3] + " is not recognized!";
            yield break;
        }

        // Press the Visual Plates
        for (int i = 1; i < 4; i++)
        {
            eighteenVisualPlateSelectables[Array.IndexOf(plateNames, commandParts[i])].OnInteract();
            yield return new WaitForSeconds(0.3f);
        }
    }



    protected override IEnumerator TwitchHandleForcedSolve()
    {
        if (isModuleSolved) { yield break; }

        ModuleLog(allmightySinnohModuleId, "Received instructions to Twitch Force Solve!");

        // Since we do not know which state we are in currently,
        // Check all possible situations and do everything accordingly


        // Step one: Press the Visuals
        // Press Mark of Time
        if (markToPress == 0)
        {
            eighteenVisualPlateSelectables[finalTimeMark].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }

        // Press Mark of Space
        if (markToPress == 1)
        {
            eighteenVisualPlateSelectables[finalSpaceMark].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }

        // Press Mark of Antimatter
        if (markToPress == 2)
        {
            eighteenVisualPlateSelectables[finalAntimaterMark].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }


        // Step 2, autosolve each plate
        // Cannot just do for loop with 3 because we do not know how many are left to do
        // (we could determine that, but a while is simpler)
        while (isModuleSolved == false)
        {
            AllmightySinnohModuleLog(allmightySinnohModuleId, "Auto-Solving next Plate");

            // Allow the plate to still visually start appearing
            yield return new WaitForSeconds(0.8f);

            // Ask the Plate to forcesolve itself using the same concept as 
            // the ReceiveTwitchCommand above.

            IEnumerator processedCommand = currentSummonedPlateScript.TwitchHandleForcedSolve();
            while (processedCommand.MoveNext())
            {
                yield return processedCommand.Current;
            }
        }


        // Now should be done!
        yield break;
    }
}
