using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class InsectPlate : PlateBase {

    /*
    * Ruleseed Support:
    * 
    * Questions can be Randomized
    */


    /* List of all Questions:
     * 
     * LastDigitOfSnParity
     *      Is the last digit of the Serial Number even/odd?
     *      Payload is 0 if Even, 1 if Odd
     * MinNonNeedyModuleCount
     *      Are there 10 or more non-needy modules?
     *      Payload is number to check against, 5-50 in steps of 5
     * MinBatteryCount
     *      Are there 3 or more batteries?
     *      Payload is number to check against, 1 to 3
     * SpecificBatteryPresent
     *      Is there any AA-type battery?
     *      Payload is 0 for AA or 1 for D
     * MinIndicatorPresent
     *      Are there 3 or more indicators?
     *      Payload is number to check against, 1 to 3
     * SpecificIndicatorPresent
     *      Is there a BOB Indicator?
     *      Payload is which indicator to compare against, 0 to 10
     * MinPortPresent
     *      Are there 3 or more ports?
     *      Payload is number to check against, 1 to 3
     * SpecificPortPresent
     *      Is there a Parallel port?
     *      Payload is which port to compare against, 0 to 5
     * LetterPresentInSn
     *      Is the letter E present in the Serial Number?
     *      Payload is which letter in 0-25 (without 14 nor 24 since O and Y can't appear)
     * DigitPresentInSn
     *      Is the digit 7 present in the Serial Number?
     *      Payload is which digit to compare against
     * DuplicatePortPresent
     *      Are there any duplicate ports?
     * DuplicateSnCharacterPresent
     *      Are there any duplicate characters in the Serial Number?
     * NumberOfLettersInSn
     *      Does the Serial Number have exactly 3 Letters?
     *      Payload is number of letters to check against, 2 to 4
     * SnContainsVowel
     *      Does the Serial Number contain a vowel? (Y and W are not vowels)
     * EmptyPortPlatePresent
     *      Is there an empty port plate?
     * HasStruckBefore
     *      Does this bomb have 1 or more Strikes?
     * NoSolvedModules
     *      Are there 0 solved Modules?
     * SimonSpidersPresent
     *      Is the module Simon's Spider present?
     * FlyswattingPresent
     *      Is the module Flyswatting present?
     * LangtonsAntPresent
     *      Is the module Langton's Ant present?
     * ButterfliesPresent
     *      Is the module Butterflies present?
     * SummonedByAllmightySinnoh
     *      Is this Plate summoned by the module Allmighty Sinnoh?
     * OtherMythicalPlatePresent
     *      Is another Mythical Plate present?
     * SnContainsInsect
     *      Does the Serial Number contain a letter from "INSECT"?
     * NoBatteriesPresent
     *      Are there no batteries?
     * NoPortsPresent
     *      Are there no ports?
     * NoIndicatorsPresent
     *      Are there no indicators?
     * SameLitsAsUnlits
     *      Is there the same number of Lits and Unlits indicators?
    */

    /// <summary> List of all possible Questions </summary>
    enum Question { LastDigitOfSnParity, MinNonNeedyModuleCount, MinBatteryCount, SpecificBatteryPresent, MinIndicatorPresent, SpecificIndicatorPresent,
        MinPortPresent, SpecificPortPresent, LetterPresentInSn, DigitPresentInSn, DuplicatePortPresent, DuplicateSnCharacterPresent, NumberOfLettersInSn, SnContainsVowel,
        EmptyPortPlatePresent, HasStruckBefore, NoSolvedModules, SimonSpidersPresent, FlyswattingPresent, LangtonsAntPresent, ButterfliesPresent, SummonedByAllmightySinnoh,
        OtherMythicalPlatePresent, SnContainsInsect, NoBatteriesPresent, NoPortsPresent, NoIndicatorsPresent, SameLitsAsUnlits }

    /// <summary> The 14 selected Questions, using Ruleseed </summary>
    Question[] selectedQuestions = new Question[14];
    /// <summary> Potential Payload for those questions. Can be letter index for "is E in the SN?" or Port name </summary>
    int[] questionPayloads = new int[14];

    readonly string[] possibleIndicators = new string[11] { "SND", "CLR", "CAR", "IND", "FRQ", "SIG", "NSA", "MSA", "TRN", "BOB", "FRK" };
    readonly string[] possiblePorts = new string[6] { "Parallel", "Serial", "DVI", "PS2", "RJ45", "StereoRCA" };


    char[] voidedLetters;

    int pressedButtonDirection = 0;

    // Universal Logging Data
    static int moduleIdCounter = 1;

    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        
        platePressableButtons[0].OnInteract += delegate () { PlayerPressedButton(0); return false; };
        platePressableButtons[1].OnInteract += delegate () { PlayerPressedButton(1); return false; };
        platePressableButtons[2].OnInteract += delegate () { PlayerPressedButton(2); return false; };
        platePressableButtons[3].OnInteract += delegate () { PlayerPressedButton(3); return false; };
        platePressableButtons[4].OnInteract += delegate () { PlayerPressedButton(4); return false; };

    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        InitializePuzzle();
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Input
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    /// <summary> 0 for Up, 1 for Down, 2 for Left, 3 for Right, 4 for Center </summary>
    void PlayerPressedButton(int buttonIndex)
    {
        platePressableButtons[0].AddInteractionPunch();
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved)
        { return; }


        pressedButtonDirection = buttonIndex;
        summoningModule.ModuleLog(moduleId, "Pressed button with direction {0}.", GetReadableButtonDirection(pressedButtonDirection));

        // Then, check the questions one by one!!
        CheckQuestion('A');
    }

    string GetReadableButtonDirection(int buttonIndex)
    {
        switch (buttonIndex)
        {
            case 0: return "Up";
            case 1: return "Down";
            case 2: return "Left";
            case 3: return "Right";
            case 4: return "Center";
        }
        return "UNKNOWN-" + buttonIndex;
    }

    /// <summary> 0 for Up, 1 for Down, 2 for Left, 3 for Right, 4 for Center </summary>
    void PressedButtonResult(int expectedButtonDirection)
    {
        if (expectedButtonDirection == pressedButtonDirection)
        {
            summoningModule.ModuleLog(moduleId, "Expected to press button {0}; which is what you pressed!!", GetReadableButtonDirection(expectedButtonDirection));
            StartCoroutine(PlateShouldSolve());
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Expected to press button {0}; but you pressed {1}!",
                GetReadableButtonDirection(expectedButtonDirection), GetReadableButtonDirection(pressedButtonDirection));
            summoningModule.ReceiveStrike();
        }
        
    }


    void CheckQuestion(char questionLetter)
    {
        switch (questionLetter)
        {
            // Question A cannot be Voided
            case 'A':
                // A true => B
                // A false => C
                if (ComputeQuestionAnswer(0) == true) { CheckQuestion('B'); }
                else { CheckQuestion('C'); }
                return;

            case 'B':
                // B true OR B void => D
                // B false => E
                if (ComputeQuestionAnswer(1) == true || voidedLetters.Contains('B')) { CheckQuestion('D'); }
                else { CheckQuestion('E'); }
                return;

            case 'C':
                // C false OR C void => F
                // C true => Should have Pressed Center!
                if (ComputeQuestionAnswer(2) == false || voidedLetters.Contains('C')) { CheckQuestion('F'); }
                else { PressedButtonResult(4); }
                return;

            case 'D':
                // D true OR D void => Should have pressed Up!
                // D false => G
                if (ComputeQuestionAnswer(3) == true || voidedLetters.Contains('D')) { PressedButtonResult(0); }
                else { CheckQuestion('G'); }
                return;

            case 'E':
                // E false OR E void => G
                // E true => H
                if (ComputeQuestionAnswer(4) == false || voidedLetters.Contains('E')) { CheckQuestion('G'); }
                else { CheckQuestion('H'); }
                return;

            case 'F':
                // F false OR F void => I
                // F true => H
                if (ComputeQuestionAnswer(5) == false || voidedLetters.Contains('F')) { CheckQuestion('I'); }
                else { CheckQuestion('H'); }
                return;

            case 'G':
                // G false OR G void => J
                // G true => K
                if (ComputeQuestionAnswer(6) == false || voidedLetters.Contains('G')) { CheckQuestion('J'); }
                else { CheckQuestion('K'); }
                return;

            case 'H':
                // H true OR H void => K
                // H false => Should have pressed Right!
                if (ComputeQuestionAnswer(7) == true || voidedLetters.Contains('G')) { CheckQuestion('K'); }
                else { PressedButtonResult(3); }
                return;

            case 'I':
                // I false OR I void => Should have pressed Down!
                // I true => L
                if (ComputeQuestionAnswer(8) == false || voidedLetters.Contains('I')) { PressedButtonResult(1); }
                else { CheckQuestion('L'); }
                return;

            case 'J':
                // J false Or J void => Should have pressed Left!
                // J true => Should have pressed Right!
                if (ComputeQuestionAnswer(9) == false || voidedLetters.Contains('J')) { PressedButtonResult(2); }
                else { PressedButtonResult(3); }
                return;

            case 'K':
                // K true OR K void => Should have pressed Left!
                // K false => Should have pressed Up!
                if (ComputeQuestionAnswer(10) == true || voidedLetters.Contains('K')) { PressedButtonResult(2); }
                else { PressedButtonResult(0); }
                return;

            case 'L':
                // L true or L void => M
                // L false => N
                if (ComputeQuestionAnswer(11) == true || voidedLetters.Contains('L')) { CheckQuestion('M'); }
                else { CheckQuestion('N'); }
                return;

            case 'M':
                // M true or M void => Should have pressed Center!
                // M false => Should have pressed Down!
                if (ComputeQuestionAnswer(12) == true || voidedLetters.Contains('M')) { PressedButtonResult(4); }
                else { PressedButtonResult(1); }
                return;

            case 'N':
                // N false or N void => Should have pressed Down!
                // N true => Should have pressed Left!
                if (ComputeQuestionAnswer(13) == false|| voidedLetters.Contains('N')) { PressedButtonResult(1); }
                else { PressedButtonResult(2); }
                return;
        }
    }

    protected override void CasingTextButtonGetsPressed() { }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        ShuffleQuestionsWithRuleseed();
        InitializeVoidedQuestions();

        summoningModule.ModuleLog(moduleId, "Questions initialized. Waiting for Player Input.");
    }

    void ShuffleQuestionsWithRuleseed()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();

        summoningModule.ModuleLog(moduleId, "Using Ruleseed {0}:", Rng.Seed);

        if (Rng.Seed == 1)
        {
            InitializeDefaultQuestions();
            return;
        }

        Question[] allPossibleQuestions = new Question[28] { Question.LastDigitOfSnParity, Question.MinNonNeedyModuleCount,
            Question.MinBatteryCount, Question.SpecificBatteryPresent, Question.MinIndicatorPresent, Question.SpecificIndicatorPresent,
            Question.MinPortPresent, Question.SpecificPortPresent, Question.LetterPresentInSn, Question.DigitPresentInSn,
            Question.DuplicatePortPresent, Question.DuplicateSnCharacterPresent, Question.NumberOfLettersInSn, Question.SnContainsVowel,
            Question.EmptyPortPlatePresent, Question.HasStruckBefore, Question.NoSolvedModules, Question.SimonSpidersPresent,
            Question.FlyswattingPresent,Question.LangtonsAntPresent, Question.ButterfliesPresent, Question.SummonedByAllmightySinnoh,
            Question.OtherMythicalPlatePresent, Question.SnContainsInsect, Question.NoBatteriesPresent, Question.NoPortsPresent,
            Question.NoIndicatorsPresent, Question.SameLitsAsUnlits};

        FisherYatesShuffle(ref allPossibleQuestions, Rng);

        for (int i = 0; i < 14; i ++)
        {
            selectedQuestions[i] = allPossibleQuestions[i];
            questionPayloads[i] = GeneratePayloadForQuestion(selectedQuestions[i], Rng);

            summoningModule.ModuleLog(moduleId, "Question {0} will be “{1}”.", alphabet[i], GetReadableQuestionString(i));
        }
    }

    /// <summary> Some Questions have a separate payload to further randomize them. This function generates them. </summary>
    int GeneratePayloadForQuestion(Question questionType, MonoRandom Rng)
    {
        switch(questionType)
        {
            case Question.LastDigitOfSnParity: // Random 0-1
                return Rng.Next(0, 2);
            case Question.MinNonNeedyModuleCount: // Random 5-50 in steps of 5
                return Rng.Next(1, 11) * 5;
            case Question.MinBatteryCount: // Random 1-3
                return Rng.Next(1, 4);
            case Question.SpecificBatteryPresent: // Random 0-1
                return Rng.Next(0, 2);
            case Question.MinIndicatorPresent: // Random 1-3
                return Rng.Next(1, 4);
            case Question.SpecificIndicatorPresent: // Random 0-10
                return Rng.Next(0, 11);
            case Question.MinPortPresent: // Random 1-3
                return Rng.Next(1, 4);
            case Question.SpecificPortPresent: // Random 0-6
                return Rng.Next(0, 6);
            case Question.LetterPresentInSn: // Random 0-25 without 14 nor 24 (return E in this case)
                int _return = Rng.Next(0, 26);
                return (_return == 14 || _return == 24) ? 4 : _return;
            case Question.DigitPresentInSn: // Random 0-9
                return Rng.Next(0, 10);
            case Question.NumberOfLettersInSn: // Random 2-4
                return Rng.Next(2, 5);
        }

        return 0;
    }

    string GetReadableQuestionString(int questionId)
    {
        Question questionType = selectedQuestions[questionId];
        int payload = questionPayloads[questionId];

        switch (questionType)
        {
            case Question.LastDigitOfSnParity:
                return "Is the last digit of the Serial Number " + (payload == 0 ? "even" : "odd") + "?";
            case Question.MinNonNeedyModuleCount:
                return "Are there " + payload + " or more non-needy modules?";
            case Question.MinBatteryCount:
                return "Are there " + payload + " or more batteries?";
            case Question.SpecificBatteryPresent:
                return "Is there any " + (payload == 0 ? "AA" : "D") + "-type battery?";
            case Question.MinIndicatorPresent:
                return "Are there " + payload + " or more indicators?";
            case Question.SpecificIndicatorPresent:
                return "Is a " + possibleIndicators[payload] + " indicator present?";
            case Question.MinPortPresent:
                return "Are there " + payload + " or more ports?";
            case Question.SpecificPortPresent:
                return "Is a " + possiblePorts[payload] + " port present?";
            case Question.LetterPresentInSn:
                return "Is the letter " + alphabet[payload] + " present in the Serial Number?";
            case Question.DigitPresentInSn:
                return "Is the digit " + payload + " present in the Serial Number?";
            case Question.DuplicatePortPresent:
                return "Are there any duplicate port types?";
            case Question.DuplicateSnCharacterPresent:
                return "Are there any duplicate characters in the Serial Number?";
            case Question.NumberOfLettersInSn:
                return "Does the Serial Number have exactly " + payload + " letters?";
            case Question.SnContainsVowel:
                return "Does the Serial Number contain a vowel? (W and Y don't count)";
            case Question.EmptyPortPlatePresent:
                return "Is there an empty port plate?";
            case Question.HasStruckBefore:
                return "Does this bomb have 1 or more strikes?";
            case Question.NoSolvedModules:
                return "Are there 0 solved modules?";
            case Question.SimonSpidersPresent:
                return "Is the module Simon's Spider present?";
            case Question.FlyswattingPresent:
                return "Is the module Flyswatting present?";
            case Question.LangtonsAntPresent:
                return "Is the module Langton's Ant present?";
            case Question.ButterfliesPresent:
                return "Is the module Butterflies present?";
            case Question.SummonedByAllmightySinnoh:
                return "Is this Plate summoned by the module Allmighty Sinnoh?";
            case Question.OtherMythicalPlatePresent:
                return "Is another Mythical Plate module present (other Insect Plates count, Allmighty Sinnoh doesn't)?";
            case Question.SnContainsInsect:
                return "Does the Serial Number contain a letter from “INSECT”?";
            case Question.NoBatteriesPresent:
                return "Are there no batteries?";
            case Question.NoPortsPresent:
                return "Are there no ports?";
            case Question.NoIndicatorsPresent:
                return "Are there no indicators?";
            case Question.SameLitsAsUnlits:
                return "Is there the same number of lit and unlit indicators?";
        }

        return "UNKNOWN QUESTION " + questionType.ToString();
    }

    bool ComputeQuestionAnswer(int questionId)
    {
        Question questionType = selectedQuestions[questionId];
        int payload = questionPayloads[questionId];

        bool validity = true;
        
        switch (questionType)
        {
            case Question.LastDigitOfSnParity: // Last digit of SN is Even(0) or Odd(1)
                validity = bombInfo.GetSerialNumberNumbers().Last() % 2 == (payload == 0 ? 0 : 1);
                break;

            case Question.MinNonNeedyModuleCount: // X or more non-needy
                validity = bombInfo.GetSolvableModuleIDs().Count >= payload;
                break;

            case Question.MinBatteryCount: // X or more batteries
                validity = bombInfo.GetBatteryCount() >= payload;
                break;

            case Question.SpecificBatteryPresent: // Is there AA-type(0) or D-type(1) battery? 
                validity = bombInfo.GetBatteryCount( (payload == 0 ? Battery.AA : Battery.D) ) > 0;
                break;

            case Question.MinIndicatorPresent: // X or more indicators
                validity = bombInfo.GetIndicators().Count() >= payload;
                break;

            case Question.SpecificIndicatorPresent: // BOB indicator is present (type determined by payload)
                validity = bombInfo.GetIndicators().Contains(possibleIndicators[payload]);
                break;

            case Question.MinPortPresent: // X or more ports
                validity = bombInfo.GetPortCount() >= payload;
                break;

            case Question.SpecificPortPresent: // PS2 port present (type determined by payload)
                validity = bombInfo.GetPorts().Contains(possiblePorts[payload]);
                break;

            case Question.LetterPresentInSn: // Specific letter(payload) is present in SN
                validity = bombInfo.GetSerialNumberLetters().Contains(alphabet[payload][0]);
                break;

            case Question.DigitPresentInSn: // Specific digit(payload) is present in SN
                validity = bombInfo.GetSerialNumberNumbers().Contains(payload);
                break;

            case Question.DuplicatePortPresent: // Duplicate port present
                validity = bombInfo.GetPorts().Distinct().Count() != bombInfo.GetPortCount();
                break;

            case Question.DuplicateSnCharacterPresent: // Duplicate SN character
                validity = bombInfo.GetSerialNumber().Distinct().Count() != 6;
                break;

            case Question.NumberOfLettersInSn: // Is there X number of letters in SN
                validity = bombInfo.GetSerialNumberLetters().Count() == payload;
                break;

            case Question.SnContainsVowel: // Is there a vowel
                validity = Regex.IsMatch(bombInfo.GetSerialNumber(), "[AEIOU]");
                break;

            case Question.EmptyPortPlatePresent: // Is there an empty port plate
                validity = bombInfo.GetPortPlates().Any(x => x.Length == 0);
                break;

            case Question.HasStruckBefore: // Has any strikes
                validity = bombInfo.GetStrikes() > 0;
                break;

            case Question.NoSolvedModules: // No modules have been solved yet
                validity = bombInfo.GetSolvedModuleIDs().Count == 0;
                break;

            case Question.SimonSpidersPresent: // Module Simon's Spider present
                validity = bombInfo.GetSolvableModuleIDs().Contains("SimonsSpiderModule");
                break;

            case Question.FlyswattingPresent: // Module Flyswatting present
                validity = bombInfo.GetSolvableModuleIDs().Contains("flyswatting");
                break;

            case Question.LangtonsAntPresent: // Module Langton's Ant present
                validity = bombInfo.GetSolvableModuleIDs().Contains("langtonAnt");
                break;

            case Question.ButterfliesPresent: // Module Butterflies present
                validity = bombInfo.GetSolvableModuleIDs().Contains("xelButterflies");
                break;

            case Question.SummonedByAllmightySinnoh: // Plate is summoned by Allmighty Sinnoh
                validity = SummonedByAllmightySinnoh();
                break;

            case Question.OtherMythicalPlatePresent: // Another Plate is present, including another InsectPlate
                List<string> _modules = bombInfo.GetSolvableModuleIDs();
                _modules.Remove("InsectPlate");
                string[] mythicalPlates = new string[18] { "BlankPlate", "FistPlate", "SkyPlate", "ToxicPlate", "EarthPlate", "StonePlate", "InsectPlate", "SpookyPlate",
                "FlamePlate", "SplashPlate", "IronPlate", "MeadowPlate", "ZapPlate", "MindPlate", "IciclePlate", "DracoPlate", "DreadPlate", "PixiePlate"};
                validity = _modules.Intersect(mythicalPlates).Count() > 0;
                break;

            case Question.SnContainsInsect: // Serial Number contains a letter from "INSECT"
                validity = Regex.IsMatch(bombInfo.GetSerialNumber(), "[INSECT]");
                break;

            case Question.NoBatteriesPresent: // No Batteries
                validity = bombInfo.GetBatteryCount() == 0;
                break;

            case Question.NoPortsPresent: // No Ports
                validity = bombInfo.GetPortCount() == 0;
                break;

            case Question.NoIndicatorsPresent: // No Indicators
                validity = bombInfo.GetIndicators().Count() == 0;
                break;

            case Question.SameLitsAsUnlits: // As many Lits as Unlits
                validity = bombInfo.GetOnIndicators().Count() == bombInfo.GetOffIndicators().Count();
                break;
        }

        // Log then return
        summoningModule.ModuleLog(moduleId, "Question {0}: answer is {1}", alphabet[questionId], validity);
        return validity;
    }


    void InitializeDefaultQuestions()
    {
        // Default 14 Questions:
        //
        // A: Is Vowel Present
        selectedQuestions[0] = Question.SnContainsVowel;

        // B: LitsAndUnlits
        selectedQuestions[1] = Question.SameLitsAsUnlits;

        // C: Serial Port present
        selectedQuestions[2] = Question.SpecificPortPresent;
        questionPayloads[2] = 1;

        // D: Empty Plate
        selectedQuestions[3] = Question.EmptyPortPlatePresent;

        // E: StrikesPresent
        selectedQuestions[4] = Question.HasStruckBefore;

        // F: DuplicateSn
        selectedQuestions[5] = Question.DuplicateSnCharacterPresent;

        // G: Letter from INSECT
        selectedQuestions[6] = Question.SnContainsInsect;

        // H: LastDigitEven
        selectedQuestions[7] = Question.LastDigitOfSnParity;
        questionPayloads[7] = 0;

        // I: 10 or more non-needy
        selectedQuestions[8] = Question.MinNonNeedyModuleCount;
        questionPayloads[8] = 10;

        // J: Flyswatting
        selectedQuestions[9] = Question.FlyswattingPresent;

        // K: Simon's Spider
        selectedQuestions[10] = Question.SimonSpidersPresent;

        // L: Summoned by Allmighty Sinnoh
        selectedQuestions[11] = Question.SummonedByAllmightySinnoh;

        // M: AA battery present
        selectedQuestions[12] = Question.SpecificBatteryPresent;
        questionPayloads[12] = 0;

        // N: Langton's Ant
        selectedQuestions[13] = Question.LangtonsAntPresent;


        for (int i = 0; i < 14; i++)
        {
            summoningModule.ModuleLog(moduleId, "Question {0} will be “{1}”.", alphabet[i], GetReadableQuestionString(i));
        }
    }




    void InitializeVoidedQuestions()
    {
        // Void letters that are in the Serial Number
        voidedLetters = bombInfo.GetSerialNumberLetters().ToArray();
    }



    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Twitch Plays
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    public override IEnumerator ProcessTwitchCommand(string command)
    {
        if (summoningModule.isModuleSolved) { yield break; }

        // Credit to Royal_Flu$h for this line 
        var commandParts = command.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);


        if (commandParts.Length == 0)
        {
            yield return "sendtochaterror {0} Received empty command!";
            yield break;
        }

        if (commandParts.Length != 2)
        {
            yield return "sendtochaterror {0} Received command formatted incorrectly!";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Received unknown command! Please use 'submit' or 'press' to submit an answer.";
            yield break;
        }

        switch (commandParts[1])
        {
            case "up": case "u":
                yield return null;
                platePressableButtons[0].OnInteract();
                yield break;

            case "down": case "d":
                yield return null;
                platePressableButtons[1].OnInteract();
                yield break;

            case "left": case "l":
                yield return null;
                platePressableButtons[2].OnInteract();
                yield break;

            case "right": case "r":
                yield return null;
                platePressableButtons[3].OnInteract();
                yield break;

            case "center": case "c":
                yield return null;
                platePressableButtons[4].OnInteract();
                yield break;
        }


    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        StartCoroutine(PlateShouldSolve());

        yield break;
    }
}

