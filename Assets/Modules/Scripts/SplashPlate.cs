using System;
using System.Collections;
using UnityEditorInternal;

public class SplashPlate : PlateBase {

    string[] baseSequences = new string[3];
    string[] finalTimelines = new string[3];

    // Universal Logging Data
    static int moduleIdCounter = 1;

    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        // platePressableButtons[0].OnInteract += delegate () { PressingPlateButton("#"); return false; };
        // platePressableButtons[1].OnInteract += delegate () { PressingPlateButton("&"); return false; };
        // platePressableButtons[2].OnInteract += delegate () { PressingPlateButton("%"); return false; };
        // platePressableButtons[3].OnInteract += delegate () { PressingPlateButton("@"); return false; };
        // platePressableButtons[4].OnInteract += delegate () { PressingPlateButton("!"); return false; };

    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        InitializePuzzle();

    }


    protected override void CasingTextButtonGetsPressed() { }


    void InitializePuzzle()
    {
        // Generate base movement Sequence
        for (int i = 0; i < 10; i ++)
        {
            for (int j = 0; j < 3; j ++)
            {
                baseSequences[j] += GetXyFromNumber(UnityEngine.Random.Range(0, 4));
            }
        }

        int[] sequenceInternalIndex = new int[3];
        byte firstResetIndex = 0;
        byte firstResetLength = 0;
        byte secondResetIndex = 0;
        byte secondResetLength = 0;

        byte scratchByte = 0;

        // Build Sequence skeleton
        // Have one persistent reset going on from character 3 onwards (1 and 2 are free)
        // Have a second reset intermittently appear from 10 onwards
        for (int i = 0; i < 30; i ++)
        {
            if (i < 2)
            {
                firstResetIndex = 255;
            }

            // If reset is 5 long, reset it and switch side
            if (firstResetLength == 5)
            {
                firstResetLength = 1;

                // Second Reset not present? We can move wherever!
                if (secondResetLength == 0)
                {
                    scratchByte = (byte)UnityEngine.Random.Range(0, 3);

                    int iterationCount = 0;

                    // Avoid Repeat
                    while (scratchByte == firstResetIndex)
                    {
                        iterationCount++;
                        if (iterationCount > 25)
                        {
                            summoningModule.ModuleLogError(moduleId, "wtf???");
                            break;
                        }

                        scratchByte = (byte)UnityEngine.Random.Range(0, 3);
                    }
                    firstResetIndex = scratchByte;
                    
                }
                else // Second Reset present? Well select where it currently isn't
                // this works because ((0+1)*2)%3 => 2, ((0+2)*2)%3 => 1, ((2+1)*2)%3 => 0
                {
                    firstResetIndex = (byte)(((firstResetIndex + secondResetIndex) * 2) % 3);
                }
            }
            else
            {
                // Increase Reset Length
                firstResetLength++;

                // If we're above 2 length we can switch
                if (firstResetLength > 2)
                {
                    if (UnityEngine.Random.value < 0.25f)
                    {
                        // Second Reset not present? We can move wherever!
                        if (secondResetLength == 0)
                        {
                            scratchByte = firstResetIndex;

                            // Avoid Repeat
                            while (scratchByte == firstResetIndex)
                            {
                                scratchByte = (byte)UnityEngine.Random.Range(0, 3);
                            }
                            firstResetIndex = scratchByte;

                        }
                        else // Second Reset present? Well select where it currently isn't
                             // this works because ((0+1)*2)%3 => 2, ((0+2)*2)%3 => 1, ((2+1)*2)%3 => 0
                        {
                            firstResetIndex = (byte)(((firstResetIndex + secondResetIndex) * 2) % 3);
                        }
                    }
                }
            }




            // Only do second one
            if (i >= 10)
            {
                // If reset is 5 long, reset it and switch side
                if (secondResetLength == 5)
                {
                    secondResetLength = 1;

                    // Second Reset not present? We can move wherever!
                    if (firstResetLength == 0)
                    {
                        scratchByte = (byte)UnityEngine.Random.Range(0, 3);

                        int iterationCount = 0;

                        // Avoid Repeat
                        while (scratchByte == secondResetIndex)
                        {
                            iterationCount++;
                            if (iterationCount > 25)
                            {
                                summoningModule.ModuleLogError(moduleId, "wtf???");
                                break;
                            }

                            scratchByte = (byte)UnityEngine.Random.Range(0, 3);
                        }
                        secondResetIndex = scratchByte;

                    }
                    else // Second Reset present? Well select where it currently isn't
                         // this works because ((0+1)*2)%3 => 2, ((0+2)*2)%3 => 1, ((2+1)*2)%3 => 0
                    {
                        secondResetIndex = (byte)(((firstResetIndex + secondResetIndex) * 2) % 3);
                    }
                }
                else
                {
                    // Increase Reset Length
                    secondResetLength++;

                    // If we're above 2 length we can switch
                    if (secondResetLength > 2)
                    {
                        if (UnityEngine.Random.value < 0.25f)
                        {
                            // Second Reset not present? We can move wherever!
                            if (firstResetLength == 0)
                            {
                                scratchByte = secondResetIndex;

                                // Avoid Repeat
                                while (scratchByte == secondResetIndex)
                                {
                                    scratchByte = (byte)UnityEngine.Random.Range(0, 3);
                                }
                                secondResetIndex = scratchByte;

                            }
                            else // Second Reset present? Well select where it currently isn't
                                 // this works because ((0+1)*2)%3 => 2, ((0+2)*2)%3 => 1, ((2+1)*2)%3 => 0
                            {
                                secondResetIndex = (byte)(((firstResetIndex + secondResetIndex) * 2) % 3);
                            }
                        }
                    }
                }
            }
            




            for (int j = 0; j < 3; j++)
            {

                if (firstResetIndex == j || (secondResetIndex == j && secondResetLength != 0))
                {
                    sequenceInternalIndex[j] = 0;
                    finalTimelines[j] += '_';
                }
                else
                {
                    finalTimelines[j] += baseSequences[j][sequenceInternalIndex[j] % 10];
                    sequenceInternalIndex[j]++;
                }
            }  
        }

        summoningModule.ModuleLog(moduleId, "Base Sequences are:\n{0}\n{1}\n{2}\nFinal Timelines are:\n{3}\n{4}\n{5}",
            baseSequences[0], baseSequences[1], baseSequences[2], finalTimelines[0], finalTimelines[1], finalTimelines[2]);
    }


    char GetXyFromNumber(int number)
    {
        switch(number)
        {
            case 0: return 'X';
            case 1: return 'Y';
            case 2: return 'x';
            case 3: return 'y';
        }

        return '+';
    }




    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

    }


    public override IEnumerator TwitchHandleForcedSolve()
    {


        yield break;
    }
}
