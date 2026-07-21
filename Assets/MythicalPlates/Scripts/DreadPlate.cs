using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DreadPlate : PlateBase {

    /// <summary> The entire text, in uppercase, with dashes considered as spaces and apostrophes removed (you're => youre) </summary>
    readonly string optimisticNihilismText = "HUMAN EXISTENCE IS SCARY AND CONFUSING A FEW HUNDRED THOUSAND YEARS AGO WE BECAME CONSCIOUS AND FOUND OURSELVES IN A STRANGE PLACE IT WAS FILLED WITH OTHER BEINGS WE COULD EAT SOME SOME COULD EAT US THERE WAS LIQUID STUFF WE COULD DRINK THINGS WE COULD USE TO MAKE MORE THINGS THE DAYTIME SKY HAD A TINY YELLOW BALL THAT WARMED OUR SKIN THE NIGHT SKY WAS FILLED WITH BEAUTIFUL LIGHTS THIS PLACE WAS OBVIOUSLY MADE FOR US SOMETHING WAS WATCHING OVER US WE WERE HOME THIS MADE EVERYTHING MUCH LESS SCARY AND CONFUSING BUT THE OLDER WE GOT THE MORE WE LEARNED ABOUT THE WORLD AND OURSELVES WE LEARNED THAT THE TWINKLING LIGHTS ARE NOT SHINING BEAUTIFULLY FOR US THEY JUST ARE WE LEARNED THAT WERE NOT AT THE CENTER OF WHAT WE NOW CALL THE UNIVERSE AND THAT IT IS MUCH MUCH OLDER THAN WE THOUGHT WE LEARNED THAT WERE MADE OF MANY LITTLE DEAD THINGS WHICH MAKE UP BIGGER THINGS THAT ARE NOT DEAD FOR SOME REASON AND THAT WERE JUST ANOTHER TEMPORARY STAGE IN A HISTORY GOING BACK OVER A BILLION YEARS WE LEARNED IN AWE THAT WE LIVE ON A MOIST SPECK OF DUST MOVING AROUND A MEDIUM SIZED STAR IN A QUIET REGION OF ONE ARM OF AN AVERAGE GALAXY WHICH IS PART OF A GALAXY GROUP THAT WE WILL NEVER LEAVE AND THIS GROUP IS ONLY ONE OF A THOUSANDS THAT TOGETHER MAKE UP A GALAXY SUPERCLUSTER BUT EVEN OUT SUPERCLUSTER IS ONLY ONE IN THOUSANDS THAT MAKE UP WHAT WE CALL THE OBSERVABLE UNIVERSE THE UNIVERSE MIGHT BE A MILLION TIMES BIGGER BUT WE WILL NEVER KNOW WE COULD THROW WORDS AROUND LIKE TWO HUNDRED MILLION GALAXIES OR TRILLIONS OF STARS OR BAZILLIONS OF PLANETS BUT ALL OF THESE NUMBERS MEAN NOTHING OUR BRAINS CANT COMPREHEND THESE CONCEPTS THE UNIVERSE IS TOO BIG THERE IS TOO MUCH OF IT BUT SIZE IS NOT THE MOST TROUBLING CONCEPT WE HAVE TO DEAL WITH ITS TIME OR MORE PRECISELY THE TIME WE HAVE IF YOURE LUCKY ENOUGH TO LIVE TO ONE HUNDRED YOU HAVE FIVE THOUSAND TWO HUNDRED WEEKS AT YOUR DISPOSAL IF YOURE TWENTY FIVE NOW THEN YOU HAVE THREE THOUSAND NINE HUNDRED WEEKS LEFT IF YOURE GOING TO DIE AT SEVENTY THEN THERE ARE TWO THOUSAND THREE HUNDRED AND FORTY WEEKS LEFT A LOT OF TIME BUT ALSO NOT REALLY AND THEN WHAT? YOUR BIOLOGICAL PROCESSES WILL BREAK DOWN AND THE DYNAMIC PATTERN THAT IS YOU WILL STOP BEING DYNAMIC IT WILL DISSOLVE UNTIL THERE IS NO YOU LEFT SOME BELIEVE THAT THERE IS A PART OF US WE CANT SEE OR MEASURE BUT WE HAVE NO WAY TO FIND OUT SO THIS LIFE MIGHT BE IT AND WE MIGHT END UP DEAD FOREVER THIS IS LESS SCARY THAN IT SOUNDS THOUGH IF YOU DONT REMEMBER THE THIRTEEN POINT SEVEN FIVE BILLION YEARS THAT WENT BY BEFORE YOU EXISTED THEN THE TRILLIONS AND TRILLIONS AND TRILLIONS OF YEARS THAT COME AFTER WILL PASS IN NO TIME ONCE YOURE GONE CLOSE YOUR EYES COUNT TO ONE THATS HOW LONG FOREVER FEELS";

    readonly Dictionary<char, char> letterToSymbolTable = new Dictionary<char, char>() 
    {
        {'E', '#'}, {'F', '#'}, {'H', '#'}, {'L', '#'}, {'N', '#'}, {'T', '#'}, {'V', '#'}, {'W', '#'},
        {'B', '&'}, {'D', '&'}, {'G', '&'}, {'R', '&'}, {'S', '&'}, {'U', '&'},
        {'K', '%'}, {'M', '%'}, {'P', '%'}, {'X', '%'}, {'Z', '%'},
        {'A', '@'}, {'C', '@'}, {'O', '@'}, {'Q', '@'},
        {'I', '!'}, {'J', '!'}, {'Y', '!'}
    };

    [SerializeField] TextMesh[] plateWordTexts;


    int concatenatedSerialNumberDigit;
    List<string> voidedWords;

    string keywordFromStart;
    string keywordFromEnd;
    string dreadSequence;

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

        InitializePuzzle();

    }

    // public override void UpdateModule() { base.UpdateModule(); }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void PressingPlateButton(string buttonPressed)
    {
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }

        if (buttonPressed == dreadSequence)
        {
            summoningModule.ModuleLog(moduleId, "Pressed {0}, which is correct. Good job! Module defused!", buttonPressed);
            ModuleShouldSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Pressed {0}, which is incorrect. Expected {1}", buttonPressed, dreadSequence);
            ModuleShouldStrike();
        }

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
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        GenerateVoidedWords();
        ShowWordsOnModule();

        GetConcatenatedSerialNumberDigits();
        GatherKeywordsFromText();
        TransformDreadSequence();
    }

    void GenerateVoidedWords()
    {
        // Initialize List
        voidedWords = new List<string>();

        // Get a list of all unique words in the text
        List<string> _individualWords = optimisticNihilismText.Split(new char[] { ' ' }).Distinct().ToList();

        // Grab 5 random words without repeats
        voidedWords = _individualWords.Shuffle().GetRange(0, 5);

        summoningModule.ModuleLog(moduleId, "Voided words are {0}.", voidedWords.Join(" "));
    }

    void ShowWordsOnModule()
    {
        int _wordLength;

        for (int i = 0; i < 5; i ++)
        {
            // Apply word
            plateWordTexts[i].text = voidedWords[i];

            // Apply text size
            // Longest word is "SUPERCLUSTER" which is 12 characters long and should have a font size of 150
            // Shortest word is "A" which is 1 character long and should have a font size of 275
            // However up until 5 characters it should stay 275
            // So it's more 5-to-12 becomes 150-to-275... It's a lerp!
            _wordLength = Mathf.Clamp(voidedWords[i].Length, 5, 12);

            // This simplifies to   275 + (750 - 150) / 7, but the code below is clearer so I'll keep it
            plateWordTexts[i].fontSize = (int)Mathf.Lerp(275, 150,  Mathf.InverseLerp(5, 12, _wordLength));
        }
    }

    void GetConcatenatedSerialNumberDigits()
    {
        concatenatedSerialNumberDigit = bombInfo.GetSerialNumberNumbers().Join("").TryParseInt().GetValueOrDefault();
        summoningModule.ModuleLog(moduleId, "Concatenated Serial Number Digits gives {0}", concatenatedSerialNumberDigit);
    }

    void GatherKeywordsFromText()
    {
        // Remove voided keywords from the text.
        List<string> _voidlessText = optimisticNihilismText.Split(new char[]{' '}).Where(w => voidedWords.Contains(w) == false).ToList();


        // Gathered Concatenated Number is the word's NUMBER, not the word's Index
        // Modulo the concatenated numbers by the final number of words in the voidless text
        concatenatedSerialNumberDigit = (concatenatedSerialNumberDigit - 1 + _voidlessText.Count) % _voidlessText.Count;

        // This discrepency between Number and Index needs to be corrected in the logs, since internally we use Index, but we show Number to the user
        summoningModule.ModuleLog(moduleId, "After removing Voided words, the new text is:");
        summoningModule.ModuleLog(moduleId, _voidlessText.Join(" "));
        summoningModule.ModuleLog(moduleId, "Since it has a total of {0} words, we can modulo the concatenated numbers to become {1} (index {2}).",
            _voidlessText.Count, concatenatedSerialNumberDigit + 1, concatenatedSerialNumberDigit);


        keywordFromStart = _voidlessText[concatenatedSerialNumberDigit];
        keywordFromEnd = _voidlessText[_voidlessText.Count - concatenatedSerialNumberDigit - 1];

        dreadSequence = keywordFromEnd + keywordFromStart;
        dreadSequence = dreadSequence.Distinct().Join("");
        summoningModule.ModuleLog(moduleId, "The word number {0} from the start is {1}, while from the end it is {2}, so the concatenated duplicate-less word is {3}",
            concatenatedSerialNumberDigit + 1, keywordFromStart, keywordFromEnd, dreadSequence);

        char _replacingCharacter;
        string _finalDreadSequence = string.Empty;
        // Replace every letter by its "Dread Cipher" character (# & % @ !)
        for (int i = 0; i < dreadSequence.Length; i ++)
        {
            letterToSymbolTable.TryGetValue(dreadSequence[i], out _replacingCharacter);
            _finalDreadSequence += _replacingCharacter;
        }

        dreadSequence = _finalDreadSequence;
        summoningModule.ModuleLog(moduleId, "Converted to a Character Sequence, this becomes {0}", dreadSequence);
    }

    void TransformDreadSequence()
    {
        summoningModule.ModuleLog(moduleId, "Starting to apply rules.");

        int loopCount = 0;

        while (dreadSequence.Length != 1)
        {
            if (loopCount > 25)
            {
                summoningModule.ModuleLog(moduleId, "Infinite loop detected. More than 25 steps were done, which shouldn't be possible. To avoid soft-locks, the module will solve automatically.");
                ModuleShouldSolve();
                return;
            }

            loopCount++;

            // Apply rules, starting back from rule 1 after every change

            if (TryApplyRule1())
            {
                continue;
            }

            if (TryApplyRule2())
            {
                continue;
            }

            if (TryApplyRule3())
            {
                continue;
            }

            if (TryApplyRule4())
            {
                continue;
            }

            if (TryApplyRule5())
            {
                continue;
            }

            if (TryApplyRule6())
            {
                continue;
            }
        }

        summoningModule.ModuleLog(moduleId, "Final character to submit is {0}", dreadSequence);
    }

    bool TryApplyRule1()
    {
        // =-= Rule 1 =-=
        // If two of the same symbols are next to ech other, remove one of them

        // No need to check the last character
        for (int i = 0; i < dreadSequence.Length - 1; i ++)
        {
            if (dreadSequence[i] == dreadSequence[i+1])
            {
                dreadSequence = dreadSequence.Remove(i, 1);

                summoningModule.ModuleLog(moduleId, "Rule 1: Removed duplicate {0} at index {1}. New sequence is {2}",
                    dreadSequence[i], i, dreadSequence);

                return true;
            }
        }

        return false;
    }

    bool TryApplyRule2()
    {
        // =-= Rule 2 =-=
        // If an & is next to an @, replace both with a single %

        // No need to check the last character
        // Check for both & and @ at each step, so we don't need to check backwards
        for (int i = 0; i < dreadSequence.Length - 1; i++)
        {
            if ((dreadSequence[i] == '&' && dreadSequence[i + 1] == '@') || (dreadSequence[i] == '@' && dreadSequence[i + 1] == '&'))
            {
                dreadSequence = dreadSequence.Remove(i, 2).Insert(i, "%");

                summoningModule.ModuleLog(moduleId, "Rule 2: Replaced adjacent @& found at index {0} by a %. New sequence is {1}", i, dreadSequence);
                return true;
            }
        }

        return false;
    }

    bool TryApplyRule3()
    {
        // =-= Rule 3 =-=
        // If a ! is next to a #, remove the !

        // No need to check the last character
        // Check for both & and @ at each step, so we don't need to check backwards
        for (int i = 0; i < dreadSequence.Length - 1; i++)
        {
            if ((dreadSequence[i] == '!' && dreadSequence[i + 1] == '#'))
            {
                dreadSequence = dreadSequence.Remove(i, 1);

                summoningModule.ModuleLog(moduleId, "Rule 3: Removed ! at index {0} found adjacent to a #. New sequence is {1}", i, dreadSequence);
                return true;
            }
            else if ((dreadSequence[i + 1] == '!' && dreadSequence[i] == '#'))
            {
                dreadSequence = dreadSequence.Remove(i + 1, 1);

                summoningModule.ModuleLog(moduleId, "Rule 3: Removed ! at index {0} found adjacent to a #. New sequence is {1}", i + 1, dreadSequence);
                return true;
            }
        }

        return false;
    }

    bool TryApplyRule4()
    {
        // =-= Rule 4 =-=
        // If a @ is at the start or end of the Sequence, remove it

        if (dreadSequence[0] == '@')
        {
            dreadSequence = dreadSequence.Remove(0, 1);

            summoningModule.ModuleLog(moduleId, "FRule 4: Removed @ at the start of the Sequence. New sequence is {0}", dreadSequence);
            return true;
        }
        else if (dreadSequence[dreadSequence.Length - 1] == '@')
        {
            dreadSequence = dreadSequence.Remove(dreadSequence.Length - 1, 1);

            summoningModule.ModuleLog(moduleId, "Rule 4: Removed @ at the end of the Sequence. New sequence is {0}", dreadSequence);
            return true;
        }

        return false;
    }

    bool TryApplyRule5()
    {
        // =-= Rule 5 =-=
        // If there are more than 2 characters left, remove the leftmost #

        if (dreadSequence.Length <= 2)
        { return false; }

        if (dreadSequence.Contains('#') == false)
        { return false; }

        for (int i = 0; i < dreadSequence.Length; i++)
        {
            if (dreadSequence[i] == '#')
            {
                dreadSequence = dreadSequence.Remove(i, 1);

                summoningModule.ModuleLog(moduleId, "Rule 5: Removed leftmost # at index {0}. New sequence is {1}", i, dreadSequence);
                return true;
            }
        }
        

        return false;
    }

    bool TryApplyRule6()
    {
        // =-= Rule 6 =-=
        // Remove the leftmost symbol in this order: ! @ % & #

        if (dreadSequence.Contains('!'))
        {
            for (int i = 0; i < dreadSequence.Length; i++)
            {
                if (dreadSequence[i] == '!')
                {
                    dreadSequence = dreadSequence.Remove(i, 1);

                    summoningModule.ModuleLog(moduleId, "Rule 6: Removed ! at index {0}. New sequence is {1}", i, dreadSequence);
                    return true;
                }
            }
        }

        if (dreadSequence.Contains('@'))
        {
            for (int i = 0; i < dreadSequence.Length; i++)
            {
                if (dreadSequence[i] == '@')
                {
                    dreadSequence = dreadSequence.Remove(i, 1);

                    summoningModule.ModuleLog(moduleId, "Found Rule 6: Removed @ at index {0}. New sequence is {1}", i, dreadSequence);
                    return true;
                }
            }
        }

        if (dreadSequence.Contains('%'))
        {
            for (int i = 0; i < dreadSequence.Length; i++)
            {
                if (dreadSequence[i] == '%')
                {
                    dreadSequence = dreadSequence.Remove(i, 1);

                    summoningModule.ModuleLog(moduleId, "Found Rule 6: Removed % at index {0}. New sequence is {1}", i, dreadSequence);
                    return true;
                }
            }
        }

        if (dreadSequence.Contains('&'))
        {
            for (int i = 0; i < dreadSequence.Length; i++)
            {
                if (dreadSequence[i] == '&')
                {
                    dreadSequence = dreadSequence.Remove(i, 1);

                    summoningModule.ModuleLog(moduleId, "Found Rule 6: Removed & at index {0}. New sequence is {1}", i, dreadSequence);
                    return true;
                }
            }
        }

        if (dreadSequence.Contains('#'))
        {
            for (int i = 0; i < dreadSequence.Length; i++)
            {
                if (dreadSequence[i] == '#')
                {
                    dreadSequence = dreadSequence.Remove(i, 1);

                    summoningModule.ModuleLog(moduleId, "Found Rule 6: Removed # at index {0}. New sequence is {1}", i, dreadSequence);
                    return true;
                }
            }
        }


        return false;
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
            yield return "sendtochaterror {0} you must format the submission with “!{1} Submit #”";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} please make sure you Submit with either “Submit” or “s”.";
            yield break;
        }

        if (commandParts[1].Length != 1)
        {
            yield return "sendtochaterror {0} please sumbit only one of the 5 allowed characters: # & % @ !";
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

        switch (dreadSequence)
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

        yield break;
    }

}
