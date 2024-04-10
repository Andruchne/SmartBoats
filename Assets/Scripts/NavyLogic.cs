using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NavyLogic : AgentLogic
{
    #region Static Variables
    private static float _boxPoints = 0.1f;
    private static float _piratePoints = 5.0f;
    #endregion

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
            pointsGathered = 0;
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag.Equals("Enemy"))
        {
            pointsGathered += _piratePoints;
            Destroy(other.gameObject);
        }
    }

}