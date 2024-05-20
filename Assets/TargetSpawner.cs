using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    public GameObject targetPrefab;
    public Transform spawnLocation;

    void Start()
    {
        BeatDetector beatDetector = FindObjectOfType<BeatDetector>();
        beatDetector.onBassBeat.AddListener(SpawnTarget);
    }

    void SpawnTarget()
    {
        Debug.Log("Spawning target at time: " + Time.time);
        Instantiate(targetPrefab, spawnLocation.position, spawnLocation.rotation);
    }
}