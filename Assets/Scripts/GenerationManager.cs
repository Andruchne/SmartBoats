using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class GenerationManager : MonoBehaviour
{
    // Decide if to create new generations for specific agents...
    [SerializeField]
    private bool evolveBoat = true;
    [SerializeField]
    private bool evolvePirate = true;
    [SerializeField]
    private bool evolveNavy = true;
    

    [Header("Generators")]
    [SerializeField]
    private GenerateObjectsInArea[] boxGenerators;
    [SerializeField]
    private GenerateObjectsInArea boatGenerator;
    [SerializeField]
    private GenerateObjectsInArea pirateGenerator;
    [SerializeField]
    private GenerateObjectsInArea navyGenerator;

    [Space(10)]
    [Header("Parenting and Mutation")]
    [SerializeField]
    private float mutationFactor;
    [SerializeField] 
    private float mutationChance;
    [SerializeField] 
    private int boatParentSize;
    [SerializeField] 
    private int pirateParentSize;
    [SerializeField]
    private int navyParentSize;

    [Space(10)] 
    [Header("Simulation Controls")]
    [SerializeField, Tooltip("Time per simulation (in seconds).")]
    private float simulationTimer;
    [SerializeField, Tooltip("Current time spent on this simulation.")]
    private float simulationCount;
    [SerializeField, Tooltip("Automatically starts the simulation on Play.")]
    private bool runOnStart;
    [SerializeField, Tooltip("Initial count for the simulation. Used for the Prefabs naming.")]
    private int generationCount;

    [Space(10)] 
    [Header("Prefab Saving")]
    [SerializeField]
    private string savePrefabsAt;
    
    /// <summary>
    /// Those variables are used mostly for debugging in the inspector.
    /// </summary>
    [Header("Former winners")]
    [SerializeField]
    private AgentData lastBoatWinnerData;
    [SerializeField]
    private AgentData lastPirateWinnerData;
    [SerializeField]
    private AgentData lastNavyWinnerData;

    private bool _runningSimulation;
    private List<BoatLogic> _activeBoats;
    private List<PirateLogic> _activePirates;
    private List<NavyLogic> _activeNavy;
    private BoatLogic[] _boatParents;
    private PirateLogic[] _pirateParents;
    private NavyLogic[] _navyParents;

    private void Awake()
    {
        Random.InitState(6);
    }

    private void Start()
    {
        if (runOnStart)
        {
            StartSimulation();
        }
    }
    
    private void Update()
    {
        if (_runningSimulation)
        {
            //Creates a new generation.
            if (simulationCount >= simulationTimer)
            {
                ++generationCount;
                MakeNewGeneration();
                simulationCount = -Time.deltaTime;
            } 
            simulationCount += Time.deltaTime;
        }
    }

    private void SetDocumentation(string textfile, string title, string info)
    {
        string path = Path.Combine(Application.dataPath, "Docs");

        // Create directory if it doesn't exist
        if (!Directory.Exists(path)) { Directory.CreateDirectory(path); }

        path = Path.Combine(path, textfile);

        // Create file if it doesn't exist
        if (!File.Exists(path))
        {
            using (StreamWriter writer = new StreamWriter(path)) 
            {
                writer.WriteLine(textfile + "\n\n\n");
            }
        }

        // Add info
        using (StreamWriter writer = new StreamWriter(path, true))
        {
            writer.WriteLine("______________________________");
            writer.WriteLine(title + ":\n" + info + "\n\n\n");
        }
    }


    /// <summary>
    /// Generates the boxes on all box areas.
    /// </summary>
    public void GenerateBoxes()
    {
        foreach (GenerateObjectsInArea generateObjectsInArea in boxGenerators)
        {
            generateObjectsInArea.RegenerateObjects();
        }
    }
    
     /// <summary>
     /// Generates boats and pirates using the parents list.
     /// If no parents are used, then they are ignored and the boats/pirates are generated using the default prefab
     /// specified in their areas.
     /// </summary>
     /// <param name="boatParents"></param>
     /// <param name="pirateParents"></param>
    public void GenerateObjects(BoatLogic[] boatParents = null, PirateLogic[] pirateParents = null, NavyLogic[] navyParents = null)
    { 
        GenerateBoats(boatParents);
        GeneratePirates(pirateParents);
        GenerateNavy(navyParents);
    }

     /// <summary>
     /// Generates the list of pirates using the parents list. The parent list can be null and, if so, it will be ignored.
     /// Newly created pirates will go under mutation (MutationChances and MutationFactor will be applied).
     /// Newly create agents will be Awaken (calling AwakeUp()).
     /// </summary>
     /// <param name="pirateParents"></param>
    private void GeneratePirates(PirateLogic[] pirateParents)
    {
        _activePirates = new List<PirateLogic>();
        List<GameObject> objects = pirateGenerator.RegenerateObjects();

        foreach (GameObject obj in objects)
        {
            PirateLogic pirate = obj.GetComponent<PirateLogic>();
            if (pirate != null)
            {
                _activePirates.Add(pirate);
                if (pirateParents != null)
                {
                    PirateLogic pirateParent = pirateParents[Random.Range(0, pirateParents.Length)];
                    pirate.Birth(pirateParent.GetData());
                }

                pirate.Mutate(mutationFactor, mutationChance);
                pirate.AwakeUp();
            }
        }
    }

     /// <summary>
     /// Generates the list of boats using the parents list. The parent list can be null and, if so, it will be ignored.
     /// Newly created boats will go under mutation (MutationChances and MutationFactor will be applied).
     /// /// Newly create agents will be Awaken (calling AwakeUp()).
     /// </summary>
     /// <param name="boatParents"></param>
    private void GenerateBoats(BoatLogic[] boatParents)
    {
        _activeBoats = new List<BoatLogic>();
        List<GameObject> objects = boatGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            BoatLogic boat = obj.GetComponent<BoatLogic>();
            if (boat != null)
            {
                _activeBoats.Add(boat);
                if (boatParents != null)
                {
                    BoatLogic boatParent = boatParents[Random.Range(0, boatParents.Length)];
                    boat.Birth(boatParent.GetData());
                }

                boat.Mutate(mutationFactor, mutationChance);
                boat.AwakeUp();
            }
        }
    }



    /// <summary>
    /// Generates the list of navys using the parents list. The parent list can be null and, if so, it will be ignored.
    /// Newly created navys will go under mutation (MutationChances and MutationFactor will be applied).
    /// Newly create agents will be Awaken (calling AwakeUp()).
    /// </summary>
    /// <param name="navyParents"></param>
    private void GenerateNavy(NavyLogic[] navyParents)
    {
        _activeNavy = new List<NavyLogic>();
        List<GameObject> objects = navyGenerator.RegenerateObjects();
        foreach (GameObject obj in objects)
        {
            NavyLogic navy = obj.GetComponent<NavyLogic>();
            if (navy != null)
            {
                _activeNavy.Add(navy);
                if (navyParents != null)
                {
                    NavyLogic navyParent = navyParents[Random.Range(0, navyParents.Length)];
                    navy.Birth(navyParent.GetData());
                }

                navy.Mutate(mutationFactor, mutationChance);
                navy.AwakeUp();
            }
        }
    }



    /// <summary>
    /// Creates a new generation by using GenerateBoxes and GenerateBoats/Pirates.
    /// Previous generations will be removed and the best parents will be selected and used to create the new generation.
    /// The best parents (top 1) of the generation will be stored as a Prefab in the [savePrefabsAt] folder. Their name
    /// will use the [generationCount] as an identifier.
    /// </summary>
    public void MakeNewGeneration()
    {
        Random.InitState(6);

        GenerateBoxes();

        BoatLogic lastBoatWinner = null;
        PirateLogic lastPirateWinner = null;
        NavyLogic lastNavyWinner = null;

        string winnerMessage = "";

        if (evolveBoat)
        {
            //Fetch parents
            _activeBoats.RemoveAll(item => item == null);

            bool foundWinner = false;
            // Check if any of the actors in the list has saved points
            for (int i = 0; i < _activeBoats.Count; i++)
            {
                if (_activeBoats[i].GetPoints() > 0) { foundWinner = true; }
            }

            // If not, set pittyPoints to true, making gatheredPoints be compared instead
            if (!foundWinner)
            {
                for (int i = 0; i < _activeBoats.Count; i++)
                {
                    _activeBoats[i].pittyPoints = true;
                }
            }

            _activeBoats.Sort();

            if (_activeBoats.Count > 0)
            {
                _boatParents = new BoatLogic[boatParentSize];
                for (int i = 0; i < boatParentSize; i++)
                {
                    _boatParents[i] = _activeBoats[0];
                }

                lastBoatWinner = _activeBoats[0];
                lastBoatWinner.name += "Gen-" + generationCount;
                lastBoatWinnerData = lastBoatWinner.GetData();
                PrefabUtility.SaveAsPrefabAsset(lastBoatWinner.gameObject, savePrefabsAt + lastBoatWinner.name + ".prefab");

                winnerMessage += $"Last winner boat had: {lastBoatWinner.GetPoints()} points!\n";
                SetDocumentation("BoatInfo", lastBoatWinner.name, lastBoatWinner.GetInfoString());
            }
        }
        
        if (evolvePirate)
        {
            _activePirates.RemoveAll(item => item == null);

            bool foundWinner = false;
            // Check if any of the actors in the list has saved points
            for (int i = 0; i < _activePirates.Count; i++)
            {
                if (_activePirates[i].GetPoints() > 0) { foundWinner = true; }
            }

            // If not, set pittyPoints to true, making gatheredPoints be compared instead
            if (!foundWinner)
            {
                for (int i = 0; i < _activePirates.Count; i++)
                {
                    _activePirates[i].pittyPoints = true;
                }
            }

            _activePirates.Sort();
            _pirateParents = new PirateLogic[pirateParentSize];
            if (_activePirates.Count > 0)
            {
                for (int i = 0; i < pirateParentSize; i++)
                {
                    _pirateParents[i] = _activePirates[0];
                }
                lastPirateWinner = _activePirates[0];
                lastPirateWinner.name += "Gen-" + generationCount;
                lastPirateWinnerData = lastPirateWinner.GetData();
                PrefabUtility.SaveAsPrefabAsset(lastPirateWinner.gameObject, savePrefabsAt + lastPirateWinner.name + ".prefab");

                winnerMessage += $"Last winner pirate had: {lastPirateWinner.GetPoints()}  points!\n";
                SetDocumentation("PirateInfo", lastPirateWinner.name, lastPirateWinner.GetInfoString());
            } 
        }

        if (evolveNavy)
        {
            _activeNavy.RemoveAll(item => item == null);

            // No need to do the check for a winner here, since points are always counted as "saved"

            _activeNavy.Sort();
            _navyParents = new NavyLogic[navyParentSize];
            if (_activeNavy.Count > 0)
            {
                for (int i = 0; i < navyParentSize; i++)
                {
                    _navyParents[i] = _activeNavy[0];
                }

                lastNavyWinner = _activeNavy[0];
                lastNavyWinner.name += "Gen-" + generationCount;
                lastNavyWinnerData = lastNavyWinner.GetData();
                PrefabUtility.SaveAsPrefabAsset(lastNavyWinner.gameObject, savePrefabsAt + lastNavyWinner.name + ".prefab");

                winnerMessage += $"Last winner navy had: {lastNavyWinner.GetPoints()} points!";
                SetDocumentation("NavyInfo", lastNavyWinner.name, lastNavyWinner.GetInfoString());
            }
        }

        if (winnerMessage != "")
        {
            Debug.Log(winnerMessage);
        }

        GenerateObjects(_boatParents, _pirateParents, _navyParents);
    }

     /// <summary>
     /// Starts a new simulation. It does not call MakeNewGeneration. It calls both GenerateBoxes and GenerateObjects and
     /// then sets the _runningSimulation flag to true.
     /// </summary>
    public void StartSimulation()
    {
        Random.InitState(6);

        GenerateBoxes();
        GenerateObjects();
        _runningSimulation = true;
    }

     /// <summary>
     /// Continues the simulation. It calls MakeNewGeneration to use the previous state of the simulation and continue it.
     /// It sets the _runningSimulation flag to true.
     /// </summary>
     public void ContinueSimulation()
     {
         MakeNewGeneration();
         _runningSimulation = true;
     }
     
     /// <summary>
     /// Stops the count for the simulation. It also removes null (Destroyed) boats from the _activeBoats list and sets
     /// all boats and pirates to Sleep.
     /// </summary>
    public void StopSimulation()
    {
        _runningSimulation = false;
        _activeBoats.RemoveAll(item => item == null);
        _activeBoats.ForEach(boat => boat.Sleep());
        _activePirates.ForEach(pirate => pirate.Sleep());
        _activeNavy.ForEach(navy => navy.Sleep());
    }
}
