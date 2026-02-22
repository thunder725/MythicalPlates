using System.Collections;
using UnityEngine;


/// <summary>
/// Base class for Bomb Modules such as the Plates or Allmighty Sinnoh
/// </summary>
public abstract class SummoningModule : MonoBehaviour {

    // PlateBase-related Data
    protected GameObject currentSummonedPlateObject;
    protected PlateBase currentSummonedPlateScript;
    /// <summary> Transform anchor point, where the Plate should spawn </summary>
    [SerializeField] protected Transform summonedPlateParentTransform;

    // Information that gets transmitted to the PlateBase
    protected KMBombModule thisModule;
    protected KMBombInfo bombInfo;
    protected KMAudio audioSubsystem;
    protected KMSelectable moduleSelectable;
    [SerializeField] protected KMSelectable casingPressableButton;
    


    // Module Data
    public string displayModuleName;


    [HideInInspector] public bool isModuleSolved;


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Vanilla Functions
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Buttons gathering and GetComponents
    protected virtual void Awake()
    {
        GatherBombComponents();
    }

    // Puzzle Initialization
    protected virtual void Start()
    { }

    protected virtual void Update()
    {

    }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Setup
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Gather Components dynamically </summary>
    void GatherBombComponents()
    {
        bombInfo = GetComponent<KMBombInfo>();
        thisModule = GetComponent<KMBombModule>();
        audioSubsystem = GetComponent<KMAudio>();
        moduleSelectable = GetComponent<KMSelectable>();
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Plate Communication 
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    /// <summary> Called by the PlateBase when a Strike should be issued </summary>
    public abstract void ReceiveStrike();
    /// <summary> Called by the PlateBase when the module should get solved. </summary>
    public abstract void ReceiveSolve();


    /// <summary> Transmit bomb-related Information to the PlateBase script </summary>
    protected void TransmitInformationToPlate()
    {
        if (currentSummonedPlateScript == null) { return; }

        currentSummonedPlateScript.bombInfo = bombInfo;
        currentSummonedPlateScript.thisModule = thisModule;
        currentSummonedPlateScript.summoningModule = this;

        if (casingPressableButton != null)
        { currentSummonedPlateScript.casingPressableButton = casingPressableButton; }
    }


    /// <summary> Receive an ordered array of all Buttons on the Plate.
    /// Each Plate is responsible for its sorting. They just get appended after the casing Button if it exists. </summary>
    public abstract void ReceivePlateButtons(KMSelectable[] plateButtons);


    /// <summary> Simplified method to just play a sound from an AudioClip </summary>
    public void PlaySound(AudioClip soundToPlay)
    {
        audioSubsystem.PlaySoundAtTransform(soundToPlay.name, transform);
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Logging
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    /// <summary> Called by the PlateBase when a message should get Logged </summary>
    // Credit for this formatting to UltimateCipher's github
    public void ModuleLog(int moduleId, string message, params object[] args)
    {
        Debug.LogFormat("[{0} #{1}] " + (string.Format(message, args)), displayModuleName, moduleId);
    }

    public void ModuleLogWarning(int moduleId, string message, params object[] args)
    {
        Debug.LogWarningFormat("[{0} #{1}] " + (string.Format(message, args)), displayModuleName, moduleId);
    }

    public void ModuleLogError(int moduleId, string message, params object[] args)
    {
        Debug.LogErrorFormat("[{0} #{1}] " + (string.Format(message, args)), displayModuleName, moduleId);
    }




    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> Receive the TwitchHelpMessage from the plate, either to use as your own or to add it if Sinnoh.
    /// Called by the PlateBase automatically on Awake. </summary>
    public abstract void ReceiveTwitchHelpMessage(string message);
    protected string TwitchHelpMessage;
    /// <summary> Transmitted to the Plate so it integrates its own logic. </summary>
    protected abstract IEnumerator ProcessTwitchCommand(string command);
    /// <summary> Transmitted to the Plate so it integrates its own logic. </summary>
    protected abstract IEnumerator TwitchHandleForcedSolve();

}
