using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleBeatDetection : MonoBehaviour
{
    public AudioSource audioSource;
    public GameObject targetPrefab;
    public BoxCollider spawnArea;
    public float spawnCooldown = 1f;
    public float targetLifetime = 3f;
    public int maxSelfDestructs = 3;

    public delegate void OnBeatHandler();
    public event OnBeatHandler OnBeat;

    private float[] samples0Channel;
    private float[] samples1Channel;
    private float[] historyBuffer;

    public int bufferSize = 1024;
    public FFTWindow FFTWindow = FFTWindow.BlackmanHarris;

    private List<GameObject> activeTargets = new List<GameObject>();
    private int selfDestructCount = 0;
    private float lastSpawnTime;

    void Start()
    {
        samples0Channel = new float[bufferSize];
        samples1Channel = new float[bufferSize];
        historyBuffer = new float[43];
        OnBeat += HandleOnBeat;
    }

    void Update()
    {
        float instantEnergy = GetInstantEnergy();
        float localAverageEnergy = GetLocalAverageEnergy();
        float varianceEnergies = ComputeVariance(localAverageEnergy);
        double constantC = (-0.0025714 * varianceEnergies) + 1.5142857;

        float[] shiftedHistoryBuffer = ShiftArray(historyBuffer, 1);
        shiftedHistoryBuffer[0] = instantEnergy;
        OverrideElementsToAnotherArray(shiftedHistoryBuffer, historyBuffer);

        if (instantEnergy > constantC * localAverageEnergy)
        {
            OnBeat?.Invoke();
        }
    }

    private void HandleOnBeat()
    {
        Debug.Log("Beat detected!");

        if (Time.time > lastSpawnTime + spawnCooldown)
        {
            SpawnTarget();
            lastSpawnTime = Time.time;
        }
    }

    void SpawnTarget()
    {
        if (targetPrefab && spawnArea)
        {
            Vector3 randomPosition = GetRandomPositionInArea();
            Debug.Log("Attempting to spawn target at position: " + randomPosition);

            if (!IsPositionOccupied(randomPosition))
            {
                GameObject target = Instantiate(targetPrefab, randomPosition, Quaternion.identity);
                activeTargets.Add(target);
                StartCoroutine(DestroyTargetAfterTime(target, targetLifetime));
                Debug.Log("Target spawned at position: " + randomPosition);
            }
            else
            {
                Debug.Log("Position occupied, cannot spawn target at position: " + randomPosition);
            }
        }
        else
        {
            Debug.LogWarning("Target prefab or spawn area not set.");
        }
    }

    Vector3 GetRandomPositionInArea()
    {
        Bounds bounds = spawnArea.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        return new Vector3(x, y, z);
    }

    bool IsPositionOccupied(Vector3 position)
    {
        activeTargets.RemoveAll(target => target == null);
        foreach (GameObject target in activeTargets)
        {
            if (Vector3.Distance(target.transform.position, position) < 1f)
            {
                return true;
            }
        }
        return false;
    }

    IEnumerator DestroyTargetAfterTime(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (target != null)
        {
            activeTargets.Remove(target);
            Destroy(target);
            selfDestructCount++;
            if (selfDestructCount >= maxSelfDestructs)
            {
                GameOver();
            }
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over!");
    }

    #region FOR_SIMPLE_ALGORITHM_USE
    public float GetInstantEnergy()
    {
        float result = 0;
        audioSource.GetSpectrumData(samples0Channel, 0, FFTWindow);
        audioSource.GetSpectrumData(samples1Channel, 1, FFTWindow);

        for (int i = 0; i < bufferSize; i++)
        {
            result += Mathf.Pow(samples0Channel[i], 2) + Mathf.Pow(samples1Channel[i], 2);
        }

        return result;
    }

    private float GetLocalAverageEnergy()
    {
        float result = 0;

        for (int i = 0; i < historyBuffer.Length; i++)
        {
            result += historyBuffer[i];
        }

        return result / historyBuffer.Length;
    }

    private float ComputeVariance(float averageEnergy)
    {
        float result = 0;

        for (int i = 0; i < historyBuffer.Length; i++)
        {
            result += Mathf.Pow(historyBuffer[i] - averageEnergy, 2);
        }

        return result / historyBuffer.Length;
    }
    #endregion

    #region UTILITY_USE
    private void OverrideElementsToAnotherArray(float[] from, float[] to)
    {
        for (int i = 0; i < from.Length; i++)
        {
            to[i] = from[i];
        }
    }

    private float[] ShiftArray(float[] array, int amount)
    {
        float[] result = new float[array.Length];
        for (int i = 0; i < array.Length - amount; i++)
        {
            result[i + amount] = array[i];
        }
        return result;
    }
    #endregion
}
