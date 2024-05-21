using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(AudioSource))]
public class DynamicBeatDetector : MonoBehaviour
{
    public GameObject targetPrefab;
    public BoxCollider spawnArea;
    public float spawnCooldown = 1f;
    public float targetLifetime = 3f;
    public int maxSelfDestructs = 3;

    private AudioSource audioSource;
    private float[] spectrum = new float[1024];
    private List<float> amplitudeSamples = new List<float>();
    private List<GameObject> activeTargets = new List<GameObject>();
    private int selfDestructCount = 0;
    private int windowSize = 1024;
    private int hopSize;
    private float sensitivity = 1.5f;
    private float threshold;
    private float lastBeatTime;
    private float lastSpawnTime;
    private float timeBetweenBeats = 0.2f;
    private float[] previousSpectrum;
    private Queue<float> recentAmplitudes = new Queue<float>();
    private int recentAmplitudeCount = 20;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.Play();
        hopSize = windowSize / 2;
        previousSpectrum = new float[windowSize];
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            AnalyzeAudio();
        }
    }

    void AnalyzeAudio()
    {
        float[] audioData = new float[windowSize];
        audioSource.GetSpectrumData(audioData, 0, FFTWindow.BlackmanHarris);

        // Normalize audio data
        Normalize(audioData);

        float[] stftResult = PerformSTFT(audioData);

        // Calculate the Spectral Flux
        float[] spectralFlux = CalculateSpectralFlux(stftResult, previousSpectrum);

        // Smooth the Spectral Flux
        float[] smoothedFlux = SmoothFlux(spectralFlux);

        // Detect peaks in the Spectral Flux
        float amplitude = DetectPeaks(smoothedFlux);

        amplitudeSamples.Add(amplitude);
        recentAmplitudes.Enqueue(amplitude);

        if (amplitudeSamples.Count > windowSize)
        {
            amplitudeSamples.RemoveAt(0);
        }

        if (recentAmplitudes.Count > recentAmplitudeCount)
        {
            recentAmplitudes.Dequeue();
        }

        AdjustSensitivityAndThreshold();

        if (amplitude > threshold && Time.time > lastBeatTime + timeBetweenBeats)
        {
            lastBeatTime = Time.time;
            OnBeatDetected();
        }

        // Update previous spectrum
        previousSpectrum = stftResult;

        Debug.Log($"Amplitude: {amplitude}, Threshold: {threshold}, Sensitivity: {sensitivity}");
    }

    void Normalize(float[] data)
    {
        float max = data.Max();
        for (int i = 0; i < data.Length; i++)
        {
            data[i] /= max;
        }
    }

    float[] PerformSTFT(float[] audioData)
    {
        int numWindows = (audioData.Length - windowSize) / hopSize + 1;
        float[] stft = new float[windowSize / 2];

        for (int i = 0; i < numWindows; i++)
        {
            float[] window = new float[windowSize];
            for (int j = 0; j < windowSize; j++)
            {
                window[j] = audioData[i * hopSize + j] * MathUtility.HanningWindow(j, windowSize);
            }

            float[] spectrum = new float[windowSize];
            FFT(window, ref spectrum);

            for (int k = 0; k < windowSize / 2; k++)
            {
                stft[k] += spectrum[k];
            }
        }

        return stft.Select(x => x / numWindows).ToArray();
    }

    void FFT(float[] data, ref float[] spectrum)
    {
        int n = data.Length;
        if (n <= 1)
        {
            spectrum[0] = data[0];
            return;
        }

        float[] even = new float[n / 2];
        float[] odd = new float[n / 2];

        for (int i = 0; i < n / 2; i++)
        {
            even[i] = data[2 * i];
            odd[i] = data[2 * i + 1];
        }

        float[] evenSpectrum = new float[n / 2];
        float[] oddSpectrum = new float[n / 2];

        FFT(even, ref evenSpectrum);
        FFT(odd, ref oddSpectrum);

        for (int i = 0; i < n / 2; i++)
        {
            float t = Mathf.Exp(-2 * Mathf.PI * i / n) * oddSpectrum[i];
            spectrum[i] = evenSpectrum[i] + t;
            spectrum[i + n / 2] = evenSpectrum[i] - t;
        }
    }

    float[] CalculateSpectralFlux(float[] currentSpectrum, float[] previousSpectrum)
    {
        float[] spectralFlux = new float[currentSpectrum.Length];

        for (int i = 0; i < currentSpectrum.Length; i++)
        {
            float flux = currentSpectrum[i] - previousSpectrum[i];
            spectralFlux[i] = Mathf.Max(0, flux);
        }

        return spectralFlux;
    }

    float[] SmoothFlux(float[] spectralFlux)
    {
        float[] smoothedFlux = new float[spectralFlux.Length];
        int smoothRange = 3;

        for (int i = 0; i < spectralFlux.Length; i++)
        {
            float sum = 0;
            int count = 0;

            for (int j = -smoothRange; j <= smoothRange; j++)
            {
                int index = i + j;
                if (index >= 0 && index < spectralFlux.Length)
                {
                    sum += spectralFlux[index];
                    count++;
                }
            }

            smoothedFlux[i] = sum / count;
        }

        return smoothedFlux;
    }

    float DetectPeaks(float[] spectralFlux)
    {
        float peakThreshold = spectralFlux.Average() * 1.5f;
        float maxPeak = 0;

        for (int i = 1; i < spectralFlux.Length - 1; i++)
        {
            if (spectralFlux[i] > spectralFlux[i - 1] && spectralFlux[i] > spectralFlux[i + 1] && spectralFlux[i] > peakThreshold)
            {
                if (spectralFlux[i] > maxPeak)
                {
                    maxPeak = spectralFlux[i];
                }
            }
        }

        return maxPeak;
    }

    void AdjustSensitivityAndThreshold()
    {
        float averageAmplitude = 0;
        foreach (float sample in amplitudeSamples)
        {
            averageAmplitude += sample;
        }
        averageAmplitude /= amplitudeSamples.Count;

        float variance = 0;
        foreach (float sample in amplitudeSamples)
        {
            variance += Mathf.Pow(sample - averageAmplitude, 2);
        }
        variance /= amplitudeSamples.Count;
        float stdDev = Mathf.Sqrt(variance);

        threshold = averageAmplitude + stdDev;

        // Adaptive threshold adjustment
        if (recentAmplitudes.Count > 0)
        {
            float recentAverage = recentAmplitudes.Average();
            float recentVariance = recentAmplitudes.Select(val => Mathf.Pow(val - recentAverage, 2)).Average();
            float recentStdDev = Mathf.Sqrt(recentVariance);

            threshold = recentAverage + recentStdDev;
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

public static class MathUtility
{
    public static float HanningWindow(int index, int size)
    {
        return 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * index / (size - 1)));
    }
}
