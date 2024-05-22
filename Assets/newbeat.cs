using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BeatDetector : MonoBehaviour
{
    public AudioSource audioSource;
    public GameObject targetPrefab;
    public BoxCollider spawnArea;
    public float spawnCooldown = 1f;
    public float targetLifetime = 3f;
    public int maxSelfDestructs = 3;

    public int bassLowerLimit = 60;
    public int bassUpperLimit = 180;
    public int lowLowerLimit = 500;
    public int lowUpperLimit = 2000;

    private int windowSize = 1024;
    private float samplingFrequency;

    private float[] spectrum;
    private float[] avgSpectrum;
    private float threshold;
    private List<GameObject> activeTargets = new List<GameObject>();
    private int selfDestructCount = 0;
    private float lastBeatTime;
    private float lastSpawnTime;
    private float timeBetweenBeats = 0.2f;
    private Queue<float> recentAmplitudes = new Queue<float>();

    void Start()
    {
        spectrum = new float[windowSize];
        avgSpectrum = new float[windowSize];
        samplingFrequency = AudioSettings.outputSampleRate;

        audioSource.Play();
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            GetSpectrumData();
            DetectBeats();
        }
    }

    void GetSpectrumData()
    {
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.Hamming);
    }

    void DetectBeats()
    {
        float lowFreqMax = 0;
        int lowFreqEnd = Mathf.FloorToInt((lowUpperLimit / samplingFrequency) * windowSize);

        for (int i = 0; i < lowFreqEnd; i++)
        {
            if (spectrum[i] > lowFreqMax)
            {
                lowFreqMax = spectrum[i];
            }
        }

        float amplitude = lowFreqMax;
        recentAmplitudes.Enqueue(amplitude);

        if (recentAmplitudes.Count > 100)
        {
            recentAmplitudes.Dequeue();
        }

        float averageAmplitude = recentAmplitudes.Average();
        float variance = recentAmplitudes.Select(a => Mathf.Pow(a - averageAmplitude, 2)).Average();
        float stdDev = Mathf.Sqrt(variance);

        threshold = averageAmplitude + stdDev * 1.5f;

        if (amplitude > threshold && Time.time > lastBeatTime + timeBetweenBeats)
        {
            lastBeatTime = Time.time;
            OnBeatDetected();
        }
    }

    void OnBeatDetected()
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
}
