using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NavyLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 0.1f;
    private static float _piratePoints = 5.0f;
    #endregion

    /*
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag.Equals("Box"))
        {
            pointsGathered += _boxPoints;
            Destroy(other.gameObject);
        }
        else if (other.gameObject.tag.Equals("BoatPoint"))
        {
            // Checkpoint reached...
            pointsSaved += pointsGathered;
            gameObject.SetActive(false);
        }
    }
    */

    private void OnCollisionEnter(Collision other)
    {
        // Here we add the points to saved immediately, to encourage hunting pirates
        if (other.gameObject.tag.Equals("Enemy"))
        {
            pointsSaved += _piratePoints;

            // Make sure to not wipe out all the pirates (It causes issues with generating a new generation otherwise)
            PirateLogic[] pirates = FindObjectsOfType<PirateLogic>();
            if (pirates.Length == 1)
            {
                other.gameObject.SetActive(false);
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }

}