// Question.LastDigitOfSnParity
// Question.MinNonNeedyModuleCount
// Question.MinBatteryCount
// Question.SpecificBatteryPresent
// Question.MinIndicatorPresent
// Question.SpecificIndicatorPresent
// Question.MinPortPresent
// Question.SpecificPortPresent
// Question.LetterPresentInSn
// Question.DigitPresentInSn
// Question.DuplicatePortPresent
// Question.DuplicateSnCharacterPresent
// Question.NumberOfLettersInSn
// Question.SnContainsVowel
// Question.EmptyPortPlatePresent
// Question.HasStruckBefore
// Question.NoSolvedModules
// Question.SimonSpidersPresent
// Question.FlyswattingPresent
// Question.LangtonsAntPresent
// Question.ButterfliesPresent
// Question.SummonedByAllmightySinnoh
// Question.OtherMythicalPlatePresent
// Question.SnContainsInsect
// Question.NoBatteriesPresent
// Question.NoPortsPresent
// Question.NoIndicatorsPresent
// Question.SameLitsAsUnlits


/*
 
 
void InitializeQuestionAnswers()
    {
        // Initialize question validities, for those that can be precomputed
        // Which is A B C D E F H K L M N
        // G I J must be computed on the fly

        // This is used in multiple questions that check the presence of other modules
        List<string> _solvableModuleIds = bombInfo.GetSolvableModuleIDs();

        // This will be used for G, later
        bombStartingTime = bombInfo.GetTime();


        // A) Is the last digit f the Serial Number even?
        questionAAnswer = bombInfo.GetSerialNumberNumbers().Last() % 2 == 0;


        // B) Are there 10 or more non-needy modules?
        questionBAnswer = _solvableModuleIds.Count >= 10;


        // C) Are there 3 or more Batteries?
        questionCAnswer = bombInfo.GetBatteryCount() >= 3;


        // D) Is the module Simon's Spider present?
        questionDAnswer = _solvableModuleIds.Contains("SimonsSpiderModule");


        // E) Is there a BOB Indicator?
        questionEAnswer = bombInfo.GetIndicators().Contains("BOB");


        // F) Is there a PS/2 Port
        questionFAnswer = bombInfo.GetPorts().Contains("PS2");


        // H) Is the module Flyswatting present?
        questionHAnswer = _solvableModuleIds.Contains("flyswatting");


        // K) Is the letter E present in the Serial Number?
        questionKAnswer = bombInfo.GetSerialNumberLetters().Contains('E');


        // L) Is the module Langton's Ant present?
        questionLAnswer = _solvableModuleIds.Contains("langtonAnt");


        // M) Is this plate summoned by the module Allmighty Sinnoh?
        questionMAnswer = summoningModule.GetType() == typeof(AllmightySinnoh);


        // N) Is there an empty Port Plate?
        questionNAnswer = bombInfo.GetPortPlates().Any(x => x.Length == 0);

        summoningModule.ModuleLog(moduleId, "Some answers to questions do not change over time, so they can be pre-computed before Player Input.");
        summoningModule.ModuleLog(moduleId, "They are: Question A {0}, Question B {1}, Question C {2}, Question D {3}, Question E {4}, Question F {5}, Question H {6}, Question K {7}, Question L {8}, Question M {9}",
            questionAAnswer, questionBAnswer, questionCAnswer, questionDAnswer, questionEAnswer, questionFAnswer, questionHAnswer, questionKAnswer, questionLAnswer, questionMAnswer);
        summoningModule.ModuleLog(moduleId, "Questions G, I and J will be checked upon Player Input only.");
    }
 
 
 */