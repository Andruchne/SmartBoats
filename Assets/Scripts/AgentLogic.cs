﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// This struct helps to order the directions an Agent can take based on its utility.
/// Every Direction (a vector to where the Agent would move) has a utility value.
/// Higher utility values are expected to lead to better outcomes.
/// </summary>
struct AgentDirection : IComparable
{
    public Vector3 Direction { get; }
    public float utility;

    public AgentDirection(Vector3 direction, float utility)
    {
        Direction = direction;
        this.utility = utility;
    }
    
    /// <summary>
    /// Notices that this method is an "inverse" sorting. It makes the higher values on top of the Sort, instead of
    /// the smaller values. For the smaller values, the return line would be utility.CompareTo(otherAgent.utility).
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public int CompareTo(object obj)
    {
        if (obj == null) return 1;
        
        AgentDirection otherAgent = (AgentDirection)obj;
        return otherAgent.utility.CompareTo(utility);
    }
}

/// <summary>
/// This struct stores all genes / weights from an Agent.
/// It is used to pass this information along to other Agents, instead of using the MonoBehavior itself.
/// Also, it makes it easier to inspect since it is a Serializable struct.
/// </summary>
[Serializable]
public struct AgentData
{
    public int steps;
    public int rayRadius;
    public float sight;
    public float movingSpeed;
    public Vector2 randomDirectionValue;
    public float boxWeight;
    public float distanceFactor;
    public float boatWeight;
    public float boatDistanceFactor;
    public float enemyWeight;
    public float enemyDistanceFactor;
    public float navyWeight;
    public float navyDistanceFactor;

    // Point where agent can save points...
    public Vector3 checkpoint;
    public float checkpointWeight;
    public float checkpointDistanceFactor;
    public float pointsWeight;

    // For variation 2 of the environment...
    public bool searchForCheckpoint;

    public AgentData(int steps, int rayRadius, float sight, float movingSpeed, Vector2 randomDirectionValue, float boxWeight,
        float distanceFactor, float boatWeight, float boatDistanceFactor, float enemyWeight, float enemyDistanceFactor,
        float navyWeight, float navyDistanceFactor, Vector3 checkpoint, float checkpointWeight, float checkpointDistanceFactor,
        float pointsWeight, bool searchForCheckpoint)
    {
        this.steps = steps;
        this.rayRadius = rayRadius;
        this.sight = sight;
        this.movingSpeed = movingSpeed;
        this.randomDirectionValue = randomDirectionValue;
        this.boxWeight = boxWeight;
        this.distanceFactor = distanceFactor;
        this.boatWeight = boatWeight;
        this.boatDistanceFactor = boatDistanceFactor;
        this.enemyWeight = enemyWeight;
        this.enemyDistanceFactor = enemyDistanceFactor;
        this.navyWeight = navyWeight;
        this.navyDistanceFactor = navyDistanceFactor;

        this.checkpoint = checkpoint;
        this.checkpointWeight = checkpointWeight;
        this.checkpointDistanceFactor = checkpointDistanceFactor;
        this.pointsWeight = pointsWeight;

        this.searchForCheckpoint = searchForCheckpoint;
    }
}

