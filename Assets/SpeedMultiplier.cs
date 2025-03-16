using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedMultiplier : MonoBehaviour
{
    [Range(0, 20)]
    [SerializeField] float speedMultiplier = 1.0f;

    private void OnValidate()
    {
        Time.timeScale = speedMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }
}
