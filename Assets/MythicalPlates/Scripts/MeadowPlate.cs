using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeadowPlate : PlateBase {

    readonly string[] ReadableMonths = new string[12] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

    /// <summary> ALL BERRIES, with the months they can be planted in. 1 is January, not 0. </summary>
    readonly Dictionary<string, int[]> berries = new Dictionary<string, int[]>()
    {
        { "Aguav", new int[4]{ 2, 3, 8, 9 } }, { "Apicot", new int[6]{ 1, 2, 3, 10, 11, 12 } }, { "Aspear", new int[6]{ 1, 2, 3, 10, 11, 12} },
        { "Babiri", new int[4]{ 5, 6, 7, 8 } }, { "Belue", new int[4]{ 1, 2, 11, 12 } }, { "Bluk", new int[6]{ 1, 2, 3, 4, 11, 12 } },
        { "Charti", new int[3]{ 3, 4, 5 } }, { "Cheri", new int[5]{ 1, 2, 3, 11, 12 } }, { "Chesto", new int[5]{ 1, 2, 3, 11, 12 } },
        { "Chilan", new int[4]{ 3, 4, 5, 6 } }, { "Chople", new int[6]{ 5, 6, 7, 8, 9, 10} }, { "Coba", new int[6]{ 1, 2, 3, 10, 11, 12} },
        { "Colbur", new int[4]{ 8, 9, 10, 11} }, { "Cornn", new int[4]{ 2, 3, 4, 5 } }, { "Custap", new int[4]{ 4, 5, 9, 10 } },
        { "Drash", new int[5]{ 5, 6, 7, 8, 9 } }, { "Durin", new int[4]{ 6, 7, 10, 11 } }, { "Eggant", new int[2]{ 5, 6 } },
        { "Enigma", new int[6]{ 1, 3, 5, 7, 9, 11 } }, { "Figy", new int[3]{ 3, 4, 5 } }, { "Ganlon", new int[3]{ 4, 5, 6 } },
        { "Ginema", new int[4]{ 2, 3, 4, 5 } }, { "Gracidea", new int[4]{ 2, 3, 4, 5 } }, { "Grepa", new int[3]{ 3, 5, 6 } },
        { "Haban", new int[3]{ 6, 7, 8 } }, { "Hondew", new int[3]{ 4, 5, 6 } }, { "Hopo", new int[5]{ 1, 2, 3, 4, 12 } },
        { "Iapapa", new int[8]{ 1, 2, 3, 4, 5, 10, 11, 12 } }, { "Jaboca", new int[5]{ 2, 4, 7, 9, 11 } }, { "Kasib", new int[4]{ 4, 5, 9, 10 } },
        { "Kebia", new int[4]{ 4, 5, 10, 11 } }, { "Kee", new int[5]{ 2, 3, 4, 5, 6 } }, { "Kelpsy", new int[5]{ 3, 6, 7, 8, 11 } },
        { "Kuo", new int[3]{ 4, 5, 6 } }, { "Lansat", new int[4]{ 5, 6, 9, 10 } }, { "Leppa", new int[5]{ 1, 2, 3, 11, 12 } },
        { "Liechi", new int[5]{ 4, 5, 8, 9, 10 } }, { "Lum", new int[4]{ 1, 2, 3, 12 } }, { "Mago", new int[4]{ 3, 4, 5, 6 } },
        { "Magost", new int[3]{ 5, 6, 8 } }, { "Maranga", new int[4]{ 8, 9, 10, 11 } }, { "Micle", new int[4]{ 2, 5, 8, 11 } },
        { "Nanab", new int[6]{ 2, 3, 4, 5, 9, 10 } }, { "Niniku", new int[5]{ 1, 9, 10, 11, 12} }, { "Nomel", new int[4]{ 1, 2, 5, 6 } },
        { "Nutpea", new int[2]{ 6, 7 } }, { "Occa", new int[4]{ 2, 4, 9, 12 } }, { "Oran", new int[5]{ 1, 2, 3, 11, 12 } },
        { "Pamtre", new int[4]{ 6, 7, 8, 9 } }, { "Passho", new int[3]{ 3, 4, 5 } }, { "Payapa", new int[8]{ 2, 3, 5, 6, 8, 9, 11, 12 } },
        { "Pecha", new int[4]{ 1, 2, 3, 12 } }, { "Persim", new int[5]{ 1, 2, 3, 4, 5 } }, { "Petaya", new int[3]{ 6, 7, 8 } },
        { "Pinap", new int[3]{ 5, 6, 7 } }, { "Pomeg", new int[3]{ 1, 3, 12 } }, { "Pumkin", new int[3]{ 6, 7, 8 } },
        { "Qualot", new int[2]{ 3, 4 } }, { "Rabuta", new int[3]{ 4, 8, 12 } }, { "Rawst", new int[3]{ 8, 9, 10 } },
        { "Razz", new int[4]{ 1, 10, 11, 12 } }, { "Rindo", new int[3]{ 5, 6, 7 } }, { "Roseli", new int[6]{ 3, 4, 6, 8, 10, 11 } },
        { "Rowap", new int[6]{ 1, 2, 5, 6, 9, 10 } }, { "Salac", new int[2]{ 3, 4 } }, { "Shuca", new int[4]{ 3, 6, 9, 12 } },
        { "Sitrus", new int[7]{ 3, 4, 5, 6, 7, 10, 11 } }, { "Spelon", new int[3]{ 2, 3, 4 } }, { "Starf", new int[5]{ 2, 4, 6, 8, 10 } },
        { "Strib", new int[4]{ 5, 6, 7, 8 } }, { "Tamato", new int[3]{ 4, 5, 6 } }, { "Tanga", new int[5]{ 5, 6, 7, 9, 10 } },
        { "Topo", new int[4]{ 3, 4, 5, 6 } }, { "Touga", new int[7]{ 2, 3, 5, 6, 9, 10, 11 } }, { "Wacan", new int[3]{ 6, 7, 12 } },
        { "Watmel", new int[2]{ 4, 5 } }, { "Wepear", new int[6]{ 1, 2, 3, 10, 11, 12 } }, { "Wiki", new int[4]{ 1, 2, 11, 12 } },
        { "Yache", new int[4]{ 5, 6, 8, 9 } }, { "Yago", new int[7]{ 2, 3, 4, 5, 6, 7, 8 } }
    };

    int voidedMonth;
    int targetBerryMonth;

    string[] selectedBerries = new string[4];

    [SerializeField] TextMesh PlateBerriesText;

    // Universal Logging Data
    static int moduleIdCounter = 1;



    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        // ButtonIndex is 0-11 but MonthNumbers are 1-12!!
        platePressableButtons[00].OnInteract += delegate () { PressedMonthButton(01); return false; };
        platePressableButtons[01].OnInteract += delegate () { PressedMonthButton(02); return false; };
        platePressableButtons[02].OnInteract += delegate () { PressedMonthButton(03); return false; };
        platePressableButtons[03].OnInteract += delegate () { PressedMonthButton(04); return false; };
        platePressableButtons[04].OnInteract += delegate () { PressedMonthButton(05); return false; };
        platePressableButtons[05].OnInteract += delegate () { PressedMonthButton(06); return false; };
        platePressableButtons[06].OnInteract += delegate () { PressedMonthButton(07); return false; };
        platePressableButtons[07].OnInteract += delegate () { PressedMonthButton(08); return false; };
        platePressableButtons[08].OnInteract += delegate () { PressedMonthButton(09); return false; };
        platePressableButtons[09].OnInteract += delegate () { PressedMonthButton(10); return false; };
        platePressableButtons[10].OnInteract += delegate () { PressedMonthButton(11); return false; };
        platePressableButtons[11].OnInteract += delegate () { PressedMonthButton(12); return false; };
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

    void PressedMonthButton(int buttonMonthNumber)
    {
        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        summoningModule.ModuleLog(moduleId, "Pressed button for the month of {0}!", GetReadableMonthName(buttonMonthNumber));


        // Verify just in case that the solution was the expected one, for fast-tracking!
        if (buttonMonthNumber == targetBerryMonth)
        {
            ValidMonthPressed();
            return;
        }



        // Verify the Month; because some Void+Berry combinations have multiple correct answers!
        // Do the same "Check Months in Between" as when generating puzzle for checking Voided Months
        int _numOfMonthsToCheck = buttonMonthNumber - voidedMonth - 1;

        if (_numOfMonthsToCheck < 0) { _numOfMonthsToCheck += 12; }

        // Precompute the months to check
        List<int> _monthsBetweenVoidAndTarget = new List<int>();
        if (_numOfMonthsToCheck != 0)
        {
            int _searchedMonth;
            for (int i = 0; i < _numOfMonthsToCheck; i++)
            {
                _searchedMonth = buttonMonthNumber - i;
                // <= 0 instead of <0 since months are noted 1-12 and not 0-11
                if (_searchedMonth <= 0) { _searchedMonth += 12; }

                _monthsBetweenVoidAndTarget.Add(_searchedMonth);
            }
        }


        string berryToVerify;
        int[] monthsToVerify;
        for (int i = 0; i < 4; i ++)
        {
            // Get information about the berry
            berryToVerify = selectedBerries[i];
            berries.TryGetValue(berryToVerify, out monthsToVerify);

            // Berry can be planted directly, so it's good
            if (monthsToVerify.Contains(buttonMonthNumber))
            { continue; }

            // Berry can't be planted directly? Well can it be planted in the Voided month at least?
            if (monthsToVerify.Contains(voidedMonth) == false)
            {
                summoningModule.ModuleLog(moduleId, "Submitted month of {0} but {1} can't be planted in that Month nor the Voided {2}!",
                    GetReadableMonthName(buttonMonthNumber), berryToVerify, GetReadableMonthName(voidedMonth));
                summoningModule.ReceiveStrike();
                return;
            }

            // It *can* get planted in the Voided Month?
            // Then check every month in-between!
            foreach (int _monthToCheck in _monthsBetweenVoidAndTarget)
            {
                if (monthsToVerify.Contains(_monthToCheck) == false)
                {
                    summoningModule.ModuleLog(moduleId, "{0} can be planted during Voided {1}, but there is not a continuous plantable chain until target {2}. That is incorrect!",
                        berryToVerify, GetReadableMonthName(voidedMonth), GetReadableMonthName(buttonMonthNumber));
                    summoningModule.ReceiveStrike();
                    return;
                }
            }

            // If code arrives here, that one berry was correct!
        }

        // If code arrives here, all 4 berries were correct!
        ValidMonthPressed();
    }

    void ValidMonthPressed()
    {
        summoningModule.ModuleLog(moduleId, "That month is correct!");
        summoningModule.ReceiveSolve();
    }



    protected override void CasingTextButtonGetsPressed() { }

    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void InitializePuzzle()
    {
        DetermineVoidedMonth();

        DetermineTargetMonth();

        FindFourCompatibleBerries();

    }


    void DetermineVoidedMonth()
    {
        int _SumOfSerialNumberDigits = bombInfo.GetSerialNumberNumbers().Sum();
        voidedMonth = _SumOfSerialNumberDigits % 12;

        if (voidedMonth == 0) { voidedMonth = 12; }

        summoningModule.ModuleLog(moduleId, "Serial Number Digits are {0}, their sum in the range 1-12 is {1}, so the Voided Month will be {2}.",
            bombInfo.GetSerialNumberNumbers().Join(), voidedMonth, GetReadableMonthName(voidedMonth));
    }

    void DetermineTargetMonth()
    {
        // For the puzzle generation, get a "target" month, then get 4 berries that can be planted in that month for sure

        targetBerryMonth = voidedMonth;

        // Generate so that it's NOT a voided month
        while (targetBerryMonth == voidedMonth)
        { targetBerryMonth = UnityEngine.Random.Range(1, 13); }

        summoningModule.ModuleLog(moduleId, "Will generate 4 berries that can be planted in {0}.", GetReadableMonthName(targetBerryMonth));
    }

    void FindFourCompatibleBerries()
    {
        string _attemptedBerryName = string.Empty;
        int[] _attemptedBerryMonths;

        summoningModule.ModuleLog(moduleId, "Generating berries that can be planted in that month...");


        // In case we need to check for the space between the Voided and Target month; pre-compute which months have to be valid for it to apply
        int _numOfMonthsToCheck = targetBerryMonth - voidedMonth - 1;

        // If the number is 0; then the Voided is just before the Target, so no months are needed to be checked in-between.
        // Just check that the Voided Month is valid and the Target will become valid instantly

        // If the number is negative, that means the Voided month happens AFTER the Target
        // But since months loop (December_12 comes just before January_1; so the result would be -11-1 => -12)
        // Take that loop into account (December-to-January is -12 but we want 0 since they are next to each other)
        if (_numOfMonthsToCheck < 0) { _numOfMonthsToCheck += 12;}

        // Precompute the months to check
        List<int> _monthsBetweenVoidAndTarget = new List<int>();
        if (_numOfMonthsToCheck != 0)
        {
            int _searchedMonth; 


            for (int i = 0; i < _numOfMonthsToCheck; i ++)
            {
                _searchedMonth = targetBerryMonth - i;
                // <= 0 instead of <0 since months are noted 1-12 and not 0-11
                if (_searchedMonth <= 0) { _searchedMonth += 12; }

                _monthsBetweenVoidAndTarget.Add(_searchedMonth);
            }
        }


        for (int i = 0; i < 4; i ++)
        {
            // Goto usage to go back to the start of the loop without incrementing
            // Could have used a while(numberOfBerriesFound < 4)
            // and incremented only when a valid one was found, to then do a continue instead...
            // That would have been better, huh?
            tryNewBerry:

            // Randomly pick Berry
            _attemptedBerryName = berries.PickRandom().Key;
            summoningModule.ModuleLog(moduleId, "Attempting with {0}.", _attemptedBerryName);

            // Make sure it's not already picked
            if (selectedBerries.Contains(_attemptedBerryName))
            {
                summoningModule.ModuleLog(moduleId, "{0} is already picked; skipping.", _attemptedBerryName);
                goto tryNewBerry;
            }


            // Can it be planted this month regularly?
            berries.TryGetValue(_attemptedBerryName, out _attemptedBerryMonths);

            if (_attemptedBerryMonths.Contains(targetBerryMonth))
            {
                // Berry is valid? Well do that!
                selectedBerries[i] = _attemptedBerryName;
                summoningModule.ModuleLog(moduleId, "{0} can be planted in {1}. Picking it for berry {2}",
                    _attemptedBerryName, GetReadableMonthName(targetBerryMonth), i);
                continue;
            }


            // Else, can it be planted this month thanks to void?
                
            // That is only the case if:
            // Voided Month is Valid
            // AND
            // months in-between Void and Target all are Valid

            // If Voided Month is not plantable; then get rid of it
            if (_attemptedBerryMonths.Contains(voidedMonth) == false)
            {
                summoningModule.ModuleLog(moduleId, "{0} cannot be planted in {1} nor in the Voided {2}; so it is invalid",
                    _attemptedBerryName, GetReadableMonthName(targetBerryMonth), GetReadableMonthName(voidedMonth));
                goto tryNewBerry;
            }

                

            summoningModule.ModuleLog(moduleId, "Will check the {0} months in between Voided {1} and Target {2}",
                _numOfMonthsToCheck, GetReadableMonthName(voidedMonth), GetReadableMonthName(targetBerryMonth));


            // Check the X months in-between the Voided and Target months
            foreach (int _monthToCheck in _monthsBetweenVoidAndTarget)
            {
                if (_attemptedBerryMonths.Contains(_monthToCheck) == false)
                {
                    summoningModule.ModuleLog(moduleId, "{0} can be planted during Voided {1}, but there is not a continuous plantable chain until target {2}. Discarding it.",
                        _attemptedBerryName, GetReadableMonthName(voidedMonth), GetReadableMonthName(targetBerryMonth));
                    goto tryNewBerry;
                }
            }

            // We land here only if all months were valid!
            selectedBerries[i] = _attemptedBerryName;
            summoningModule.ModuleLog(moduleId, "{0} can be planted in {1} thanks to the Void. Picking it for berry {2}",
                _attemptedBerryName, GetReadableMonthName(targetBerryMonth), i);
            continue;
        }

        // All 4 berries have been found
        summoningModule.ModuleLog(moduleId, "Found our four Berries to be planted: {0}", selectedBerries.Join());
        PlateBerriesText.text = selectedBerries.Join("\n").ToUpper();
    }

    string GetReadableMonthName(int monthNUMBER)
    {
        return ReadableMonths[monthNUMBER - 1];
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
            case "1": case "01": case "january": case "jan":
                PressedMonthButton(01); break;

            case "2": case "02": case "february": case "feb":
                PressedMonthButton(02); break;

            case "3": case "03": case "march": case "mar":
                PressedMonthButton(03); break;

            case "4": case "04": case "april": case "apr":
                PressedMonthButton(04); break;

            case "5": case "05": case "may":
                PressedMonthButton(05); break;

            case "6": case "06": case "june": case "jun":
                PressedMonthButton(06); break;

            case "7": case "07": case "july": case "jul":
                PressedMonthButton(07); break;

            case "8": case "08": case "august": case "aug":
                PressedMonthButton(08); break;

            case "9": case "09": case "september": case "sep":
                PressedMonthButton(09); break;

            case "10": case "october": case "oct":
                PressedMonthButton(10); break;

            case "11": case "november": case "nov":
                PressedMonthButton(11); break;

            case "12": case "december": case "dec":
                PressedMonthButton(12); break;

            default:
                yield return "sendtochaterror {0} Received unknown month descriptor: " + commandParts[1] + ". Please use the month number (1-12), full name (september), or three-letter abbreviation (sep).";
                break;
        }
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        PressedMonthButton(targetBerryMonth);

        yield break;
    }

}
