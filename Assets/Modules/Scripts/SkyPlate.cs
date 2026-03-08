using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkyPlate : PlateBase {

    /// <summary> All four possible Flight Duration Types as described in the manual </summary>
    public enum FlightDurationType { Circle, Square, Triangle, Star }

    Dictionary<FlightDurationType, int> AllFlightDurations;

    /// <summary> The two possible Line Parities, used to determine what happens with Voided Cities </summary>
    public enum FlightLineParity { Full, Dotted };

    /// <summary> Structure representing the Flights available from a given City. They are considered one-way for ease of code architecture </summary>
    public struct Flight
    {
        /// <summary> String of the City you land on after taking this flight. Flights are considered one-way in this case. </summary>
        public string otherConnectedCityName;
        /// <summary> Duration of this flight </summary>
        public FlightDurationType flightDuration;
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

    /// <summary>  </summary>
    City[] allCities = new City[26] {
    new City{ cityName = "A", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "B", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "C", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "D", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "E", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "F", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "G", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "H", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Q", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "I", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "J", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "K", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "L", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "M", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "N", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "W", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "V", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "O", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "P", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "Q", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "H", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "R", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "A", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "Y", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "S", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "U", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "T", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "X", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "Z", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "U", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "E", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "V", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "I", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "W", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "J", flightDuration = FlightDurationType.Triangle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "K", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "B", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "N", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "X", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "C", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "G", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "O", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted } } },
    new City{ cityName = "Y", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "P", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "R", flightDuration = FlightDurationType.Star, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "D", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "L", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Full } } },
    new City{ cityName = "Z", allConnectedFlights = new Flight[4] {
        new Flight { otherConnectedCityName = "S", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "M", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted },
        new Flight { otherConnectedCityName = "F", flightDuration = FlightDurationType.Square, lineParity = FlightLineParity.Full },
        new Flight { otherConnectedCityName = "T", flightDuration = FlightDurationType.Circle, lineParity = FlightLineParity.Dotted } } }
    };



    string targetCityName, startingCityName;
    [SerializeField] TextMesh citiesInformationText;
    [SerializeField] TextMesh timerInformationText;

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

        // VerifyDataIntegrity();

        GenerateSkyPuzzle();

    }

    // public override void UpdateModule() { base.UpdateModule(); }

    void PressingPlateButton(string buttonType)
    {
        if (summoningModule.isModuleSolved)
        { return; }

        platePressableButtons[0].AddInteractionPunch();
        summoningModule.PlaySound(platePressedSound);

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
        string _landedOnCity = TryMoveToCity(alphabet[_result], currentCity, ref totalTravelTime);
        
        // ¤ is the Error Character, because I like that character
        if (_landedOnCity != "¤")
        {
            // Move City
            currentCity = GetCityFromName(_landedOnCity);
        
            // Reduce Time
            currentTimer -= totalTravelTime;
        
            // WaitForNextPlane => Go to the next 600s or 10 minutes interval
            currentTimer -= (currentTimer % 600);

            summoningModule.ModuleLog(moduleId, "Landed in City {0}, with a current timer (after Waiting for Next Plane To Arrive) of {1} seconds or {2}.",
                currentCity.cityName, currentTimer, GetReadableHourNotationFromTime(currentTimer));

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


    void GenerateSkyPuzzle()
    {
        // Generate Void cities
        GenerateVoidedCities();

        // Generate Flight Durations:
        ComputeAllFlightDurations();

        // Generate the puzzle in reverse:
        // Start at the end city, move 3-5 times, and that's the starting city
        // The Timer starts at 00:00:00, adds the Flight Duration for each movement, and rounds up to the nearest XX:X0:00 time
        // This becomes the time at which you leave the previous City, to which we add a random 00:0X:XX value to account for Waiting for the Next Plane to Arrive
        // After moving a few times (making sure not to go above 23:59:59), stop here, at the Starting City

        // Start at the TargetCity
        // Can't have a Target that is voided!!
        List<string> _nonVoidAlphabet = alphabet.ToList();
        _nonVoidAlphabet.RemoveAll(l => voidedCellsIndices.Contains(Array.IndexOf(alphabet, l)));

        targetCityName = _nonVoidAlphabet.PickRandom();
        currentCity = GetCityFromName(targetCityName);

        // Log
        summoningModule.ModuleLog(moduleId, "Target City will be {0}.", targetCityName);



        // Random maximum number of movements between 2 and 4
        // With Void, this can already turn into a Nightmare because there are A LOT of paths to search for
        // The longer the path, the more this turns into TFC-abuse, and I don't wan't that
        int _maximumMovements = UnityEngine.Random.Range(2, 5);

        // Initialize variables
        currentTimer = 0;
        int _timerToAdd = 0;
        string _landedOnCity;

        summoningModule.ModuleLog(moduleId, "Puzzle Generation will now move up to {0} times to generate a valid path.", _maximumMovements);

        // Move random amount of times
        for (int i = 0; i < _maximumMovements; i ++)
        {
            // Reset timer for this singular movement
            _timerToAdd = 0;

            // Try to move to a new, random valid city
            _landedOnCity = TryMoveToCity(currentCity.allConnectedFlights.PickRandom().otherConnectedCityName, currentCity, ref _timerToAdd);

            // If this would bring us above the Maximum Time, then ignore and break right now with the current values
            // Instead of 86399 (23:59:59), verify against 85799 (23:49:59) so that we still can round up and add a random value safely
            if (currentTimer + _timerToAdd > 85799)
            {
                break;
            }

            // Otherwise, add to Timer
            currentTimer += _timerToAdd;

            // Add random interval that will get absorbed by Waiting for the Next Plane to Arrive
            currentTimer += UnityEngine.Random.Range(0, 600);



            // Apply the current city
            currentCity = GetCityFromName(_landedOnCity);

            // Log
            summoningModule.ModuleLog(moduleId, "Successfully moved to {0} in a total time of {1} seconds or {2}. Accounting for Next Plane to Arrive, current Timer is now {3} seconds or {4}",
                _landedOnCity, _timerToAdd, GetReadableHourNotationFromTime(_timerToAdd), currentTimer, GetReadableHourNotationFromTime(currentTimer));
        }


        // Save Starting City
        startingCityName = currentCity.cityName;
        finalTimerToSolve = currentTimer;

        summoningModule.ModuleLog(moduleId, "You have {0} seconds or {1} to go from City {2} to City {3}",
            finalTimerToSolve, GetReadableHourNotationFromTime(finalTimerToSolve), startingCityName, targetCityName);

        // Show on Plate
        MarkDataOnPlateText();
    }

    void GenerateVoidedCities()
    {
        char[] letters = bombInfo.GetSerialNumberLetters().ToArray();

        foreach (char _letter in letters)
        {
            // Add the index of the Letter, we don't care about repeats
            voidedCellsIndices.Add(Array.IndexOf(alphabet, _letter.ToString().ToUpper()));

            // Log
            summoningModule.ModuleLog(moduleId, "Added {0} as a Voided City", alphabet[voidedCellsIndices.Last()]);
        }
    }

    void MarkDataOnPlateText()
    {
        citiesInformationText.text = startingCityName + "   " + targetCityName;
        timerInformationText.text = currentTimer.ToString();
    }
    

    /// <summary> Returns the string of the new City, and the travel time; because multiple travels can happen due to Void </summary>
    string TryMoveToCity(string flightCityTargetName, City departCity, ref int totalTravelTime)
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

                // Otherwise, Move!
                summoningModule.ModuleLog(moduleId, "Landed in city {0}, but since it is Voided, continue moving forward!", _landedOnCity.cityName);


                // Save the new City that gets returned up the chain of Voided Flights
                finalCity = TryMoveToCity(_connectedFlight.otherConnectedCityName, _landedOnCity, ref totalTravelTime);
            }

        }

        // Return the city we landed on at the very end
        return finalCity;
    }


    int GetFlightDuration(FlightDurationType flightDurationType)
    {
        int _value = 0;

        AllFlightDurations.TryGetValue(flightDurationType, out _value);

        return _value;
    }

    void ComputeAllFlightDurations()
    {
        string _debug = "All Flight Durations are:\n";
        int _duration = 0;
        int edgeworkOne, edgeworkTwo;

        AllFlightDurations = new Dictionary<FlightDurationType, int>();


        // Circle => 00:35:20 per Battery => 2120

        edgeworkOne = bombInfo.GetBatteryCount();
        _duration = 2120 * Mathf.Max(edgeworkOne, 1);

        AllFlightDurations.Add(FlightDurationType.Circle, _duration);

        _debug += "Circle: " + _duration + " seconds, or " + GetReadableHourNotationFromTime(_duration) + " because of the " + edgeworkOne + " Batteries.\n";




        // Square => 00:50:45 per Indicator or Port Plate => 3045

        edgeworkOne = bombInfo.GetIndicators().Count();
        edgeworkTwo = bombInfo.GetPortPlateCount();
        _duration = 3045 * Mathf.Max(edgeworkOne + edgeworkTwo, 1);

        AllFlightDurations.Add(FlightDurationType.Square, _duration);

        _debug += "Square: " + _duration + " seconds, or " + GetReadableHourNotationFromTime(_duration) + " because of the " + edgeworkOne + " Indicators and "+ edgeworkTwo + " Port Plates.\n";



        // Triangle => 01:15:30 per Port => 4530

        edgeworkOne = bombInfo.GetPortCount();
        _duration = 4530 * Mathf.Max(edgeworkOne, 1);

        AllFlightDurations.Add(FlightDurationType.Triangle, _duration);

        _debug += "Triangle: " + _duration + " seconds, or " + GetReadableHourNotationFromTime(_duration) + " because of the " + edgeworkOne + " Ports.\n";



        // Star => 3:45:05 flat => 13505
        _duration = 13505;

        AllFlightDurations.Add(FlightDurationType.Star, _duration);

        _debug += "Star: " + _duration + " seconds, or " + GetReadableHourNotationFromTime(_duration) + "\n";


        // Log
        summoningModule.ModuleLog(moduleId, _debug);
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

    void VerifyDataIntegrity()
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

    protected override void CasingTextButtonGetsPressed() { }

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

        if (commandParts.Length <= 1)
        {
            yield return "sendtochat {0} you must format the submission with “!{1} Submit .-. -.-- -.. ...- -.-.”";
            yield break;
        }

        if (commandParts[0] != "submit" && commandParts[0] != "s")
        {
            yield return "sendtochat {0} please make sure you Submit with either “Submit” or “s”.";
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

}
