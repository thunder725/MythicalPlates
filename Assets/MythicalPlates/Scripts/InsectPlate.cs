using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InsectPlate : PlateBase {

    char[] voidedLetters;

    // Boolean for every question answer, since most of them can be pre-computed from edgeword and bomb data
    bool questionAAnswer, questionBAnswer, questionCAnswer, questionDAnswer, questionEAnswer, questionFAnswer, questionGAnswer,
        questionHAnswer, questionIAnswer, questionJAnswer, questionKAnswer, questionLAnswer, questionMAnswer, questionNAnswer;

    float bombStartingTime = Mathf.Infinity;

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

        // Check question G I and J on the fly since they cannot be pre-computed
        // G) Is over 50% of the time remaining?
        questionGAnswer = bombInfo.GetTime() > (bombStartingTime / 2);

        // I) Are there 0 solved modules?
        questionIAnswer = bombInfo.GetSolvedModuleIDs().Count == 0;

        // J) Has the bomb struct previously?
        questionJAnswer = bombInfo.GetStrikes() > 0;

        pressedButtonDirection = buttonIndex;
        summoningModule.ModuleLog(moduleId, "Pressed button with direction {0}.", GetReadableButtonDirection(pressedButtonDirection));

        summoningModule.ModuleLog(moduleId, "Checking answers that can't be precomputed! Question G {0}, Question I {0}, Question J {0}",
            questionGAnswer, questionIAnswer, questionJAnswer);

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
            summoningModule.ReceiveSolve();
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

        summoningModule.ModuleLog(moduleId, "Checking Question {0}!", questionLetter);

        switch (questionLetter)
        {
            // Question A cannot be Voided
            case 'A':
                // A true => B
                // A false => C
                if (questionAAnswer == true) { CheckQuestion('B'); }
                else { CheckQuestion('C'); }
                return;

            case 'B':
                // B true OR B void => D
                // B false => E
                if (questionBAnswer == true || voidedLetters.Contains('B')) { CheckQuestion('D'); }
                else { CheckQuestion('E'); }
                return;

            case 'C':
                // C false OR C void => F
                // C true => Should have Pressed Center!
                if (questionCAnswer == false || voidedLetters.Contains('C')) { CheckQuestion('F'); }
                else { PressedButtonResult(4); }
                return;

            case 'D':
                // D true OR D void => Should have pressed Up!
                // D false => G
                if (questionDAnswer == true || voidedLetters.Contains('D')) { PressedButtonResult(0); }
                else { CheckQuestion('G'); }
                return;

            case 'E':
                // E false OR E void => G
                // E true => H
                if (questionEAnswer == false || voidedLetters.Contains('E')) { CheckQuestion('G'); }
                else { CheckQuestion('H'); }
                return;

            case 'F':
                // F false OR F void => I
                // F true => H
                if (questionFAnswer == false || voidedLetters.Contains('F')) { CheckQuestion('I'); }
                else { CheckQuestion('H'); }
                return;

            case 'G':
                // G false OR G void => J
                // G true => K
                if (questionGAnswer == false || voidedLetters.Contains('G')) { CheckQuestion('J'); }
                else { CheckQuestion('K'); }
                return;

            case 'H':
                // H true OR H void => K
                // H false => Should have pressed Right!
                if (questionHAnswer == true || voidedLetters.Contains('G')) { CheckQuestion('K'); }
                else { PressedButtonResult(3); }
                return;

            case 'I':
                // I false OR I void => Should have pressed Down!
                // I true => L
                if (questionIAnswer == false || voidedLetters.Contains('I')) { PressedButtonResult(1); }
                else { CheckQuestion('L'); }
                return;

            case 'J':
                // J false Or J void => Should have pressed Left!
                // J true => Should have pressed Right!
                if (questionJAnswer == false || voidedLetters.Contains('J')) { PressedButtonResult(2); }
                else { PressedButtonResult(3); }
                return;

            case 'K':
                // K true OR K void => Should have pressed Left!
                // K false => Should have pressed Up!
                if (questionKAnswer == true || voidedLetters.Contains('K')) { PressedButtonResult(2); }
                else { PressedButtonResult(0); }
                return;

            case 'L':
                // L true or L void => M
                // L false => N
                if (questionLAnswer == true || voidedLetters.Contains('L')) { CheckQuestion('M'); }
                else { CheckQuestion('N'); }
                return;

            case 'M':
                // M true or M void => Should have pressed Center!
                // M false => Should have pressed Down!
                if (questionMAnswer == true || voidedLetters.Contains('M')) { PressedButtonResult(4); }
                else { PressedButtonResult(1); }
                return;

            case 'N':
                // N false or N void => Should have pressed Down!
                // N true => Should have pressed Left!
                if (questionNAnswer == false|| voidedLetters.Contains('N')) { PressedButtonResult(1); }
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
        InitializeVoidedQuestions();
        InitializeQuestionAnswers();
    }

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
            case "Up": case "U":
                PlayerPressedButton(0);
                yield break;

            case "Down": case "D":
                PlayerPressedButton(1);
                yield break;

            case "Left": case "L":
                PlayerPressedButton(2);
                yield break;

            case "Right": case "R":
                PlayerPressedButton(3);
                yield break;

            case "Center": case "C":
                PlayerPressedButton(4);
                yield break;
        }


    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        summoningModule.ReceiveSolve();

        yield break;
    }
}
