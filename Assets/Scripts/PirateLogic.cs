using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(Rigidbody))]
public class PirateLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 0.1f;
    private static float _boatPoints = 5.0f;
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag.Equals("Box"))
        {
            pointsGathered += _boxPoints;
            Destroy(other.gameObject);
        }
        else if ((other.gameObject.tag.Equals("PiratePoint") && !capCheckpointAccess) || 
            (capCheckpointAccess && pointsGathered >= minPointsAmount))
        {
            // Checkpoint reached...
            pointsSaved += pointsGathered;
            gameObject.SetActive(false);
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if(other.gameObject.tag.Equals("Boat"))
        {
            pointsGathered += _boatPoints;
            Destroy(other.gameObject);
        }
    }

}