/// <summary>
/// Main script for the Agent behaviour.
/// It is responsible for caring its genes, deciding its actions and controlling debug properties.
/// The agent moves by using its rigidBody velocity. The velocity is set to its speed times the movementDirection.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AgentLogic : MonoBehaviour, IComparable
{
    public bool show;

    private Vector3 _movingDirection;
    private Rigidbody _rigidbody;
    
    [SerializeField]
    protected float pointsGathered;
    [SerializeField]
    protected float pointsSaved;

    // This bool allows for agents to be compared with gathered points, instead of saved points
    // In case no actor is able to save any points, the one with the most gathered ones will win
    public bool pittyPoints;

    private bool _isAwake;

    [Header("Genes")]
    [SerializeField, Tooltip("Steps for the area of sight.")]
    private int steps;
    [SerializeField, Range(0.0f, 360.0f), Tooltip("Divides the 360˚ view of the Agent into rayRadius steps.")]
    private int rayRadius = 16;
    [SerializeField, Tooltip("Ray distance. For the front ray, the value of 1.5 * Sight is used.")]
    private float sight = 10.0f;
    [SerializeField]
    private float movingSpeed;
    [SerializeField, Tooltip("All directions starts with a random value from X-Y (Math.Abs, Math.Min and Math.Max are applied).")]
    private Vector2 randomDirectionValue;

    [Space(10)]
    [Header("Weights")]
    [SerializeField]
    private float boxWeight;
    [SerializeField]
    private float distanceFactor;
    [SerializeField]
    private float boatWeight;
    [SerializeField]
    private float boatDistanceFactor;
    [SerializeField]
    private float enemyWeight;
    [SerializeField]
    private float enemyDistanceFactor;
    [SerializeField]
    private float navyWeight;
    [SerializeField]
    private float navyDistanceFactor;
    [SerializeField]
    private Vector3 checkpoint;
    [SerializeField]
    private float checkpointWeight;
    [SerializeField]
    private float checkpointDistanceFactor;
    [SerializeField]
    private float pointsWeight;

    // For variation 2 of the environment...
    [Space(10)]
    [SerializeField]
    protected bool capCheckpointAccess;
    [SerializeField]
    protected int minPointsAmount;

    [Space(10)]
    [Header("Debug & Help")] 
    [SerializeField]
    private Color visionColor;
    [SerializeField]
    private Color foundColor;
    [SerializeField]
    private Color directionColor;
    [SerializeField, Tooltip("Shows visualization rays.")] 
    private bool debug;

    #region Static Variables
    private static float _minimalSteps = 1.0f;
    private static float _minimalRayRadius = 1.0f;
    private static float _minimalSight = 0.1f;
    private static float _minimalMovingSpeed = 1.0f;
    private static float _speedInfluenceInSight = 0.1250f;
    private static float _sightInfluenceInSpeed = 0.0625f;
    private static float _maxUtilityChoiceChance = 0.85f;
    #endregion
    
    private void Awake()
    {
        Initiate();
    }

    /// <summary>
    /// Initiate the values for this Agent, settings its points to 0 and recalculating its sight parameters.
    /// </summary>
    private void Initiate()
    {
        pointsGathered = 0;
        pointsSaved = 0;
        steps = 360 / rayRadius;
        _rigidbody = GetComponent<Rigidbody>();
    }
    
    /// <summary>
    /// Copies the genes / weights from the parent.
    /// </summary>
    /// <param name="parent"></param>
    public void Birth(AgentData parent)
    {
        steps = parent.steps;
        rayRadius = parent.rayRadius;
        sight = parent.sight;
        movingSpeed = parent.movingSpeed;
        randomDirectionValue = parent.randomDirectionValue;
        boxWeight = parent.boxWeight;
        distanceFactor = parent.distanceFactor;
        boatWeight = parent.boatWeight;
        boatDistanceFactor = parent.boatDistanceFactor;
        enemyWeight = parent.enemyWeight;
        enemyDistanceFactor = parent.enemyDistanceFactor;
        navyWeight = parent.navyWeight;
        navyDistanceFactor = parent.navyDistanceFactor;
        checkpoint = parent.checkpoint;
    }

    /// <summary>
    /// Has a mutationChance ([0%, 100%]) of causing a mutationFactor [-mutationFactor, +mutationFactor] to each gene / weight.
    /// The chance of mutation is calculated per gene / weight.
    /// </summary>
    /// <param name="mutationFactor">How much a gene / weight can change (-mutationFactor, +mutationFactor)</param>
    /// <param name="mutationChance">Chance of a mutation happening per gene / weight.</param>
    public void Mutate(float mutationFactor, float mutationChance)
    {
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            steps += (int) Random.Range(-mutationFactor, +mutationFactor);
            steps = (int) Mathf.Max(steps, _minimalSteps);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            rayRadius += (int) Random.Range(-mutationFactor, +mutationFactor);
            rayRadius = (int) Mathf.Max(rayRadius, _minimalRayRadius);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            float sightIncrease = Random.Range(-mutationFactor, +mutationFactor);
            sight += sightIncrease;
            sight = Mathf.Max(sight, _minimalSight);
            if (sightIncrease > 0.0f)
            {
                movingSpeed -= sightIncrease * _sightInfluenceInSpeed;
                movingSpeed = Mathf.Max(movingSpeed, _minimalMovingSpeed);    
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            float movingSpeedIncrease = Random.Range(-mutationFactor, +mutationFactor);
            movingSpeed += movingSpeedIncrease;
            movingSpeed = Mathf.Max(movingSpeed, _minimalMovingSpeed);
            if (movingSpeedIncrease > 0.0f)
            {
                sight -= movingSpeedIncrease * _speedInfluenceInSight;
                sight = Mathf.Max(sight, _minimalSight);    
            }
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            randomDirectionValue.x += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            randomDirectionValue.y += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boxWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            distanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boatWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            boatDistanceFactor +=  Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            enemyWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            enemyDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            navyWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            navyDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            checkpointDistanceFactor += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            checkpointWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
        if (Random.Range(0.0f, 100.0f) <= mutationChance)
        {
            pointsWeight += Random.Range(-mutationFactor, +mutationFactor);
        }
    }

    private void Update()
    {
        if (_isAwake)
        {
            Act();    
        }
    }

    /// <summary>
    /// Calculate the best direction to move using the Agent properties.
    /// The agent shoots a ray in a area on front of itself and calculates the utility of each one of them based on what
    /// it did intersect or using a random value (uses a Random from [randomDirectionValue.x, randomDirectionValue.y]).
    /// 
    /// </summary>
    private void Act()
    {
        Transform selfTransform = transform;
        Vector3 forward = selfTransform.forward;
        //Ignores the y component to avoid flying/sinking Agents.
        forward.y = 0.0f;
        forward.Normalize();
        Vector3 selfPosition = selfTransform.position;

        //Initiate the rayDirection on the opposite side of the spectrum.
        Vector3 rayDirection = Quaternion.Euler(0, -1.0f * steps * (rayRadius / 2.0f), 0) * forward;

        //List of AgentDirection (direction + utility) for all the directions. (Raycasts)
        
        List<AgentDirection> directions = new List<AgentDirection>();
        for (int i = 0; i <= rayRadius; i++)
        {
            //Add the new calculatedAgentDirection looking at the rayDirection.
            directions.Add(CalculateAgentDirection(selfPosition, rayDirection));
            
            //Rotate the rayDirection by _steps every iteration through the entire rayRadius.
            rayDirection = Quaternion.Euler(0, steps, 0) * rayDirection;
        }
        

        //Adds an extra direction for the front view with a extra range.
        //directions.Add(CalculateAgentDirection(selfPosition, forward, 1.5f));

        if (!capCheckpointAccess || (capCheckpointAccess && pointsGathered >= minPointsAmount))
        {
            //Add the checkpoint to the directions for continuous check.
            Vector3 checkpointDirection = checkpoint - selfTransform.position;

            float lerpParam = Mathf.InverseLerp(300, 0, checkpointDirection.magnitude);
            float distanceIndex = Mathf.Lerp(0.1f, 1, lerpParam);

            float checkpointUtility = (distanceIndex * checkpointDistanceFactor + checkpointWeight) * (pointsGathered * pointsWeight);

            // Check if you have no need for the checkpoint (mainly for navy)
            if (checkpointUtility > 0)
            {
                AgentDirection checkpointAgentDir = new AgentDirection(checkpointDirection.normalized, checkpointDistanceFactor);
                checkpointAgentDir.utility = checkpointUtility;
                directions.Add(checkpointAgentDir);
            }
        }

        directions.Sort();
        //There is a (100 - _maxUtilityChoiceChance) chance of using the second best option instead of the highest one. Should help into ambiguous situation.
        int directionChoice = 0;
        float randomChoice = Random.Range(0.0f, 100.0f);
        if (randomChoice <= _maxUtilityChoiceChance)
        {
            directionChoice = 0;
        }
        else if (randomChoice > _maxUtilityChoiceChance && directions[0].utility - directions[1].utility < 2)
        {
            directionChoice = 1;
        }

        AgentDirection highestAgentDirection = directions[directionChoice];
        
        //Rotate towards direction. The factor of 0.1 helps to create a "rotation" animation instead of automatically rotates towards the target. 
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(highestAgentDirection.Direction), 0.1f);
        
        //Sets the velocity using the chosen direction
        _rigidbody.velocity = highestAgentDirection.Direction * movingSpeed;
        
        if (debug)
        {
            Debug.DrawRay(selfPosition, highestAgentDirection.Direction * (sight * 1.5f), directionColor);
        }
    }

    private AgentDirection CalculateAgentDirection(Vector3 selfPosition, Vector3 rayDirection, float sightFactor = 1.0f)
    {
        if (debug)
        {
            Debug.DrawRay(selfPosition, rayDirection * sight, visionColor);
        }

        //Calculate a random utility to initiate the AgentDirection.
        float utility = Random.Range(Mathf.Min(randomDirectionValue.x, randomDirectionValue.y), Mathf.Max(randomDirectionValue.x, randomDirectionValue.y));

        //Create an AgentDirection struct with a random utility value [utility]. Ignores y component.
        AgentDirection direction = new AgentDirection(new Vector3(rayDirection.x, 0.0f, rayDirection.z), utility);
        
        //Raycast into the rayDirection to check if something can be seen in that direction.
        //The sightFactor is a variable that increases / decreases the size of the ray.
        //For now, the sightFactor is only used to control the long sight in front of the agent.

        if (Physics.Raycast(selfPosition, rayDirection, out RaycastHit raycastHit, sight * sightFactor))
        {
            if (debug)
            {
                Debug.DrawLine(selfPosition, raycastHit.point, foundColor);
            }
            
            //Calculate the normalized distance from the agent to the intersected object.
            //Closer objects will have distancedNormalized close to 0, and further objects will have it close to 1.
            float distanceNormalized = (raycastHit.distance / (sight * sightFactor));
            
            //Inverts the distanceNormalized. Closer objects will tend to 1, while further objects will tend to 0.
            //Thus, closer objects will have a higher value.
            float distanceIndex = 1.0f - distanceNormalized;

            string tag = raycastHit.collider.gameObject.tag;

            //Calculate the utility of the found object according to its type.
            switch (tag)
            {
                //All formulas are the same. Only the weights change.
                case "Box":
                    utility = distanceIndex * distanceFactor + boxWeight;
                    break;
                case "Boat":
                    utility = distanceIndex * boatDistanceFactor + boatWeight;
                    break;
                case "Enemy":
                    utility = distanceIndex * enemyDistanceFactor + enemyWeight;
                    break;
            }
        }
        
        direction.utility = utility;
        return direction;
    }

    public string GetInfoString()
    {
        string info = "";

        if (pointsSaved == 0) { info += "Pitty Points" + "\n"; }
        info += "Final Points: " + pointsSaved + "\n\n";
        info += "Steps: " + steps + "\n";
        info += "Ray Radius: " + rayRadius + "\n";
        info += "Sight: " + sight + "\n";
        info += "Moving Speed: " + movingSpeed + "\n";
        info += "Random Direction Values: " + randomDirectionValue + "\n\n";
        info += "Box Weight: " + boxWeight + "\n";
        info += "Box Distance Factor: " + distanceFactor + "\n\n";
        info += "Boat Weight: " + boatWeight + "\n";
        info += "Boat Distance Factor: " + boatDistanceFactor + "\n\n";
        info += "Enemy Weight: " + enemyWeight + "\n";
        info += "Enemy Distance Factor: " + enemyDistanceFactor + "\n\n";
        info += "Navy Weight: " + navyWeight + "\n";
        info += "Navy Distance Factor: " + navyDistanceFactor + "\n\n";
        info += "Checkpoint Weight: " + checkpointWeight + "\n";
        info += "Checkpoint Distance Factor: " + checkpointDistanceFactor + "\n";
        info += "Gathered Points Weight: " + pointsWeight + "\n";

        return info;
    }

    /// <summary>
    /// Activates the agent update method.
    /// Does nothing if the agent is already awake.
    /// </summary>
    public void AwakeUp()
    {
        _isAwake = true;
    }

    /// <summary>
    /// Stops the agent update method and sets its velocity to zero.
    /// Does nothing if the agent is already sleeping.
    /// </summary>
    public void Sleep()
    {
        _isAwake = false;
        _rigidbody.velocity = Vector3.zero;
    }

    public float GetPoints()
    {
        if (pittyPoints) { return pointsGathered; }

        return pointsSaved;
    }
    
    /// <summary>
    /// Compares the points of two agents. When used on Sort function will make the highest points to be on top.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public int CompareTo(object obj) {
        if (obj == null) return 1;
        
        AgentLogic otherAgent = obj as AgentLogic;
        if (otherAgent != null)
        {
            return otherAgent.GetPoints().CompareTo(GetPoints());
        } 
        else
        {
            throw new ArgumentException("Object is not an AgentLogic");
        }
    }

    /// <summary>
    /// Returns the AgentData of this Agent.
    /// </summary>
    /// <returns></returns>
    public AgentData GetData()
    {
        return new AgentData(steps, rayRadius, sight, movingSpeed, randomDirectionValue, boxWeight,
            distanceFactor, boatWeight, boatDistanceFactor, enemyWeight,  enemyDistanceFactor, 
            navyWeight, navyDistanceFactor, checkpoint, checkpointWeight, checkpointDistanceFactor,
            pointsWeight, capCheckpointAccess);
    }
}
