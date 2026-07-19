using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public class BlankPlate : PlateBase {

    /// <summary> Dictionary containing the different Paths associated with each letter. </summary>
    readonly Dictionary<char, string> PathConversionTable = new Dictionary<char, string>() {
        {'A', "__x_x_*__"},
        {'B', "___"},
        {'C', "__*_*__"},
        {'D', "__*__"},
        {'E', "_*__*_*__"},
        {'F', "_*__"},
        {'G', "__x_"},
        {'H', "_*___xx_"},
        {'I', "_xxx__*_"},
        {'J', "xx___*_"},
        {'K', "*___x__"},
        {'L', "_*_"},
        {'M', "_xxx_*__"},
        {'N', "_xx_*__"},
        {'O', "x_x_**_"},
        {'P', "*__*__"},
        {'Q', "_*__xx__"},
        {'R', "xx___x__"},
        {'S', "_**__*_"},
        {'T', "_*_xx__"},
        {'U', "xxx_"},
        {'V', "_xx__*_"},
        {'W', "**_x_x_"},
        {'X', "__x__*_"},
        {'Y', "___x__x__"},
        {'Z', "__*__xx_"},
        {'0', "__"},
        {'1', "**__*_"},
        {'2', "_x_x_x___"},
        {'3', "_**_xx__"},
        {'4', "_xx__x_x_"},
        {'5', "_**__"},
        {'6', "**_x____*_"},
        {'7', "_xx_"},
        {'8', "x_x_x_x_"},
        {'9', "x___**__"},
    };
    enum PabloMovementType { Step, Jump, Slide };

    /// <summary> Constructed Path that Pablo will have to traverse </summary>
    string pabloPath;
    [SerializeField] TextMesh plateVoidText;

    /// <summary> Boolean used for puzzle generation </summary>
    bool foundValidVoid;
    /// <summary> Example sequence of movements that correctly solves the module </summary>
    string correctMovementPattern;

    /// <summary> Current location of where Pablo will move on the next step.
    /// A current location +1.
    /// What this means is that Pablo starts outside of the path, in an "index -1",
    /// and this is represented by currentPabloIndex being 0.
    /// This index could be thought as "The next tile that Pablo will Traverse".</summary>
    int currentPabloIndex;


    // Universal Logging Data
    static int moduleIdCounter = 1;


    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { OnPressedMovementButton(PabloMovementType.Jump); return false; };
        platePressableButtons[1].OnInteract += delegate () { OnPressedMovementButton(PabloMovementType.Step); return false; };
        platePressableButtons[2].OnInteract += delegate () { OnPressedMovementButton(PabloMovementType.Slide); return false; };
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        InitializePuzzle();
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void OnPressedMovementButton(PabloMovementType pressedButtonType)
    {
        // Feedbacks
        PlayPlatePressSound();
        platePressableButtons[0].AddInteractionPunch(.5f);

        if (summoningModule.isModuleSolved) { return; }

        switch (pressedButtonType)
        {
            case PabloMovementType.Step:
                // Step is only correct if the tile to move into is empty
                if (pabloPath[currentPabloIndex] == '_')
                {
                    MovePabloForwardWhileWatchingForVoid();
                    break;
                }

                // But otherwise... That's bad!
                GiveStrike();
                break;

            case PabloMovementType.Jump:
                
                // Move 3 tiles forward
                for (int i = 0; i < 3; i ++)
                {
                    // Only can get hit by SlideObstacles (you jump over the Jump Obstacles)
                    if (pabloPath[currentPabloIndex] == '*')
                    {
                        GiveStrike();
                        break;
                    }

                    // If no obstacle we can move of course.
                    MovePabloForwardWhileWatchingForVoid();
                }

                // Then on the 4th, verify that it's a valid empty tile
                if (pabloPath[currentPabloIndex] == '_')
                {
                    MovePabloForwardWhileWatchingForVoid();
                    break;
                }

                // But otherwise... That's bad!
                GiveStrike();
                break;

            case PabloMovementType.Slide:
                // For 2 tiles, only get hit by JumpObstacles (you pass under the Slide Obstacles)
                for (int i = 0; i < 2; i++)
                {
                    if (pabloPath[currentPabloIndex] == 'x')
                    {
                        GiveStrike();
                        break;
                    }

                    // If no obstacle we can move of course.
                    MovePabloForwardWhileWatchingForVoid();
                }

                // Then on the 3rd, verify that it's a valid empty tile
                if (pabloPath[currentPabloIndex] == '_')
                {
                    MovePabloForwardWhileWatchingForVoid();
                    break;
                }

                // But otherwise... That's bad!
                GiveStrike();
                break;
        }

        
        if (currentPabloIndex >= pabloPath.Length)
        {
            summoningModule.ModuleLog(moduleId, "Pressing {0} moved Pablo to the exit of the Path. Well done!", pressedButtonType.ToString());
            summoningModule.ReceiveSolve();
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Pressing {0} moved Pablo to tile index {1}.", pressedButtonType.ToString(), currentPabloIndex);
        }
    }

    /// <summary> Give a Strike for bad movement, and then move Pablo forward until the next Obstacle-less Voidless tile </summary>
    void GiveStrike()
    {
        // Get a Strike!
        summoningModule.ReceiveStrike();

        // Move forward until a valid spot is okay
        bool foundCorrectTile = false;
        while (foundCorrectTile == false)
        {
            MovePabloForwardWhileWatchingForVoid();

            // If we moved forward until leaving the path
            // Well congrats you solved during a strike!
            if (currentPabloIndex >= pabloPath.Length)
            {
                summoningModule.ModuleLog(moduleId, "Would you look at that! Moving you to the next obstacle-less tile brought you directly to the end!! Module still solves; luck you!");
                summoningModule.ReceiveSolve();
                return;
            }

            // Leave Pablo on the next Obstacle-less Void-less tile that was found
            if (pabloPath[currentPabloIndex] == '_')
            {
                foundCorrectTile = true;
            }
        }

        summoningModule.ModuleLog(moduleId, "Moving you to the next obstacle-less tile; which is tile with index {0} (first one is 0)", currentPabloIndex);
    }

    // Can't just move Pablo Forward willy-nilly; we have to keep moving forward while ignoring Voided tiles of course!
    // Separate function since this can extend jump and slides and all that jazz
    void MovePabloForwardWhileWatchingForVoid()
    {
        currentPabloIndex++;

        while (voidedCellsIndices.Contains(currentPabloIndex))
        {
            currentPabloIndex++;
        }
    }

    // Reset button
    protected override void CasingTextButtonGetsPressed()
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        currentPabloIndex = 0;
        summoningModule.ModuleLog(moduleId, "'BLANK' button pressed! Reset Pablo to its starting location!");
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void InitializePuzzle()
    {
        GeneratePath();
        StartCoroutine(StartVoidTileSearching());
    }



    void GeneratePath()
    {
        summoningModule.ModuleLog(moduleId, "Generating Path.");

        string _charactersForPathCreation = "";

        // Take all Indicators, sum the First Letters together, then the Second Letters together, then the Third
        string[] _allIndicators = bombInfo.GetIndicators().ToArray();
        if (_allIndicators.Length == 0 )
        {
            summoningModule.ModuleLog(moduleId, "Found no indicators.");
        }
        // Only found 1 indicator
        else if (_allIndicators.Length == 1)
        {
            _charactersForPathCreation += _allIndicators[0];

            summoningModule.ModuleLog(moduleId, "Found singular indicator {0}. Adding it to the Character chain as-is. Character chain now is {1}", _allIndicators[0], _charactersForPathCreation);
        }
        // Found more than 1 indicator
        else
        {
            summoningModule.ModuleLog(moduleId, "Found indicators {0}.", _allIndicators.Join(", "));

            int _sumOfCharacters;

            int _characterValue;
            for (int i = 0; i < 3; i ++)
            {
                _sumOfCharacters = 0;

                string _lettersFromIndicators = "Characters " + (i+1) + ": ";

                foreach (string _indicator in _allIndicators)
                {
                    _characterValue = Array.IndexOf(alphabet, _indicator[i].ToString()) + 1;

                    // +1 is there because A is index 0; when its alphabetical position is 1
                    _sumOfCharacters += _characterValue;

                    // Log 
                    _lettersFromIndicators += String.Format("letter {0} from {1} has value {2}; ", _indicator[i].ToString(), _indicator, _characterValue);
                }

                // remove the final "; "
                _lettersFromIndicators = _lettersFromIndicators.Remove(_lettersFromIndicators.Length - 2);

                // Log the letters from those indicators.
                summoningModule.ModuleLog(moduleId, _lettersFromIndicators);


                // Mod26 to be sure, but keep 26...
                // So it's a "%26 but == 0 => 26"
                // So just subtract 26 if above 26
                while (_sumOfCharacters > 26)
                {
                    _sumOfCharacters -= 26;
                }

                // ...Since we explicitely want a -1 at this step
                // That is because a single A will be interpreted as 1, which then should become index 0
                // A single Z would be 26, which should stay 26 to become 25
                // So then sums should now work correctly
                _charactersForPathCreation += alphabet[_sumOfCharacters - 1];

                // Log, taking care of the +1 -1 shuffle
                summoningModule.ModuleLog(moduleId, "Sum of alphabetical position of charcters {0} (post 26 subtraction) is {1}. Converted back to a letter it becomes {2}. Character chain now is {3}",
                    i + 1, _sumOfCharacters, alphabet[_sumOfCharacters - 1], _charactersForPathCreation);
            }
        }


        // // Number of batteries
        // // DEPRECATED!!
        // int _numberOfBatteries = bombInfo.GetBatteryCount();
        // _charactersForPathCreation += _numberOfBatteries.ToString();
        // 
        // summoningModule.ModuleLog(moduleId, "Found {0} batteries. Character chain now is {1}", _numberOfBatteries, _charactersForPathCreation);


        // All of the Serial Number
        string _serialNumber = bombInfo.GetSerialNumber();
        _charactersForPathCreation += _serialNumber;

        summoningModule.ModuleLog(moduleId, "Serial Number is {0}. Character chain now is {1}", _serialNumber, _charactersForPathCreation);


        // // Number of modules
        // // DEPRECATED!!
        // int _numberOfModules = bombInfo.GetModuleNames().Count;
        // _charactersForPathCreation += _numberOfModules.ToString();
        // 
        // summoningModule.ModuleLog(moduleId, "Found {0} modules. Character chain now is {1}", _numberOfModules, _charactersForPathCreation);



        // Make everything uppercase just to be sure.
        _charactersForPathCreation = _charactersForPathCreation.ToUpper();


        // Override to test puzzle generation on a wide array of paths: Every module gets its own unique path!
        // string _charactersForPathCreation = "ABCDEFGHIJKLMNOPQRSTUVXYZ0123456789ABCDEFGHIJKLMNOPQRSTUVXYZ0123456789".ToCharArray().Shuffle().Take(8).Join("");


        pabloPath = string.Empty;
        string _pathToAdd;

        // Increase Path
        foreach (char _pathLetter in _charactersForPathCreation)
        {
            if (PathConversionTable.TryGetValue(_pathLetter, out _pathToAdd) == false)
            {
                summoningModule.ModuleLogError(moduleId, "Did not find path using character '{0}'!!! Please report this to thunder725.", _pathLetter);
            }

            if (_pathToAdd.Length == 0)
            {
                summoningModule.ModuleLogError(moduleId, "Received path with length 0 using character '{0}'!!! Please report this to thunder725.", _pathLetter);
            }

            pabloPath += _pathToAdd;
        }

        summoningModule.ModuleLog(moduleId, "After transforming each character into path; the path now is '{0}'", pabloPath);
    }


    IEnumerator StartVoidTileSearching()
    {
        // We need to have Void on the path; but we need Void to still make the path workable

        // We cannot check individual Void tiles, check if they work, and combine them; because the combination could become impossible.
        // Likewise, even if we check individual Void tiles, then combine and re-check, we would miss combinations that only work by being together and not alone
        // That is because Void offsets the entire path so changes are cumulative and impact each other

        // So the mission is to take some frames, void 3 random tiles; and walk forward recursively trying every movement possible (^>v)
        // If a single path is found to be correct, then bingo we found the solution and we can use that.
        // Otherwise, do another X tries with 3 other random tiles.

        // After several frames of generation, if nothing valid was found, it will default to the second-to-last tile; because that is always valid to cut
        // In the meantime, text will show "generating" to make sure it is known to not interact with the module


        foundValidVoid = false;

        // Current settings are to do 5 path generation per frame, and to throw the towel after 60 frames of failed generation, or 300 attempts.
        // I tested this by generating 10,000 random paths, and the highest number of frames it took was 12;
        // so this is a very generous failsafe threshold that shouldn't happen in practice.
        // But hey, those thresholds are still important to have!
        int framesOfGenerationTaken = 0;
        int maximumFramesToGenerate = 60;
        int pathGenerationAttemptsPerFrame = 5;

        plateVoidText.text = "GENE-\nRATING\nPUZZLE";


        while (foundValidVoid == false)
        {
            // If we have found nothing after exhausting all frames
            // Use the default only second-to-last tile; it is always valid even if sad
            if (framesOfGenerationTaken == maximumFramesToGenerate)
            {
                voidedCellsIndices = new List<int>() { pabloPath.Length - 2 };
                break;
            }

            // Try to generate multiple Void paths per frame

            for (int i = 0; i < pathGenerationAttemptsPerFrame; i ++)
            {
                TryOneVoidPath();
                if (foundValidVoid) { break; }
            }

            framesOfGenerationTaken++;
            yield return null;
        }

        if (foundValidVoid)
        {
            summoningModule.ModuleLog(moduleId, "Successfully found a set of three Void tiles that still have a valid solution in {0} frames!", framesOfGenerationTaken);
        }
        else
        {
            summoningModule.ModuleLog(moduleId, "Despite having generated {0} set of Void tiles, no path had valid solutions...",
                maximumFramesToGenerate * pathGenerationAttemptsPerFrame);
            summoningModule.ModuleLog(moduleId, "In over 10k puzzle generation this never happened while developping. Sorry about that! Using failsafe solution.");
        }

        FinalizePath();
        yield break;
    }

    void TryOneVoidPath()
    {
        voidedCellsIndices.Clear();

        int _singleTryoutVoidTile;

        // Populate by picking 3 "random" indices
        while (voidedCellsIndices.Count < 3)
        {
            // Do not take any of the last two of the path, nor the very first one, because they aren't really interesting to check
            _singleTryoutVoidTile = UnityEngine.Random.Range(1, pabloPath.Length - 2);

            // Do not take the same one multiple times
            if (voidedCellsIndices.Contains(_singleTryoutVoidTile))
            { continue; }

            // But use a few rules to not pick obviously impossible ones
            // Technically some rules do not produce impossible ones depending on the other Voided tiles
            // For example Rule 1 can void the 3rd tile in a R module if it also decides to void the 2nd tile; it would become viable then.
            // However I prefer to filter them to avoid wasting iterations since we don't have many of them

            // Rule 1
            // A tile cannot be Voided if its two neighbors are a * and a x
            if ((pabloPath[_singleTryoutVoidTile - 1] == 'x' && pabloPath[_singleTryoutVoidTile + 1] == '*') ||
                (pabloPath[_singleTryoutVoidTile + 1] == 'x' && pabloPath[_singleTryoutVoidTile - 1] == '*'))
            { continue; }


            // Passed all rules? Add it to the attempted Void Indices for this generation!
            voidedCellsIndices.Add(_singleTryoutVoidTile);
        }

        // summoningModule.ModuleLog(moduleId, "Trying with voided tiles {0}", voidedCellsIndices.Join());



        // Start checking with a Step!!
        correctMovementPattern = GenerateNextMove(0, PabloMovementType.Step, 0);
        if (correctMovementPattern.Length > 0)
        { 
            foundValidVoid = true;
            return;
        }

        // Starting with a step didn't work; so let's start with a Jump instead!
        correctMovementPattern = GenerateNextMove(0, PabloMovementType.Jump, 0);
        if (correctMovementPattern.Length > 0)
        {
            foundValidVoid = true;
            return;
        }

        // Well then, let's start with a Slide!
        correctMovementPattern = GenerateNextMove(0, PabloMovementType.Slide, 0);
        if (correctMovementPattern.Length > 0)
        {
            foundValidVoid = true;
            return;
        }


        // Otherwise, no subtree worked, regardless how we started it!
    }

    /// <summary>
    /// Recursive Function that searches the entire Pablo Path, one tile at a time, one move at a time.
    /// It passes :
    /// - the current tile to look at (which gets incremented each time).
    /// - the movement type (step, jump, slide) to know which obstacles can be avoided
    /// - the current movement duration (useless for step), to know when a step or jump should finish,
    ///   or otherwise when it is locked in that movement
    /// The string return is empty if the recursion tree ended in a wrong leaf
    /// Otherwise it will have the movement history up until now
    /// </summary>
    string GenerateNextMove(int tileToLookAt, PabloMovementType movementType, int movementDuration)
    {
        // Did we reach beyond the final tile? Well that's a success!
        if (tileToLookAt == pabloPath.Length)
        {
            // It's past the Path, so no matter what Movement we did, it's correct!
            // Cannot return just an empty string since that's invalidity; so instead return a space; close enough!
            // summoningModule.ModuleLog(moduleId, "Got to the Leaf in tileIndex {0} with movement", tileToLookAt, movementType.ToString());
            return " ";
        }


        // Void? Just Skip to the next one without any modification
        if (voidedCellsIndices.Contains(tileToLookAt))
        {
            // summoningModule.ModuleLog(moduleId, "Found void at {0}, keeping going", tileToLookAt);
            return GenerateNextMove(tileToLookAt + 1, movementType, movementDuration);
        }


        switch (movementType)
        {
            case PabloMovementType.Step:
                // Steps are blocked by SlideObstacles and JumpObstacles
                if (pabloPath[tileToLookAt] == 'x' || pabloPath[tileToLookAt] == '*')
                {
                    // summoningModule.ModuleLog(moduleId, "Step hit an obstacle in tile {0}", tileToLookAt);
                    return string.Empty;
                }
                
                // Nothing else, a Step can directly go to anything else
                break;


            case PabloMovementType.Jump:
                // Jumps are blocked by JumpObstacles only
                if (pabloPath[tileToLookAt] == '*')
                {
                    // summoningModule.ModuleLog(moduleId, "Jump hit a JumpObstacle in tile {0}", tileToLookAt);
                    return string.Empty;
                }

                // A jump lasts 3 tiles, so if movementDuration is 0, 1 or 2 we must keep going
                // Since it's the continuation of the previous movement, nothing is added to the History
                if (movementDuration < 3)
                {
                    // summoningModule.ModuleLog(moduleId, "Jump is locked in its movement in tile {0}, current duration is {1}", tileToLookAt, movementDuration);
                    return GenerateNextMove(tileToLookAt + 1, PabloMovementType.Jump, movementDuration + 1);
                }

                // Otherwise, if movementDuration is 3, we landed at this movement.
                // This means we're vulnerable to SlideObstacles
                if (pabloPath[tileToLookAt] == 'x')
                {
                    // summoningModule.ModuleLog(moduleId, "Jump ended and landed on a SlideObstacle in tile {0}", tileToLookAt);
                    return string.Empty;
                }

                // summoningModule.ModuleLog(moduleId, "Jump successfully landed in tile {0}", tileToLookAt);
                // But if this is a clear tile; we can then do any action!
                break;



            case PabloMovementType.Slide:
                // Slides are blocked by SlideObstacles only
                if (pabloPath[tileToLookAt] == 'x')
                {
                    // summoningModule.ModuleLog(moduleId, "Slide hit a SlideObstacle in tile {0}", tileToLookAt);
                    return string.Empty;
                }

                // A slide lasts 2 tiles, so if movementDuration is 0, or 1 we must keep going
                // Since it's the continuation of the previous movement, nothing is added to the History
                if (movementDuration < 2)
                {
                    // summoningModule.ModuleLog(moduleId, "Slide is locked in its movement in tile {0}, current duration is {1}", tileToLookAt, movementDuration);
                    return GenerateNextMove(tileToLookAt + 1, PabloMovementType.Slide, movementDuration + 1);
                }

                // Otherwise, if movementDuration is 2, we got up at this movement.
                // This means we're vulnerable to JumpObstacles
                if (pabloPath[tileToLookAt] == '*')
                {
                    // summoningModule.ModuleLog(moduleId, "Slide arose into a JumpObstacle in tile {0}", tileToLookAt);
                    return string.Empty;
                }

                // summoningModule.ModuleLog(moduleId, "Slide successfully arose in tile {0}", tileToLookAt);
                // But if this is a clear tile; we can then do any action!
                break;

        }


        // Search one level deeper!!
        // With Step
        correctMovementPattern = GenerateNextMove(tileToLookAt + 1, PabloMovementType.Step, 0);
        if (correctMovementPattern.Length > 0)
        {
            return GetCharacterFromMovementType(movementType) + correctMovementPattern;
        }

        // With Jump
        correctMovementPattern = GenerateNextMove(tileToLookAt + 1, PabloMovementType.Jump, 0);
        if (correctMovementPattern.Length > 0)
        {
            return GetCharacterFromMovementType(movementType) + correctMovementPattern;
        }

        // With Slide
        correctMovementPattern = GenerateNextMove(tileToLookAt + 1, PabloMovementType.Slide, 0);
        if (correctMovementPattern.Length > 0)
        {
            return GetCharacterFromMovementType(movementType) + correctMovementPattern;
        }


        // If none of the subtrees returned true, then this whole tree is bad!
        return string.Empty;
    }

    string GetCharacterFromMovementType(PabloMovementType movementType)
    {
        if (movementType == PabloMovementType.Step)
        { return ">"; }

        if (movementType == PabloMovementType.Jump)
        { return "^"; }

        return "v";
    }

    void FinalizePath()
    {
        // Show value on plate
        plateVoidText.text = voidedCellsIndices.Join("\n");
        summoningModule.ModuleLog(moduleId, "After generating the puzzle; the Voided path tiles are {0}.", voidedCellsIndices.Join());
        summoningModule.ModuleLog(moduleId, "One possible valid movement is '{0}'.", correctMovementPattern.Join(""));
        summoningModule.ModuleLog(moduleId, "A > represents a regular movement forward, ^ represents a jump and v represents a slide. Note that this represents button presses only; jumps and slides still last multiple tiles!");
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

        if (commandParts.Length == 1 && commandParts[0] == "blank")
        {
            yield return "sendtochat {0} Successfully pressed BLANK and reset the module.";
            CasingTextButtonGetsPressed();
            yield break;
        }

        if (commandParts.Length != 2)
        {
            yield return "sendtochaterror {0} Received command formatted incorrectly!";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s" && commandParts[0] != "press" && commandParts[0] != "p")
        {
            yield return "sendtochaterror {0} Please use keyword Press or just p to submit an answer";
            yield break;
        }

        foreach (char _movementSubmission in commandParts[1])
        {
            switch (_movementSubmission)
            {
                case 't': case 'u': case '^':
                    OnPressedMovementButton(PabloMovementType.Jump);
                    break;

                case 'b': case 'd': case 'v':
                    OnPressedMovementButton(PabloMovementType.Slide);
                    break;

                case 'c': case 'm': case '>':
                    OnPressedMovementButton(PabloMovementType.Step);
                    break;

                default:
                    yield return "sendtochaterror {0} Unknown character " + _movementSubmission + " received. Pablo currently is at tile index " + currentPabloIndex.ToString();
                    yield break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        yield return null;
    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        // Reset before doing anything
        CasingTextButtonGetsPressed();

        yield return new WaitForSeconds(0.1f);

        // Press all buttons
        foreach (char _movementSubmission in correctMovementPattern)
        {
            switch (_movementSubmission)
            {
                case '^':
                    OnPressedMovementButton(PabloMovementType.Jump);
                    break;

                case 'v':
                    OnPressedMovementButton(PabloMovementType.Slide);
                    break;

                case '>':
                    OnPressedMovementButton(PabloMovementType.Step);
                    break;

                default:
                    break;
            }

            yield return new WaitForSeconds(0.1f);
        }

        yield break;
    }
}



/*
 * =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
 * VEYARD  -  CODE GRAVEYARD  -  CODE GRAVEYARD  -  CODE GRAVEYARD  -  CODE 
 *  CODE GRAVEYARD  -  CODE GRAVEYARD  -  CODE GRAVEYARD  -  CODE GRAVEYARD
 * RD  -  CODE GRAVEYARD  -  CODE GRAVEYARD  -  CODE GRAVEYARD  -  CODE GRA
 * =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-
 * 
 * 
 * There was an idea to use the First Character of the Mission's Display name as
 * part of the characters used for path creation
 * 
 * However, the code I found that does this (from Mission Control) gets values
 * such as "custom" for freeplay, "undefined" sometimes, or stuff like
 * "mod_missionpack_VFlyer_missionModuleCorruption"
 * or "mod_jamMissions_Espik"
 * or "mod_theBombsBlanMade_mountainBside"
 * 
 * There is no way to extract the correct name from that, especially with missing "The"
 * and other formatting oddities
 * 
 * so nope!!
 * 
 * 
 * // First character of mission name
        // Code for this from Mission Control; credit to Espik
        string _missionName = "";
        try {
            Component gameplayState = GameObject.Find("GameplayState(Clone)").GetComponent("GameplayState");
            Type type = gameplayState.GetType();
            FieldInfo fieldMission = type.GetField("MissionToLoad", BindingFlags.Public | BindingFlags.Static);
            _missionName = fieldMission.GetValue(gameplayState).ToString();
        }
        catch (NullReferenceException)
        {
            _missionName = "";
        }

        // Trycatch errors in the mission name retrieving logic
        if (_missionName.Length == 0)
        {
            // No mission found
            _charactersForPathCreation += 'N';
            summoningModule.ModuleLog(moduleId, "No mission name found. Using 'N' instead. Character chain now is {0}", _charactersForPathCreation);
        }
        // For some reason I found that logging with a } as single-character as input for format breaks stuff, probably because Format uses {}
        // So an explicit checks helps avoiding nullexceptions
        else if (_missionName[0] == '{' || _missionName[0] == '}')
        {
            _charactersForPathCreation += 'N';
            summoningModule.ModuleLog(moduleId, "No Mission starts with a forbidden character . Using 'N' instead. Character chain now is {0}", _charactersForPathCreation);
        }
        // Explicit check for Freeplay because apparently the mission name is called "custom" and so is detected as valid
        else if (_missionName == "custom")
        {
            summoningModule.ModuleLog(moduleId, "Freeplay / Dynamic Mission Generator mode (mission name 'custom') detected! Using 'N' as a character.");
        }
        // Explicite check for "undefined" since that also can happen according to Mission Control's code
        else if (_missionName == "undefined")
        {
            summoningModule.ModuleLog(moduleId, "Unknown mission (mission name 'undefined') detected! Using 'N' as a character.");
        }
        // Make sure the first character is alphanumerical
        else if (Regex.Match(_missionName[0].ToString(), "^[a-zA-Z0-9]").Success)
        {
            _charactersForPathCreation += _missionName[0];

            summoningModule.ModuleLog(moduleId, "Gathered mission name '{0}'. First character is {1} which is valid. Character chain now is {2}",
                _missionName, _missionName[0], _charactersForPathCreation);
        }
        else
        {
            _charactersForPathCreation += 'N';

            summoningModule.ModuleLog(moduleId, "Gathered mission name '{0}'. First character is {1} which is not A-9. Using N instead. Character chain now is {2}",
                _missionName, _missionName[0], _charactersForPathCreation);
        }
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * */