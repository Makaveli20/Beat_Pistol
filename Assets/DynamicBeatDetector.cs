using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class DynamicBeatDetector : MonoBehaviour
{
    public GameObject targetPrefab; // Reference to the target prefab
    public Transform spawnArea; // Reference to the spawn area (use a GameObject with a BoxCollider)
    public Vector3 spawnAreaSize = new Vector3(10f, 10f, 10f); // Size of the spawn area (x, y)
    public float spawnCooldown = 1f; // Cooldown time between spawns
    public float targetLifetime = 3f; // Lifetime of each target
    public int maxSelfDestructs = 3; // Maximum number of self-destroyed targets before game over

    private AudioSource audioSource;
    private float[] spectrum = new float[1024];
    private List<float> amplitudeSamples = new List<float>();
    private List<GameObject> activeTargets = new List<GameObject>();
    private int selfDestructCount = 0; // Counter for self-destroyed targets
    private int windowSize = 1024; // Size of the rolling window for amplitude samples
    private float sensitivity = 100.0f; // Initial sensitivity
    private float threshold;
    private float lastBeatTime;
    private float lastSpawnTime;
    private float timeBetweenBeats = 0.2f; // Minimum time between beats to avoid rapid firing

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.Play(); // Start playing the audio
    }

    void Update()
    {
        AnalyzeAudio(); // Continuously analyze the audio in each frame
    }

    void AnalyzeAudio()
    {
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // Smooth the spectrum data
        float[] smoothedSpectrum = SmoothSpectrum(spectrum);

        // Focus on lower frequencies (bass)
        float lowFreqMax = 0;
        int lowFreqEnd = spectrum.Length / 8; // Focus on the first 1/8th of the spectrum

        for (int i = 0; i < lowFreqEnd; i++)
        {
            if (smoothedSpectrum[i] > lowFreqMax)
            {
                lowFreqMax = smoothedSpectrum[i];
            }
        }

        // Calculate the amplitude using the initial sensitivity
        float amplitude = lowFreqMax * sensitivity;
        amplitudeSamples.Add(amplitude);

        // Maintain a rolling window of amplitude samples
        if (amplitudeSamples.Count > windowSize)
        {
            amplitudeSamples.RemoveAt(0); // Remove the oldest value to keep the window size constant
        }

        // Adjust sensitivity and threshold based on the rolling window
        AdjustSensitivityAndThreshold();

        // Check if the amplitude exceeds the threshold and enough time has passed since the last beat
        if (amplitude > threshold && Time.time > lastBeatTime + timeBetweenBeats)
        {
            lastBeatTime = Time.time;
            OnBeatDetected();
        }

        
    }

    float[] SmoothSpectrum(float[] spectrum)
    {
        float[] smoothedSpectrum = new float[spectrum.Length];
        int smoothRange = 3; // Number of neighboring points to average

        for (int i = 0; i < spectrum.Length; i++)
        {
            float sum = 0;
            int count = 0;

            for (int j = -smoothRange; j <= smoothRange; j++)
            {
                int index = i + j;
                if (index >= 0 && index < spectrum.Length)
                {
                    sum += spectrum[index];
                    count++;
                }
            }

            smoothedSpectrum[i] = sum / count;
        }

        return smoothedSpectrum;
    }

    void AdjustSensitivityAndThreshold()
    {
        // Calculate the average amplitude in the rolling window
        float averageAmplitude = 0;
        foreach (float sample in amplitudeSamples)
        {
            averageAmplitude += sample;
        }
        averageAmplitude /= amplitudeSamples.Count;

        // Calculate the standard deviation of the amplitudes in the rolling window
        float variance = 0;
        foreach (float sample in amplitudeSamples)
        {
            variance += Mathf.Pow(sample - averageAmplitude, 2);
        }
        variance /= amplitudeSamples.Count;
        float stdDev = Mathf.Sqrt(variance);

        // Set the threshold to be the average amplitude plus one standard deviation
        threshold = averageAmplitude + stdDev;
    }

    void OnBeatDetected()
    {
        // Action when a beat is detected
        Debug.Log("Beat detected!");
        if (Time.time > lastSpawnTime + spawnCooldown)
        {
            SpawnTarget();
            lastSpawnTime = Time.time;
        }
        // Add additional actions (e.g., trigger animations, particle effects) here
    }

    void SpawnTarget()
    {
        if (targetPrefab && spawnArea)
        {
            Vector3 randomPosition = GetRandomPositionInArea();

            if (!IsPositionOccupied(randomPosition))
            {
                GameObject target = Instantiate(targetPrefab, randomPosition, Quaternion.identity);
                activeTargets.Add(target);
                StartCoroutine(DestroyTargetAfterTime(target, targetLifetime));
            }
        }
        else
        {
            Debug.LogWarning("Target prefab or spawn area not set.");
        }
    }

    Vector3 GetRandomPositionInArea()
    {
        Vector3 spawnPosition = spawnArea.position;
        float halfWidth = spawnAreaSize.x / 2;
        float halfHeight = spawnAreaSize.y / 2;
        spawnPosition.x += Random.Range(-halfWidth, halfWidth);
        spawnPosition.y += Random.Range(-halfHeight, halfHeight);
        spawnPosition.z += Random.Range(-halfWidth, halfWidth); // Assuming z dimension is the same as x for 3D
        return spawnPosition;
    }

    bool IsPositionOccupied(Vector3 position)
    {
        // Clean up the activeTargets list by removing null references
        activeTargets.RemoveAll(target => target == null);

        foreach (GameObject target in activeTargets)
        {
            if (Vector3.Distance(target.transform.position, position) < 1f) // Adjust the distance as needed
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
        // Implement game over logic (e.g., stop spawning targets, show game over screen)
    }
}
