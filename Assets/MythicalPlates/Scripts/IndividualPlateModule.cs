using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class IndividualPlateModule : SummoningModule {

    [SerializeField] GameObject PlateToSummon;
    [SerializeField] AudioClip SolveClip;

    readonly Vector3 plateSpawnLocalPosition = new Vector3 (0, 0.04f, 0.008f);






    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Vanilla Unity Methods
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    // Buttons gathering and GetComponents
    protected override void Awake()
    {
        base.Awake();

        SummonNewPlate();
        TransmitInformationToPlate();

        if (currentSummonedPlateScript != null)
        {
            currentSummonedPlateScript.InitializeModuleAwake();
        }

    }

    // Puzzle Initialization
    protected override void Start()
    {
        base.Start();

        if (currentSummonedPlateScript != null)
        {
            currentSummonedPlateScript.InitializeModuleStart();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (currentSummonedPlateScript != null)
        {
            currentSummonedPlateScript.UpdateModule();
        }
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Plate Communication 
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void SummonNewPlate()
    {
        if (PlateToSummon == null)
        {
            ModuleLog(-1, "PlateToSummon is not set!!! Please contact thunder725 on Discord!! This is very wrong!!!!!");
            return;
        }

        if (summonedPlateParentTransform == null)
        {
            ModuleLog(-1, "summonedPlateParentTransform is not set!!! Please contact thunder725 on Discord!! This is very wrong!!!!!");
            return;
        }
        
        // "Instantiate" summons in World Space and THEN attaches, locations are NOT in localspace contrary to what I thought
        currentSummonedPlateObject = Instantiate(PlateToSummon, summonedPlateParentTransform.position + plateSpawnLocalPosition, summonedPlateParentTransform.rotation, summonedPlateParentTransform);
        currentSummonedPlateScript = currentSummonedPlateObject.GetComponent<PlateBase>();
    }


    public override void ReceiveSolve()
    {
        ModuleLog(currentSummonedPlateScript.moduleId, "Module Solved");
        PlaySound(SolveClip);
        isModuleSolved = true;
        thisModule.HandlePass();
    }

    public override void ReceiveStrike()
    {
        ModuleLog(currentSummonedPlateScript.moduleId, "!! STRIKE !!");
        thisModule.HandleStrike();
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override void ReceiveTwitchHelpMessage(string message)
    {
        TwitchHelpMessage = message;
    }

    protected override IEnumerator ProcessTwitchCommand(string command)
    {
        if (currentSummonedPlateScript == null) 
        {
            ModuleLog(-1, "currentSummonedPlateScript in ProcessTwitchCommand() is not defined!!! Please contact thunder725 on Discord!! This is very wrong!!!!!");
            return null;
        }

        return currentSummonedPlateScript.ProcessTwitchCommand(command);
    }

    protected override IEnumerator TwitchHandleForcedSolve()
    {
        if (currentSummonedPlateScript == null)
        {
            ModuleLog(-1, "currentSummonedPlateScript in TwitchHandleForcedSolve() is not defined!!! Please contact thunder725 on Discord!! This is very wrong!!!!!");
            return null;
        }

        ModuleLog(currentSummonedPlateScript.moduleId, "Received instructions to Twitch Force Solve!");

        return currentSummonedPlateScript.TwitchHandleForcedSolve();
    }
}
