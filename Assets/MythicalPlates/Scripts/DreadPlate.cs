using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DreadPlate : PlateBase {

    /// <summary> The entire text, in uppercase, with dashes considered as spaces and apostrophes removed (you're => youre) </summary>
    readonly string optimisticNihilismText = "HUMAN EXISTENCE IS SCARY AND CONFUSING A FEW HUNDRED THOUSAND YEARS AGO WE BECAME CONSCIOUS AND FOUND OURSELVES IN A STRANGE PLACE IT WAS FILLED WITH OTHER BEINGS WE COULD EAT SOME SOME COULD EAT US THERE WAS LIQUID STUFF WE COULD DRINK THINGS WE COULD USE TO MAKE MORE THINGS THE DAYTIME SKY HAD A TINY YELLOW BALL THAT WARMED OUR SKIN THE NIGHT SKY WAS FILLED WITH BEAUTIFUL LIGHTS THIS PLACE WAS OBVIOUSLY MADE FOR US SOMETHING WAS WATCHING OVER US WE WERE HOME THIS MADE EVERYTHING MUCH LESS SCARY AND CONFUSING BUT THE OLDER WE GOT THE MORE WE LEARNED ABOUT THE WORLD AND OURSELVES WE LEARNED THAT THE TWINKLING LIGHTS ARE NOT SHINING BEAUTIFULLY FOR US THEY JUST ARE WE LEARNED THAT WERE NOT AT THE CENTER OF WHAT WE NOW CALL THE UNIVERSE AND THAT IT IS MUCH MUCH OLDER THAN WE THOUGHT WE LEARNED THAT WERE MADE OF MANY LITTLE DEAD THINGS WHICH MAKE UP BIGGER THINGS THAT ARE NOT DEAD FOR SOME REASON AND THAT WERE JUST ANOTHER TEMPORARY STAGE IN A HISTORY GOING BACK OVER A BILLION YEARS WE LEARNED IN AWE THAT WE LIVE ON A MOIST SPECK OF DUST MOVING AROUND A MEDIUM SIZED STAR IN A QUIET REGION OF ONE ARM OF AN AVERAGE GALAXY WHICH IS PART OF A GALAXY GROUP THAT WE WILL NEVER LEAVE AND THIS GROUP IS ONLY ONE OF A THOUSANDS THAT TOGETHER MAKE UP A GALAXY SUPERCLUSTER BUT EVEN OUT SUPERCLUSTER IS ONLY ONE IN THOUSANDS THAT MAKE UP WHAT WE CALL THE OBSERVABLE UNIVERSE THE UNIVERSE MIGHT BE A MILLION TIMES BIGGER BUT WE WILL NEVER KNOW WE COULD THROW WORDS AROUND LIKE TWO HUNDRED MILLION GALAXIES OR TRILLIONS OF STARS OR BAZILLIONS OF PLANETS BUT ALL OF THESE NUMBERS MEAN NOTHING OUR BRAINS CANT COMPREHEND THESE CONCEPTS THE UNIVERSE IS TOO BIG THERE IS TOO MUCH OF IT BUT SIZE IS NOT THE MOST TROUBLING CONCEPT WE HAVE TO DEAL WITH ITS TIME OR MORE PRECISELY THE TIME WE HAVE IF YOURE LUCKY ENOUGH TO LIVE TO ONE HUNDRED YOU HAVE FIVE THOUSAND TWO HUNDRED WEEKS AT YOUR DISPOSAL IF YOURE TWENTY FIVE NOW THEN YOU HAVE THREE THOUSAND NINE HUNDRED WEEKS LEFT IF YOURE GOING TO DIE AT SEVENTY THEN THERE ARE TWO THOUSAND THREE HUNDRED AND FORTY WEEKS LEFT A LOT OF TIME BUT ALSO NOT REALLY AND THEN WHAT YOUR BIOLOGICAL PROCESSES WILL BREAK DOWN AND THE DYNAMIC PATTERN THAT IS YOU WILL STOP BEING DYNAMIC IT WILL DISSOLVE UNTIL THERE IS NO YOU LEFT SOME BELIEVE THAT THERE IS A PART OF US WE CANT SEE OR MEASURE BUT WE HAVE NO WAY TO FIND OUT SO THIS LIFE MIGHT BE IT AND WE MIGHT END UP DEAD FOREVER THIS IS LESS SCARY THAN IT SOUNDS THOUGH IF YOU DONT REMEMBER THE THIRTEEN POINT SEVEN FIVE BILLION YEARS THAT WENT BY BEFORE YOU EXISTED THEN THE TRILLIONS AND TRILLIONS AND TRILLIONS OF YEARS THAT COME AFTER WILL PASS IN NO TIME ONCE YOURE GONE CLOSE YOUR EYES COUNT TO ONE THATS HOW LONG FOREVER FEELS";

    readonly Dictionary<char, char> letterToSymbolTable = new Dictionary<char, char>() 
    {
        {'F', '#'}, {'H', '#'}, {'L', '#'}, {'N', '#'}, {'T', '#'}, {'V', '#'}, {'W', '#'}, {'X', '#'},
        {'B', '&'}, {'D', '&'}, {'E', '&'}, {'G', '&'}, {'S', '&'}, {'U', '&'},
        {'K', '%'}, {'M', '%'}, {'P', '%'}, {'R', '%'}, {'Z', '%'},
        {'A', '@'}, {'C', '@'}, {'O', '@'}, {'Q', '@'},
        {'I', '!'}, {'J', '!'}, {'Y', '!'}
    };

    [SerializeField] TextMesh[] plateWordTexts;


    int concatenatedSerialNumberDigit;
    List<string> voidedWords;

    string keywordFromStart;
    string keywordFromEnd;
    string dreadSequence;
    string submittedPlayerSequence;

    int[] selectedRules;
    char[] selectedRulesSymbols;

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
        submittedPlayerSequence = "";

    }

    // public override void UpdateModule() { base.UpdateModule(); }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void PressingPlateButton(string buttonPressed)
    {
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { return; }

        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (hasPlateSolved)
        { return; }

        // Correct button pressed?
        if (dreadSequence[submittedPlayerSequence.Length] == buttonPressed[0])
        {
            submittedPlayerSequence += buttonPressed;

            if (submittedPlayerSequence == dreadSequence)
            {
                summoningModule.ModuleLog(moduleId, "Pressed {0}, which is correct. Full sequence submitted! Module defused!", buttonPressed);
                ModuleShouldSolve();
            }
            else
            {
                summoningModule.ModuleLog(moduleId, "Pressed {0}, which is correct. Currently submitted sequence is {1}", buttonPressed, submittedPlayerSequence);
            }
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Pressed {0}, which is incorrect. Expected {1}", buttonPressed, dreadSequence[submittedPlayerSequence.Length]);
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
        StartCoroutine(PlateShouldSolve());
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        ManageRuleseed();

        GenerateVoidedWords();
        ShowWordsOnModule();

        GetConcatenatedSerialNumberDigits();
        GatherKeywordsFromText();
        TransformDreadSequence();
    }

    void ManageRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        {
            selectedRules = new int[5] { 0, 1, 2, 3, 4 };
            selectedRulesSymbols = new char[15] { '!', '&', '%', '&', '@', '%', '!', '#', '@', '@', '!', '%', '#', '!', '%' };
            return;
        }

        string[] splitAlphabet = new string[26];
        Array.Copy(alphabet, splitAlphabet, 26);

        FisherYatesShuffle(ref splitAlphabet, Rng);

        char _associatedSymbol = '.';
        for (int i = 0; i < 26; i ++)
        {
            if (i < 8) { _associatedSymbol = '#'; }
            else if (i < 14) { _associatedSymbol = '&'; }
            else if (i < 19) { _associatedSymbol = '%'; }
            else if (i < 23) { _associatedSymbol = '@'; }
            else { _associatedSymbol = '!'; }

            letterToSymbolTable[splitAlphabet[i][0]] = _associatedSymbol;
        }


        // Log but in alphabetical order!

        string _joinedLetters = splitAlphabet.Join("");
        Debug.LogFormat("<Dread Plate #{0}> Letters associated with # are {1}, with & are {2}, with % are {3}, with @ are {4} and with ! are {5}", moduleId,
            _joinedLetters.Substring(0,8).OrderBy(x => x).Join(""),
            _joinedLetters.Substring(8, 6).OrderBy(x => x).Join(""),
            _joinedLetters.Substring(14, 5).OrderBy(x => x).Join(""),
            _joinedLetters.Substring(19, 4).OrderBy(x => x).Join(""),
            _joinedLetters.Substring(23, 3).OrderBy(x => x).Join(""));


        // rules 
        int[] _possibleRules = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Rng.ShuffleFisherYates(_possibleRules);
        selectedRules = _possibleRules.Take(5).ToArray();


        // characters
        // The most important rule is for the symbols to not repeat in the same rule
        // It'd be better if the symbols could also have an even distribution (All appear in position 1/2/3 only once)
        // but that's a double-exclusivity shuffle that I don't know how to do
        selectedRulesSymbols = new char[15];
        char[] possibleSymbols = new char[5] { '#', '&', '%', '@', '!' };
        for (int i = 0; i < 5; i++)
        {
            int[] selection = Rng.ShuffleFisherYates(new int[5] { 0, 1, 2, 3, 4 });
            selectedRulesSymbols[3 * i + 0] = possibleSymbols[selection[0]];
            selectedRulesSymbols[3 * i + 1] = possibleSymbols[selection[1]];
            selectedRulesSymbols[3 * i + 2] = possibleSymbols[selection[2]];
        }




        string[] rulesStrings = new string[10]
        {
            "If two of the same symbol are next to each other, remove one of them.",
            "If a {0} is next to a {1}, replace both with a single {2}.",
            "If a {0} is next to a {1}, remove the {0}.",
            "If a {0} is at the beginning or end of the Character Sequence, remove it.",
            "If there is a {0}, remove its leftmost occurence.",
            "If a {0} is NOT next to a {1} or {2}, remove it.",
            "If all Characters are unique, remove the last one.",
            "If a {0} is NOT at the beggining or end of the Character Sequence, remove it.",
            "If a {0} is next to a {1} and they have a neighbor, remove the character to their left, or right if there are none.",
            "If there is a {0}, replace its leftmost occurence by a {1}."
        };

        Debug.LogFormat("<Dread Plate #{0}> Selected Rules in order are:", moduleId);
        for (int i = 0; i < 5 ; i++)
        {
            Debug.LogFormat("<Dread Plate #{0}> {1}", moduleId, string.Format(rulesStrings[selectedRules[i]],
                selectedRulesSymbols[3 * i], selectedRulesSymbols[3 * i + 1], selectedRulesSymbols[3 * i + 2]));
        }
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

            if (TryApplyDreadCipherRule(0))
            {
               continue;
            }

            if (TryApplyDreadCipherRule(1))
            {
                continue;
            }

            if (TryApplyDreadCipherRule(2))
            {
                continue;
            }

            if (TryApplyDreadCipherRule(3))
            {
                continue;
            }
            
            if (TryApplyDreadCipherRule(4))
            {
                continue;
            }

            break;
        }

        summoningModule.ModuleLog(moduleId, "Final character sequence to submit is {0}", dreadSequence);
    }

    bool TryApplyDreadCipherRule(int ruleKeyIndex)
    {
        int ruleIndex = selectedRules[ruleKeyIndex];
        char characterOne = selectedRulesSymbols[3 * ruleKeyIndex];
        char characterTwo = selectedRulesSymbols[3 * ruleKeyIndex + 1];
        char characterThree = selectedRulesSymbols[3 * ruleKeyIndex + 2];

        switch (ruleIndex)
        {
            default: return false;

            case 0:
                {
                    // If two of the same symbols are next to ech other, remove one of them

                    // No need to check the last character
                    for (int i = 0; i < dreadSequence.Length - 1; i++)
                    {
                        if (dreadSequence[i] == dreadSequence[i + 1])
                        {
                            dreadSequence = dreadSequence.Remove(i, 1);

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Removed duplicate {1} at index {2}. New sequence is {3}",
                                ruleKeyIndex + 1, dreadSequence[i], i, dreadSequence);

                            return true;
                        }
                    }

                    return false;
                }


            case 1:
                {
                    // If a A is next to an B, replace both with a single C

                    // No need to check the last character
                    // Check for both & and @ at each step, so we don't need to check backwards
                    for (int i = 0; i < dreadSequence.Length - 1; i++)
                    {
                        if ((dreadSequence[i] == characterOne && dreadSequence[i + 1] == characterTwo) || (dreadSequence[i] == characterTwo && dreadSequence[i + 1] == characterOne))
                        {
                            dreadSequence = dreadSequence.Remove(i, 2).Insert(i, characterThree.ToString());

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Replaced adjacent {1}{2} found at index {3} by a {4}. New sequence is {5}",
                                ruleKeyIndex + 1, characterOne, characterTwo, i, characterThree, dreadSequence);
                            return true;
                        }
                    }

                    return false;
                }

            case 2: 
                {
                    // If a A is next to a B, remove the A

                    // No need to check the last character
                    // Check for both A and B at each step, so we don't need to check backwards
                    for (int i = 0; i < dreadSequence.Length - 1; i++)
                    {
                        if ((dreadSequence[i] == characterOne && dreadSequence[i + 1] == characterTwo))
                        {
                            dreadSequence = dreadSequence.Remove(i, 1);

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Removed {1} at index {2} found adjacent to a {3}. New sequence is {4}",
                                ruleKeyIndex + 1, characterOne, i, characterTwo, dreadSequence);
                            return true;
                        }
                        else if ((dreadSequence[i + 1] == characterOne && dreadSequence[i] == characterTwo))
                        {
                            dreadSequence = dreadSequence.Remove(i + 1, 1);

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Removed {1} at index {2} found adjacent to a {3}. New sequence is {4}",
                                 ruleKeyIndex + 1, characterOne, i+1, characterTwo, dreadSequence);
                            return true;
                        }
                    }

                    return false;
                }

            case 3:
                {
                    // If a A is at the start or end of the Sequence, remove it

                    if (dreadSequence[0] == characterOne)
                    {
                        dreadSequence = dreadSequence.Remove(0, 1);

                        summoningModule.ModuleLog(moduleId, "Rule {0}: Removed {1} at the start of the Sequence. New sequence is {2}",
                            ruleKeyIndex + 1, characterOne, dreadSequence);
                        return true;
                    }
                    else if (dreadSequence[dreadSequence.Length - 1] == characterOne)
                    {
                        dreadSequence = dreadSequence.Remove(dreadSequence.Length - 1, 1);

                        summoningModule.ModuleLog(moduleId, "Rule {0}: Removed {1} at the end of the Sequence. New sequence is {2}",
                            ruleKeyIndex + 1, characterOne, dreadSequence);
                        return true;
                    }

                    return false;
                }

            case 4:
                {
                    // If there is a A, remove the leftmost A

                    if (dreadSequence.Contains(characterOne) == false)
                    { return false; }

                    for (int i = 0; i < dreadSequence.Length; i++)
                    {
                        if (dreadSequence[i] == characterOne)
                        {
                            dreadSequence = dreadSequence.Remove(i, 1);

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Removed leftmost {1} at index {2}. New sequence is {3}",
                                ruleKeyIndex + 1, characterOne, i, dreadSequence);
                            return true;
                        }
                    }


                    return false;
                }

            case 5:
                {
                    // If a A is NOT close to a B nor a C, remove the A

                    // No need to check the last character
                    // Check for both & and @ at each step, so we don't need to check backwards
                    for (int i = 0; i < dreadSequence.Length - 1; i++)
                    {
                        // i = A && (i+1 == B NOR i+1 == C)
                        if (dreadSequence[i] == characterOne)
                        {
                            if ((dreadSequence[i+1] == characterTwo || dreadSequence[i+1] == characterThree) == false)
                            {
                                dreadSequence = dreadSequence.Remove(i, 1);

                                summoningModule.ModuleLog(moduleId, "Rule {0}: removed {1} found at index {2} since not next to {3} nor {4}. New sequence is {5}",
                                    ruleKeyIndex + 1, characterOne, i, characterTwo, characterThree, dreadSequence);

                                return true;
                            }
                        }
                        // (i == B NOR i == C) && i+1 = A
                        else if ((dreadSequence[i] == characterTwo || dreadSequence[i] == characterThree) == false)
                        {
                            if (dreadSequence[i+1] == characterOne)
                            {
                                dreadSequence = dreadSequence.Remove(i + 1, 1);

                                summoningModule.ModuleLog(moduleId, "Rule {0}: removed {1} found at index {2} since not next to {3} nor {4}. New sequence is {5}",
                                    ruleKeyIndex + 1, characterOne, i+1, characterTwo, characterThree, dreadSequence);

                                return true;
                            }
                        }
                    }

                    return false;

                }

            case 6:
                {
                    // If all characters are unique, remove the last one
                    if (dreadSequence.Distinct().Count() == dreadSequence.Length)
                    {
                        dreadSequence = dreadSequence.Remove(dreadSequence.Length - 1);

                        summoningModule.ModuleLog(moduleId, "Rule {0}: All characters are unique, removing last one. New sequence is {1}",
                                    ruleKeyIndex + 1, dreadSequence);

                        return true;
                    }

                    return false;
                }


            case 7:
                {
                    // If a A is NOT at the start or end, remove it

                    for (int i = 1; i < dreadSequence.Length - 1; i++)
                    {
                        if (dreadSequence[i] == characterOne)
                        {
                            dreadSequence = dreadSequence.Remove(i, 1);

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Removed {1} in index {2} NOT at the beginning or end of sequence. New sequence is {3}",
                                ruleKeyIndex + 1, characterOne, i, dreadSequence);
                            return true;
                        }
                    }

                    return false;
                }

            case 8:
                {
                    // If a A is next to a B and they have a neighbor, remove the character to their left, else right
                    if (dreadSequence.Length < 3) { return false; }

                    // No need to check the last character
                    // Check for both A and B at each step, so we don't need to check backwards
                    for (int i = 0; i < dreadSequence.Length - 1; i++)
                    {
                        if ((dreadSequence[i] == characterOne && dreadSequence[i + 1] == characterTwo) || (dreadSequence[i + 1] == characterOne && dreadSequence[i] == characterTwo))
                        {
                            if (i == 0)
                            {
                                // Remove one on the right!
                                dreadSequence = dreadSequence.Remove(i + 2, 1);

                                summoningModule.ModuleLog(moduleId, "Rule {0}: Removed character at index {1}, on the right of the adjacent {2}{3}. New sequence is {4}",
                                ruleKeyIndex + 1, i + 2, characterOne, characterTwo, dreadSequence);
                                return true;
                            }
                            else
                            {
                                // Remove one on the left!
                                dreadSequence = dreadSequence.Remove(i - 1, 1);

                                summoningModule.ModuleLog(moduleId, "Rule {0}: Removed character at index {1}, on the left of the adjacent {2}{3}. New sequence is {4}",
                                ruleKeyIndex + 1, i - 1, characterOne, characterTwo, dreadSequence);
                                return true;
                            }
                        }
                    }

                    return false;
                }

            case 9:
                {
                    // If there is a A, replace its leftmost occurence by a B
                    for (int i = 0; i < dreadSequence.Length; i++)
                    {
                        if (dreadSequence[i] == characterOne)
                        {
                            dreadSequence = dreadSequence.Remove(i, 1).Insert(i, characterTwo.ToString());

                            summoningModule.ModuleLog(moduleId, "Rule {0}: Removed {1} at index {2} and replaced it with a {3}. New sequence is {4}",
                                ruleKeyIndex + 1, characterOne, i, characterTwo, dreadSequence);
                            return true;
                        }
                    }

                    return false;
                }
        }
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
        // Due to Allmighty Sinnoh TP Autosolve, we're never safe from Plates being destroyed but still calling code
        if (this == null) { yield break; }

        Debug.LogFormat("<Dread Plate #{0}> Received Command ''{1}''", moduleId, command);
        if (hasPlateSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        if (commandParts.Length != 2)
        {
            Debug.LogFormat("<Dread Plate #{0}> You must format the submission with “!# Submit %”", moduleId);
            yield return "sendtochaterror {0} You must format the submission with “!{1} Submit %”";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            Debug.LogFormat("<Dread Plate #{0}> Please make sure you Submit with either “Submit” or “s”.”", moduleId);
            yield return "sendtochaterror {0} Please make sure you Submit with either “Submit” or “s”.";
            yield break;
        }

        if (commandParts[1].Length != 1)
        {
            Debug.LogFormat("<Dread Plate #{0}> Please sumbit only one of the 5 allowed characters: # & % @ !", moduleId);
            yield return "sendtochaterror {0} Please sumbit only one of the 5 allowed characters: # & % @ !";
        }

        Debug.LogFormat("<Dread Plate #{0}> Trying to press {1}", moduleId, commandParts[1]);

        switch (commandParts[1])
        {
            case "#":
                yield return null;
                platePressableButtons[0].OnInteract();
                break;

            case "&":
                yield return null;
                platePressableButtons[1].OnInteract();
                break;

            case "%":
                yield return null;
                platePressableButtons[2].OnInteract();
                break;

            case "@":
                yield return null;
                platePressableButtons[3].OnInteract();
                break;

            case "!":
                yield return null;
                platePressableButtons[4].OnInteract();
                break;

            default:
                Debug.LogFormat("<Dread Plate #{0}> Received unknown character '{1}'. Please only use one of the 5 allowed characters: # & % @ !", moduleId, commandParts[1]);
                yield return "sendtochaterror {0} Received unknown character '" + commandParts[1] + "'. Please only use one of the 5 allowed characters: # & % @ !";
                break;
        }
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
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
    }

}
