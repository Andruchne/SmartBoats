﻿using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class BoatLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 2.0f;
    private static float _piratePoints = -100.0f;
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag.Equals("Box"))
        {
            pointsGathered += _boxPoints;
            Destroy(other.gameObject);
        }
        else if((other.gameObject.tag.Equals("BoatPoint") && !capCheckpointAccess) ||
            (capCheckpointAccess && pointsGathered >= minPointsAmount))
        {
            // Checkpoint reached...
            pointsSaved += pointsGathered;
            gameObject.SetActive(false);
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.tag.Equals("Enemy"))
        {
            //This is a safe-fail mechanism. In case something goes wrong and the Boat is not destroyed after touching
            //a pirate, it also gets a massive negative number of points.
            pointsGathered += _piratePoints;
        }
    }
}
