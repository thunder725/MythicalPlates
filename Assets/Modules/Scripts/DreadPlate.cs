using KModkit;
using System;
using System.Collections;
using UnityEngine;

public class DreadPlate : PlateBase {

    string optimisticNihilismText = "HUMAN EXISTENCE IS SCARY AND CONFUSING A FEW HUNDRED THOUSAND YEARS AGO WE BECAME CONSCIOUS AND FOUND OURSELVES IN A STRANGE PLACE IT WAS FILLED WITH OTHER BEINGS WE COULD EAT SOME SOME COULD EAT US THERE WAS LIQUID STUFF WE COULD DRINK THINGS WE COULD USE TO MAKE MORE THINGS THE DAYTIME SKY HAD A TINY YELLOW BALL THAT WARMED OUR SKIN THE NIGHT SKY WAS FILLED WITH BEAUTIFUL LIGHTS THIS PLACE WAS OBVIOUSLY MADE FOR US SOMETHING WAS WATCHING OVER US WE WERE HOME THIS MADE EVERYTHING MUCH LESS SCARY AND CONFUSING BUT THE OLDER WE GOT THE MORE WE LEARNED ABOUT THE WORLD AND OURSELVES WE LEARNED THAT THE TWINKLING LIGHTS ARE NOT SHINING BEAUTIFULLY FOR US THEY JUST ARE WE LEARNED THAT WE'RE NOT AT THE CENTER OF WHAT WE NOW CALL THE UNIVERSE AND THAT IT IS MUCH MUCH OLDER THAN WE THOUGHT WE LEARNED THAT WE'RE MADE OF MANY LITTLE DEAD THINGS WHICH MAKE UP BIGGER THINGS THAT ARE NOT DEAD FOR SOME REASON AND THAT WE'RE JUST ANOTHER TEMPORARY STAGE IN A HISTORY GOING BACK OVER A BILLION YEARS WE LEARNED IN AWE THAT WE LIVE ON A MOIST SPECK OF DUST MOVING AROUND A MEDIUM SIZED STAR IN A QUIET REGION OF ONE ARM OF AN AVERAGE GALAXY WHICH IS PART OF A GALAXY GROUP THAT WE WILL NEVER LEAVE AND THIS GROUP IS ONLY ONE OF A THOUSANDS THAT TOGETHER MAKE UP A GALAXY SUPERCLUSTER BUT EVEN OUT SUPERCLUSTER IS ONLY ONE IN THOUSANDS THAT MAKE UP WHAT WE CALL THE OBSERVABLE UNIVERSE THE UNIVERSE MIGHT BE A MILLION TIMES BIGGER BUT WE WILL NEVER KNOW WE COULD THROW WORDS AROUND LIKE TWO HUNDRED MILLION GALAXIES OR TRILLIONS OF STARS OR BAZILLIONS OF PLANETS BUT ALL OF THESE NUMBERS MEAN NOTHING OUR BRAINS CAN'T COMPREHEND THESE CONCEPTS THE UNIVERSE IS TOO BIG THERE IS TOO MUCH OF IT BUT SIZE IS NOT THE MOST TROUBLING CONCEPT WE HAVE TO DEAL WITH IT'S TIME OR MORE PRECISELY THE TIME WE HAVE IF YOU'RE LUCKY ENOUGH TO LIVE TO ONE HUNDRED YOU HAVE FIVE THOUSAND TWO HUNDRED WEEKS AT YOUR DISPOSAL IF YOU'RE TWENTY FIVE NOW THEN YOU HAVE THREE THOUSAND NINE HUNDRED WEEKS LEFT IF YOU'RE GOING TO DIE AT SEVENTY THEN THERE ARE TWO THOUSAND THREE HUNDRED AND FORTY WEEKS LEFT A LOT OF TIME BUT ALSO NOT REALLY AND THEN WHAT? YOUR BIOLOGICAL PROCESSES WILL BREAK DOWN AND THE DYNAMIC PATTERN THAT IS YOU WILL STOP BEING DYNAMIC IT WILL DISSOLVE UNTIL THERE IS NO YOU LEFT SOME BELIEVE THAT THERE IS A PART OF US WE CAN'T SEE OR MEASURE BUT WE HAVE NO WAY TO FIND OUT SO THIS LIFE MIGHT BE IT AND WE MIGHT END UP DEAD FOREVER THIS IS LESS SCARY THAN IT SOUNDS THOUGH IF YOU DON'T REMEMBER THE THIRTEEN POINT SEVEN FIVE BILLION YEARS THAT WENT BY BEFORE YOU EXISTED THEN THE TRILLIONS AND TRILLIONS AND TRILLIONS OF YEARS THAT COME AFTER WILL PASS IN NO TIME ONCE YOU'RE GONE CLOSE YOUR EYES COUNT TO ONE THAT'S HOW LONG FOREVER FEELS";

    [SerializeField] TextMesh[] plateWordTexts;

    // Universal Logging Data
    static int moduleIdCounter = 1;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PressingPlateButton("#"); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressingPlateButton("&"); return false; };
        platePressableButtons[2].OnInteract += delegate () { PressingPlateButton("%"); return false; };
        platePressableButtons[3].OnInteract += delegate () { PressingPlateButton("@"); return false; };
        platePressableButtons[4].OnInteract += delegate () { PressingPlateButton("!"); return false; };

    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();



    }

    // public override void UpdateModule() { base.UpdateModule(); }

    void PressingPlateButton(string buttonPressed)
    {
        if (summoningModule.isModuleSolved)
        { return; }

        platePressableButtons[0].AddInteractionPunch();
        summoningModule.PlaySound(platePressedSound);

        


    }

    protected override void CasingTextButtonGetsPressed() { }

    void ModuleShouldStrike()
    {
        summoningModule.ReceiveStrike();
    }

    void ModuleShouldSolve()
    {
        summoningModule.ReceiveSolve();
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
            yield return "sendtochat {0} you must format the submission with “!{1} Submit #”";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s")
        {
            yield return "sendtochat {0} please make sure you Submit with either “Submit” or “s”.";
            yield break;
        }

        if (commandParts[1].Length != 1)
        {
            yield return "sendtochat {0} please sumbit only one of the 5 allowed characters: # & % @ !";
        }


        switch (commandParts[1])
        {
            case "#":
                platePressableButtons[0].OnInteract();
                break;

            case "&":
                platePressableButtons[1].OnInteract();
                break;

            case "%":
                platePressableButtons[2].OnInteract();
                break;

            case "@":
                platePressableButtons[3].OnInteract();
                break;

            case "!":
                platePressableButtons[4].OnInteract();
                break;
        }

    }


    public override IEnumerator TwitchHandleForcedSolve()
    {

        yield break;
    }

}
