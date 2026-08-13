using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkyPlate : PlateBase {

    /*
    *   Ruleseed Support:
    *   The time calculation for each Travel Type is randomized
    */

    /// <summary> Which Edgework elements can be used as Flight Duration Dependencies </summary>
    public enum FlightDurationEdgeworkElement { Battery, Port, Indicator, BatteryHolder, PortPlate, AABattery, DBattery, LitIndicator, UnlitIndicator, SerialNumberDigit, SerialNumberLetter }
    /// <summary> Struct representing the Rules and Dependencies used to compute a Flight Duration </summary>
    public struct RuleseedFlightDuration
    {
        public int numberOfEdgeworkDependencies;
        public FlightDurationEdgeworkElement firstEdgeworkType;
        public FlightDurationEdgeworkElement secondEdgeworkType;
        public int singularDuration;
    }

    /// <summary> Array containing the four Ruleseeded Flight Duration, ready to be computed </summary>
    RuleseedFlightDuration[] determinedRuleseedDurations = new RuleseedFlightDuration[4];


    /// <summary> All four possible Flight Duration Types as described in the manual </summary>
    public enum FlightDurationSymbol { Circle, Square, Triangle, Star }

    /// <summary> Duration of the flights after they've been computed as a fixed value </summary>
    Dictionary<FlightDurationSymbol, int> ComputedFlightDurations;

    /// <summary> The two possible Line Parities, used to determine what happens with Voided Cities </summary>
    public enum FlightLineParity { Full, Dotted };

    /// <summary> Structure representing the Flights available from a given City. They are considered one-way for ease of code architecture </summary>
    public struct Flight
    {
        /// <summary> String of the City you land on after taking this flight. Flights are considered one-way in this case. </summary>
        public string otherConnectedCityName;
        /// <summary> Duration of this flight </summary>
        public FlightDurationSymbol flightDuration;
        /// <summary> Line Parity of this flight </summary>
        public FlightLineParity lineParity;
    }

    /// <summary> Structure representing one of the 26 Cities </summary>
    public struct City
    {
        /// <summary> Letter associated with the City </summary>
        public string cityName;
        /// <summary> All flights that depart from this City </summary>
        public Flight[] allConnectedFlights;
    }

    /// <summary> All Cities in the graph, each with their own 4 flights </summary>
    readonly City[] allCities = new City[26] {
    new City{ cityName = "A", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "B", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "C", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "D", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "E", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "F", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "G", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "H", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "I", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "J", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "K", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "L", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "M", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "N", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "O", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "P", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "Q", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "R", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "S", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "T", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "U", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "V", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "W", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationSymbol.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "X", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "Y", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationSymbol.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "Z", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationSymbol.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationSymbol.Circle, lineParity = FlightLineParity.Dotted } } }
    };


    enum TravelType { PlayerMovement, PuzzleGeneration, Logging};


    string targetCityName, startingCityName;
    [SerializeField] TextMesh citiesInformationText;
    [SerializeField] TextMesh timerInformationText;

    /// <summary>
    /// List of the Log messages (in reverse order) that make the path to the target.
    /// They are build upon puzzle generation (in reverse order), and then logged.
    /// </summary>
    List<string> expectedPathLog = new List<string>();

    City currentCity;
    int currentTimer;
    int finalTimerToSolve;

    // Universal Logging Data
    static int moduleIdCounter = 1;

    string currentPlayerInput;

    // Buttons gathering and GetComponents
    public override void InitializeModuleAwake()
    {
        base.InitializeModuleAwake();

        moduleId = moduleIdCounter++;

        platePressableButtons[0].OnInteract += delegate () { PressingPlateButton("dot"); return false; };
        platePressableButtons[1].OnInteract += delegate () { PressingPlateButton("dash"); return false; };
        platePressableButtons[2].OnInteract += delegate () { PressingPlateButton("submit"); return false; };

        currentPlayerInput = "";
    }

    // Puzzle Initialization
    public override void InitializeModuleStart()
    {
        // No need to log, this is done in the summoningModule
        base.InitializeModuleStart();

        // VerifyFlightDataIntegrity();

        GenerateSkyPuzzle();

    }

    // public override void UpdateModule() { base.UpdateModule(); }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Player Inputs
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=

    void PressingPlateButton(string buttonType)
    {
        platePressableButtons[0].AddInteractionPunch( buttonType == "submit" ? 1f : 0.7f);
        PlayPlatePressSound();

        if (summoningModule.isModuleSolved) { return; }

        switch (buttonType)
        {
            case "dot":
                currentPlayerInput += ".";
                summoningModule.ModuleLog(moduleId, "Added a dot to the current submission. It currently is {0}", currentPlayerInput);
                break;

            case "dash":
                currentPlayerInput += "-";
                summoningModule.ModuleLog(moduleId, "Added a dash to the current submission. It currently is {0}", currentPlayerInput);
                break;

            case "submit":

                VerifyPlayerAnswer();
                // Reset Player Input
                currentPlayerInput = "";

                break;
        }
    }

    void VerifyPlayerAnswer()
    {
        int _result = Array.IndexOf(morseCodeAlphabet, currentPlayerInput);

        // Submitted unknown Morse Character
        if (_result == -1)
        {
            summoningModule.ModuleLog(moduleId, "Submitted unknown Morse '{0}'. Strike!!! ", currentPlayerInput);
            ModuleShouldStrike();
            return;
        }
        
        
        // Else, we submitted a Letter
        
        // Try to move
        int totalTravelTime = 0;
        
        // TryMoveToCity handles the Strikes itself, so we just check result
        string _landedOnCity = TryMoveToCity(alphabet[_result], currentCity, ref totalTravelTime, TravelType.PlayerMovement);
        
        // ¤ is the Error Character, because I like that character
        if (_landedOnCity != "¤")
        {
            // Move City
            currentCity = GetCityFromName(_landedOnCity);
        
            // Reduce Time
            currentTimer -= totalTravelTime;
        
            // WaitForNextPlane => Go to the next 600s or 10 minutes interval
            currentTimer -= (currentTimer % 600);

            summoningModule.ModuleLog(moduleId, "Landed in City {0}, with a current timer (after Waiting for Next Plane To Arrive) of {1}.",
                currentCity.cityName, GetFullyLoggableTime(currentTimer));

            if (currentTimer <= 0)
            {
                if (currentCity.cityName == targetCityName)
                {
                    summoningModule.ModuleLog(moduleId, "Timer ran out and successfully landed in City {0}. Good Job!",
                        currentCity.cityName);
                    ModuleShouldSolve();
                }
                else
                {
                    summoningModule.ModuleLog(moduleId, "Timer ran out but current City {0} is not Target City {1}... STRIKE!!!",
                        currentCity.cityName, targetCityName);
                    ModuleShouldStrike();
                }
            }
        }
        
    }

    protected override void CasingTextButtonGetsPressed() 
    {
        platePressableButtons[0].AddInteractionPunch();

        if (summoningModule.isModuleSolved) { return; }

        currentCity = GetCityFromName(startingCityName);
        currentTimer = finalTimerToSolve;

        summoningModule.ModuleLog(moduleId, "Resetting you to your Starting City {0} and your Starting Timer {1}", currentCity.cityName, currentTimer);
    }


    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
    //    Puzzle Initialization
    // =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=


    void GenerateSkyPuzzle()
    {
        // Generate Void cities
        GenerateVoidedCities();

        // Generate Flight Durations:
        ComputeAllFlightDurations();

        // Generate the puzzle in reverse:
        // Start at the end city, move 3-5 times, and that's the starting city
        // The Timer starts at 00:00:00
        // For each movement, we add the Flight Duration for each movement, and rounds up to XX:X0:00 time
        // The Round Up is equivalent to the WaitingForPlaneToArrive; which is a RoundDown
        // This becomes the time at which you leave the previous City
        // After moving a few times (making sure not to go above 23:59:59), stop here, at the Starting City


        DetermineTargetCity();


        // Random maximum number of movements between 2 and 4
        // With Void, this can already turn into a Nightmare because there are A LOT of paths to search for
        // The longer the path, the more this turns into TFC-abuse, and I don't wan't that
        int _maximumMovements = UnityEngine.Random.Range(2, 5);

        // Initialize variables
        currentTimer = 0;
        int _timerToAdd = 0;
        string _landedOnCity;
        string _initialFlightTargetCity;

        // Move several times
        for (int i = 0; i < _maximumMovements; i ++)
        {
            // Reset timer for this singular movement
            _timerToAdd = 0;

            _initialFlightTargetCity = currentCity.allConnectedFlights.PickRandom().otherConnectedCityName;

            // Try to move to a new, random valid city
            _landedOnCity = TryMoveToCity(_initialFlightTargetCity, currentCity, ref _timerToAdd, TravelType.PuzzleGeneration);

            // If this would bring us above the Maximum Time, then ignore and break right now with the current values
            // Instead of 86399 (23:59:59), verify against 85799 (23:49:59) so that we still can round up and add a random value safely
            if ((currentTimer + _timerToAdd) > 85799)
            {
                summoningModule.ModuleLog(moduleId, "Expected flight duration from {0} to {1} would exceed the of 24 hours; so it has been aborted and removed from expected movements.",
                    currentCity.cityName, _initialFlightTargetCity);

                // Movement has been aborted, so remove it from the expected path!
                expectedPathLog.RemoveAt(0);
                
                break;
            }

            // Otherwise, add to Timer
            currentTimer += _timerToAdd;

            // Round up to the nearest higher 600 seconds value
            // to take into account the Waiting for the Next Plane to Arrive
            currentTimer = Mathf.CeilToInt((float)currentTimer / 600f) * 600;


            // Apply the current city
            currentCity = GetCityFromName(_landedOnCity);


            // Log
            // summoningModule.ModuleLog(moduleId, "Successfully moved to {0} in a total time of {1}. Accounting for Next Plane to Arrive, current Timer is now {2}",
            //    _landedOnCity, GetFullyLoggableTime(_timerToAdd), GetFullyLoggableTime(currentTimer));
        }



        // Save Starting City
        startingCityName = currentCity.cityName;
        finalTimerToSolve = currentTimer;

        summoningModule.ModuleLog(moduleId, "Puzzle Generation Finished - You have {0} to go from City {1} to City {2}",
            GetFullyLoggableTime(finalTimerToSolve), startingCityName, targetCityName);


        // Log Expected Path
        LogExpectedPath();

        // Show on Plate
        MarkDataOnPlateText();
    }


    void DetermineTargetCity()
    {
        // Start at the TargetCity
        // Take only a city neighboring with Void to favorize a path that goes through it.
        List<string> _nonVoidAlphabet = new List<string>();

        // Add all neighbors of the Voided City
        foreach (int _voidedCityIndex in voidedCellsIndices)
        {
            City voidedCity = GetCityFromName(alphabet[_voidedCityIndex]);

            foreach (Flight _flight in voidedCity.allConnectedFlights)
            {
                _nonVoidAlphabet.Add(_flight.otherConnectedCityName);
            }
        }


        // Can't have a Target that is voided!!
        // List<string> _nonVoidAlphabet = alphabet.ToList();
        _nonVoidAlphabet.RemoveAll(l => voidedCellsIndices.Contains(Array.IndexOf(alphabet, l)));

        targetCityName = _nonVoidAlphabet.PickRandom();
        currentCity = GetCityFromName(targetCityName);

        // Log
        // summoningModule.ModuleLog(moduleId, "Starting Puzzle Generation with Target City {0}.", targetCityName);
    }

    void LogExpectedPath()
    {
        // Due to the way the puzzle is generated, "expectedPathLog" stores data in this way
        // MovingTo_1A {MovingTo_1B MovingTo_1C} TotalTimeOfMovement1 
        // MovingTo_2A {MovingTo_2B MovingTo_2C} TotalTimeOfMovement1 
        // MovingTo_3A {MovingTo_3B MovingTo_3C} TotalTimeOfMovement1 

        // B and C might not exist if A is not Voided
        // Total Time of Movement does NOT take into account the TimeBetweenFlights so that's all good
        // We can just use those values and do the path in the correct time by subtracting the times!

        summoningModule.ModuleLog(moduleId, "Expected Path is:");

        // summoningModule.ModuleLog(moduleId, "All recorded movements: {0}", expectedPathLog.Join(" // "));

        int _timeRemaining = finalTimerToSolve;
        summoningModule.ModuleLog(moduleId, "Starting in City {0} with {1} left.", startingCityName, GetFullyLoggableTime(finalTimerToSolve));


        // Prepare Data
        int flightDuration;
        int timeAfterMovement;
        int timeAfterWaitingToArrive;
        // End Prepare Data

        foreach (string _movementLog in expectedPathLog)
        {
            // Movement does NOT have any Void
            if (_movementLog[1] == '-')
            {
                // Time computation
                flightDuration = int.Parse(_movementLog.Substring(2));
                timeAfterMovement = _timeRemaining - flightDuration;
                timeAfterWaitingToArrive = timeAfterMovement - (timeAfterMovement % 600);

                summoningModule.ModuleLog(moduleId, "Moving directly to City {0} takes {1}. Time remaining upon landing is {2}. After Waiting For Plane To Arrive it becomes {3}.",
                    _movementLog[0], GetFullyLoggableTime(flightDuration), GetFullyLoggableTime(timeAfterMovement), GetFullyLoggableTime(timeAfterWaitingToArrive));

            }
            // Movement DOES have Void
            else
            {
                int _pointer = 0;
                string voidedCitiesPath = "";

                // Check the Voided Cities
                while (alphabet.Contains(_movementLog[_pointer].ToString()))
                {
                    if (_pointer > 1)
                    {
                        voidedCitiesPath += " then " + _movementLog[_pointer];
                    }
                    else if (_pointer == 1)
                    {
                        voidedCitiesPath += _movementLog[_pointer];
                    }
                    _pointer++;
                }

                // Skip over the "-" separator
                _pointer++;

                // Time computation
                flightDuration = int.Parse(_movementLog.Substring(_pointer));
                timeAfterMovement = _timeRemaining - flightDuration;
                timeAfterWaitingToArrive = timeAfterMovement - (timeAfterMovement % 600);

                summoningModule.ModuleLog(moduleId, "Moving to Voided City {0} takes you to {4}, taking a total of {1}. Time upon landing is {2}. After Waiting For Plane To Arrive it becomes {3}.",
                    _movementLog[0], GetFullyLoggableTime(flightDuration), GetFullyLoggableTime(timeAfterMovement), GetFullyLoggableTime(timeAfterWaitingToArrive), voidedCitiesPath);
            }

            // Apply time
            _timeRemaining = timeAfterWaitingToArrive;
        }


    }

    void GenerateVoidedCities()
    {
        char[] letters = bombInfo.GetSerialNumberLetters().Distinct().ToArray();
        string voidedCities = "";

        foreach (char _letter in letters)
        {
            // Add the index of the Letter, we don't care about repeats
            voidedCellsIndices.Add(Array.IndexOf(alphabet, _letter.ToString().ToUpper()));

            voidedCities += alphabet[voidedCellsIndices.Last()] + " ";
        }

        summoningModule.ModuleLog(moduleId, "Using the Serial Number, the Voided Cities are {0}.", voidedCities.Remove(voidedCities.Length-1));
    }

    void MarkDataOnPlateText()
    {
        citiesInformationText.text = startingCityName + "   " + targetCityName;
        timerInformationText.text = currentTimer.ToString();
    }
    

    /// <summary> Returns the string of the new City, and the travel time; because multiple travels can happen due to Void.
    /// Travel Type determines which info is saved, or what can be logged.</summary>
    string TryMoveToCity(string flightCityTargetName, City departCity, ref int totalTravelTime, TravelType travelType, string previousVoid = "")
    {
        // Verify if the City is available for travel right now
        bool _isFlightValid = false;
        Flight flightTaken = new Flight();

        foreach (Flight _connectedFlights in departCity.allConnectedFlights)
        {
            if (_connectedFlights.otherConnectedCityName == flightCityTargetName)
            {
                _isFlightValid = true;
                flightTaken = _connectedFlights;
                break;
            }
        }
        if (_isFlightValid == false)
        {
            summoningModule.ModuleLog(moduleId, "Tried to move from {0} to {1} but no valid Flights exist! STRIKE!!!!", departCity.cityName, flightCityTargetName);
            ModuleShouldStrike();

            totalTravelTime = 0;
            return "¤";
        }



        // Here, the Flight is valid, so save the City
        City _landedOnCity = GetCityFromName(flightCityTargetName);

        // Save differently the finalCity because it can be overriden by Voided Flights
        string finalCity = _landedOnCity.cityName;


        // Increase the given TravelTime for this whole Flight (chain?)
        totalTravelTime += GetFlightDuration(flightTaken.flightDuration);


        // If Voided, Move again, following the same Line Parity (Full or Dotted)
        if (IsCityVoided(_landedOnCity))
        {
            // On the current City, check the Flights
            foreach (Flight _connectedFlight in _landedOnCity.allConnectedFlights)
            {
                // Ignore different Line Parities
                if (_connectedFlight.lineParity != flightTaken.lineParity)
                { continue;}

                // Ignore the Flight that brings us where we just were
                if (_connectedFlight.otherConnectedCityName == departCity.cityName)
                { continue; }


                // Otherwise, Move once more!
                // Log only if the Player is moving
                if (travelType == TravelType.PlayerMovement)
                {
                    summoningModule.ModuleLog(moduleId, "Landed in city {0}, but since it is Voided, continue moving forward!", _landedOnCity.cityName);
                }
                

                // Save the new City that gets returned up the chain of Voided Flights
                // Pass the list of previous Voided cities so we finish a good chain
                finalCity = TryMoveToCity(_connectedFlight.otherConnectedCityName, _landedOnCity, ref totalTravelTime, travelType, departCity.cityName + previousVoid);
            }

        }
        // Not Voided!
        // We landed somewhere Valid!
        else if (travelType == TravelType.PuzzleGeneration)
        {
            // Just log that information
            // previousVoid is empty if no Void has been moved to, so it's okay
            expectedPathLog.Insert(0, departCity.cityName + previousVoid + "-" + totalTravelTime);
            
            
            // summoningModule.ModuleLog(moduleId, "Added reverse movement from {0} in {1}!", _landedOnCity.cityName, totalTravelTime);
        }

        // Return the city we landed on at the very end
        return finalCity;
    }


    int GetFlightDuration(FlightDurationSymbol flightDurationType)
    {
        int _value = 0;

        ComputedFlightDurations.TryGetValue(flightDurationType, out _value);

        return _value;
    }

    void GenerateRuleseedRules()
    {
        MonoRandom Rng = ruleseedManager.GetRNG();
        if (Rng.Seed == 1)
        { GetDefaultFlightDurationRules(); return; }

        summoningModule.ModuleLog(moduleId, "Detected Ruleseed {0}. Rules are:", Rng.Seed);

        /*
        *   Flight Durations can be dependant on:
        *   - no edgework at all (time between 2h and 5h)
        *   - one piece of edgework (time between 20' and 1h20')
        *   - sum of two pieces of edgework (time between 15' and 45')
        *   
        *   Randomization Array to make sure not too much degeneracy is here:
        *   { None, None, One, One, One, Two }
        *   
        *   Time is rounded to the nearest 5 for both seconds and minutes (no 0:24:23, but 0:25:25 accepted)
        *   
        *   
        *   Edgework allowed (single or with pair)
        *   - Battery
        *   - Port
        *   - Indicator
        *   - Battery Holder
        *   - Port Plate
        *   - AA Battery
        *   - D Battery
        *   - Lit Indicator
        *   - Unlit Indicator
        *   - Digit in SN
        *   - Letter in SN
        *   
        *   
        *   Default is 
        *   CIRCLE  -00:35:20 per Battery
        *   SQUARE  -00:40:15 per (Port Plate and Indicator)
	    *   TRIANG  -01:15:30 per Port
	    *   STAR    -03:45:05 
        */

        // Create the arrays for the Ruleseed to make sure it's not entirely RNG
        int[] _possibleDependencyNumbers = new int[6] { 0, 0, 1, 1, 1, 2};

        FlightDurationEdgeworkElement[] _possibleDurationEdgeworks = new FlightDurationEdgeworkElement[11] { FlightDurationEdgeworkElement.Battery, FlightDurationEdgeworkElement.Port, FlightDurationEdgeworkElement.Indicator,
            FlightDurationEdgeworkElement.BatteryHolder, FlightDurationEdgeworkElement.PortPlate, FlightDurationEdgeworkElement.AABattery, FlightDurationEdgeworkElement.DBattery, FlightDurationEdgeworkElement.LitIndicator,
            FlightDurationEdgeworkElement.UnlitIndicator, FlightDurationEdgeworkElement.SerialNumberDigit, FlightDurationEdgeworkElement.SerialNumberLetter};

        // Shuffle those according to Ruleseed
        FisherYatesShuffle(ref _possibleDependencyNumbers, Rng);
        FisherYatesShuffle(ref _possibleDurationEdgeworks, Rng);



        // Construct the RuleseedFlightDurations
        RuleseedFlightDuration _generatedFlightDuration;
        for (int i = 0; i < 4; i ++)
        {
            // Construct the struct
            _generatedFlightDuration = new RuleseedFlightDuration();

            // Give it the Dependency Type
            _generatedFlightDuration.numberOfEdgeworkDependencies = _possibleDependencyNumbers[i];

            // Give it up to two edgework data, even if they are unused it's okay
            _generatedFlightDuration.firstEdgeworkType = _possibleDurationEdgeworks[i * 2];
            _generatedFlightDuration.secondEdgeworkType = _possibleDurationEdgeworks[i * 2 + 1];


            // Randomly get a value depending on the Flight Duration Dependency 
            int _generatedSingularDuration = 0;
            switch (_generatedFlightDuration.numberOfEdgeworkDependencies)
            {
                case 0:
                    // Time between [2h; 5h[

                    // 2h00' and 4h55' minutes inclusive (in range of 5)
                    _generatedSingularDuration += Rng.Next(24, 60) * 300;
                    // 0 and 55 seconds inclusive (in range of 5)
                    _generatedSingularDuration += Rng.Next(0, 12) * 5;


                    // Log
                    summoningModule.ModuleLog(moduleId, "{0}: No Edgework dependency. Flat duration is {1}.",
                        ((FlightDurationSymbol)i).ToString(), GetFullyLoggableTime(_generatedSingularDuration));
                    break;

                case 1:
                    // Time between [20'; 1h20'[

                    // 20 and 80 minutes inclusive (in range of 5)
                    _generatedSingularDuration += Rng.Next(4, 16) * 300;
                    // 0 and 55 seconds inclusive (in range of 5)
                    _generatedSingularDuration += Rng.Next(0, 12) * 5;

                    // Log
                    summoningModule.ModuleLog(moduleId, "{0}: One Edgework dependency. Duration is {1} per {2}.",
                        ((FlightDurationSymbol)i).ToString(), GetFullyLoggableTime(_generatedSingularDuration), _generatedFlightDuration.firstEdgeworkType.ToString());
                    break;

                case 2:
                    // Time between [15'; 45'[

                    // 15 and 40 minutes inclusive (in range of 5)
                    _generatedSingularDuration += Rng.Next(3, 9) * 300;
                    // 0 and 55 seconds inclusive (in range of 5)
                    _generatedSingularDuration += Rng.Next(0, 12) * 5;

                    // Log
                    summoningModule.ModuleLog(moduleId, "{0}: Two Edgework dependencies. Duration is {1} per ({2} + {3}).",
                        ((FlightDurationSymbol)i).ToString(), GetFullyLoggableTime(_generatedSingularDuration),
                        _generatedFlightDuration.firstEdgeworkType.ToString(), _generatedFlightDuration.secondEdgeworkType.ToString());
                    break;
            }
            // Apply that value
            _generatedFlightDuration.singularDuration = _generatedSingularDuration;


            // Save that generated Flight Duration
            determinedRuleseedDurations[i] = _generatedFlightDuration;
        }
    }

    void GetDefaultFlightDurationRules()
    {
        // Default values are
        // CIRCLE  -00:35:20 per Battery
        // SQUARE  -00:40:15 per (Port Plate and Indicator)
        // TRIANG  -01:15:30 per Port
        // STAR    -03:45:05

        // Circle
        determinedRuleseedDurations[0] = new RuleseedFlightDuration()
            { numberOfEdgeworkDependencies = 1, firstEdgeworkType = FlightDurationEdgeworkElement.Battery, singularDuration = 2120 };

        // Square
        determinedRuleseedDurations[1] = new RuleseedFlightDuration()
            { numberOfEdgeworkDependencies = 2, firstEdgeworkType = FlightDurationEdgeworkElement.PortPlate, 
            secondEdgeworkType = FlightDurationEdgeworkElement.Indicator, singularDuration = 2415 };

        // Triangle
        determinedRuleseedDurations[2] = new RuleseedFlightDuration()
            { numberOfEdgeworkDependencies = 1, firstEdgeworkType = FlightDurationEdgeworkElement.Port, singularDuration = 4530 };

        // Star
        determinedRuleseedDurations[3] = new RuleseedFlightDuration() { numberOfEdgeworkDependencies = 0, singularDuration = 13505 };
    }

    void ComputeAllFlightDurations()
    {
        GenerateRuleseedRules();

        summoningModule.ModuleLog(moduleId, "All Flight Durations are:");

        ComputedFlightDurations = new Dictionary<FlightDurationSymbol, int>();


        // // Fake high durations to test over-24h limit
        // AllFlightDurations.Add(FlightDurationType.Circle, 30000);
        // AllFlightDurations.Add(FlightDurationType.Square, 32500);
        // AllFlightDurations.Add(FlightDurationType.Triangle, 35000);
        // AllFlightDurations.Add(FlightDurationType.Star, 37500);
        // return;


        // Prepare Memory Locations
        FlightDurationSymbol _flightSymbol;
        int _firstEdgeworkDependency = 0;
        int _secondEdgeworkDependency = 0;
        int _totalDuration = 0;

        for (int i = 0; i < 4; i ++)
        {
            // Circle, Square, Triangle, Star
            _flightSymbol = (FlightDurationSymbol)i;


            switch (determinedRuleseedDurations[i].numberOfEdgeworkDependencies)
            {
                case 0:

                    // No edgework, just a singular value
                    _totalDuration = determinedRuleseedDurations[i].singularDuration;

                    summoningModule.ModuleLog(moduleId, "{0}: Duration is {1}.", _flightSymbol.ToString(), GetFullyLoggableTime(_totalDuration));
                    break;


                case 1:

                    // One edgework type
                    _firstEdgeworkDependency = GetEdgeworkDependencyData(determinedRuleseedDurations[i].firstEdgeworkType);
                    
                    _totalDuration = determinedRuleseedDurations[i].singularDuration * Mathf.Clamp(_firstEdgeworkDependency, 1, 9);

                    summoningModule.ModuleLog(moduleId, "{0}: Duration is {1} because of the {2} {3}.",
                        _flightSymbol.ToString(), GetFullyLoggableTime(_totalDuration), _firstEdgeworkDependency, determinedRuleseedDurations[i].firstEdgeworkType.ToString());

                    break;


                case 2:

                    // Two edgework types
                    _firstEdgeworkDependency = GetEdgeworkDependencyData(determinedRuleseedDurations[i].firstEdgeworkType);
                    _secondEdgeworkDependency = GetEdgeworkDependencyData(determinedRuleseedDurations[i].secondEdgeworkType);

                    _totalDuration = determinedRuleseedDurations[i].singularDuration * Mathf.Clamp(_firstEdgeworkDependency + _secondEdgeworkDependency, 1, 9);

                    summoningModule.ModuleLog(moduleId, "{0}: Duration is {1} because of the {2} {3} and {4} {5}.",
                        _flightSymbol.ToString(), GetFullyLoggableTime(_totalDuration), _firstEdgeworkDependency, determinedRuleseedDurations[i].firstEdgeworkType.ToString(),
                        _secondEdgeworkDependency, determinedRuleseedDurations[i].secondEdgeworkType.ToString());
                    break;
            }

            // It has been computed, it can now be added!
            ComputedFlightDurations.Add(_flightSymbol, _totalDuration);
        }
    }

    int GetEdgeworkDependencyData(FlightDurationEdgeworkElement edgeworkType)
    {
        switch (edgeworkType)
        {
            case FlightDurationEdgeworkElement.Battery:
                return bombInfo.GetBatteryCount();

            case FlightDurationEdgeworkElement.Port:
                return bombInfo.GetPortCount();

            case FlightDurationEdgeworkElement.Indicator:
                return bombInfo.GetIndicators().Count();

            case FlightDurationEdgeworkElement.BatteryHolder:
                return bombInfo.GetBatteryHolderCount();

            case FlightDurationEdgeworkElement.PortPlate:
                return bombInfo.GetPortPlateCount();

            case FlightDurationEdgeworkElement.AABattery:
                return bombInfo.GetBatteryCount(Battery.AA);

            case FlightDurationEdgeworkElement.DBattery:
                return bombInfo.GetBatteryCount(Battery.D);

            case FlightDurationEdgeworkElement.LitIndicator:
                return bombInfo.GetOnIndicators().Count();

            case FlightDurationEdgeworkElement.UnlitIndicator:
                return bombInfo.GetOffIndicators().Count();

            case FlightDurationEdgeworkElement.SerialNumberDigit:
                return bombInfo.GetSerialNumberNumbers().Count();

            case FlightDurationEdgeworkElement.SerialNumberLetter:
                return bombInfo.GetSerialNumberLetters().Count();
        }

        summoningModule.ModuleLogError(moduleId, "Tried to get unknown FlightDuration Edgework Element value: {0}. Please report this to thunder725", edgeworkType.ToString());
        return 0;
    }

    string GetReadableHourNotationFromTime(int time)
    {
        return String.Format("{0:D2}:{1:D2}:{2:D2}", time / 3600, (time % 3600) / 60, time % 60);
    }

    bool IsCityVoided(City cityToCheck)
    {
        return IsCityVoided(cityToCheck.cityName);
    }

    bool IsCityVoided(string cityName)
    {
        return voidedCellsIndices.Contains(Array.IndexOf(alphabet, cityName));
    }

    City GetCityFromName(string cityName)
    {
        return allCities[Array.IndexOf(alphabet, cityName)];
    }

    string GetFullyLoggableTime(int time)
    {
        return String.Format("{0} seconds or {1}", time, GetReadableHourNotationFromTime(time));
    }

    void VerifyFlightDataIntegrity()
    {
        City _connectedCity;

        foreach (City _cityToTest in allCities)
        {
            // Test every Flight in data
            foreach (Flight _flightToTest in _cityToTest.allConnectedFlights)
            {
                // Get connected city 
                _connectedCity = GetCityFromName(_flightToTest.otherConnectedCityName); ;
                

                // Find the struct that represents the same flight
                foreach (Flight _potentiallyConnectedFlight in _connectedCity.allConnectedFlights)
                {
                    if (_potentiallyConnectedFlight.otherConnectedCityName == _cityToTest.cityName)
                    {
                        if (_potentiallyConnectedFlight.lineParity != _flightToTest.lineParity)
                        {
                            summoningModule.ModuleLogError(moduleId, "Found two non-matching Line Parities in-between city {0} and {1}. Please report to thunder725 ASAP :D",
                                _cityToTest.cityName, _connectedCity.cityName);
                        }

                        if (_potentiallyConnectedFlight.flightDuration != _flightToTest.flightDuration)
                        {
                            summoningModule.ModuleLogError(moduleId, "Found two non-matching Flight Duration in-between city {0} and {1}. Please report to thunder725 ASAP :D",
                                _cityToTest.cityName, _connectedCity.cityName);
                        }

                        break;
                    }
                }
            }
        }

        summoningModule.ModuleLog(moduleId, "Internal Data Integrity successfully Verified.");
    }

    

    void ModuleShouldStrike()
    {
        currentCity = GetCityFromName(startingCityName);
        currentTimer = finalTimerToSolve;

        summoningModule.ModuleLog(moduleId, "Resetting you to your Starting City {0} and your Starting Timer {1}", currentCity.cityName, currentTimer);

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

        if (command == "sky")
        {
            yield return "sendtochat {0} Successfully pressed SKY and reset you to your starting City and starting Timer.";
            CasingTextButtonGetsPressed();
            yield break;
        }

        if (commandParts.Length <= 1)
        {
            yield return "sendtochaterror {0} you must format the submission with “!{1} Submit .-. -.-- -.. ...- -.-.”";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s")
        {
            yield return "sendtochaterror {0} please make sure you Submit with either “Submit” or “s”.";
            yield break;
        }

        // Foreach Letter
        foreach (var part in commandParts)
        {
            // Ignore submit
            if (part == "submit" || part == "s")
            { continue; }


            // Foreach morse part
            foreach (var c in part)
            {
                switch (c)
                {
                    case '.':
                        platePressableButtons[0].OnInteract();
                        break;

                    case '-':
                        platePressableButtons[1].OnInteract();
                        break;
                }

                yield return new WaitForSeconds(0.1f);
            }

            platePressableButtons[2].OnInteract();
        }

    }


    public override IEnumerator TwitchHandleForcedSolve()
    {
        // An Auto-solver would be easy enough to do by recording movements in reverse when generating puzzle...
        // But nahhh
        ModuleShouldSolve();

        yield break;
    }




    /*
    =-=-= CODE CEMETARY =-=-=


    Pre-ruleseed default values initialization
    
    // Circle => 00:35:20 per Battery => 2120

    edgeworkOne = bombInfo.GetBatteryCount();
    _duration = 2120 * Mathf.Clamp(edgeworkOne, 1, 9);

    ComputedFlightDurations.Add(FlightDurationSymbol.Circle, _duration);
    summoningModule.ModuleLog(moduleId, "Circle: " + GetFullyLoggableTime(_duration) + " because of the " + edgeworkOne + " Batteries.");




    // Square => 00:50:45 per Indicator or Port Plate => 3045

    edgeworkOne = bombInfo.GetIndicators().Count();
    edgeworkTwo = bombInfo.GetPortPlateCount();
    _duration = 3045 * Mathf.Clamp(edgeworkOne + edgeworkTwo, 1, 9);

    ComputedFlightDurations.Add(FlightDurationSymbol.Square, _duration);

    summoningModule.ModuleLog(moduleId, "Square: " + GetFullyLoggableTime(_duration) + " because of the " + edgeworkOne + " Indicators and "+ edgeworkTwo + " Port Plates.");



    // Triangle => 01:15:30 per Port => 4530

    edgeworkOne = bombInfo.GetPortCount();
    _duration = 4530 * Mathf.Clamp(edgeworkOne, 1, 9);

    ComputedFlightDurations.Add(FlightDurationSymbol.Triangle, _duration);

    summoningModule.ModuleLog(moduleId, "Triangle: " + GetFullyLoggableTime(_duration) + " because of the " + edgeworkOne + " Ports.");



    // Star => 3:45:05 flat => 13505
    _duration = 13505;

    ComputedFlightDurations.Add(FlightDurationSymbol.Star, _duration);

    summoningModule.ModuleLog(moduleId, "Star: " + GetFullyLoggableTime(_duration));
     
     */
}
