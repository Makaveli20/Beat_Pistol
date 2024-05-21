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

    float[] freqSpectrum = new float[4];
    float[] freqAvgSpectrum = new float[4];

    public bool bass, low;

    Queue<List<float>> FFTHistory_beatDetector = new Queue<List<float>>();

    int FFTHistory_maxSize;
    List<int> beatDetector_bandLimits = new List<int>();

    private List<float> amplitudeSamples = new List<float>();
    private List<GameObject> activeTargets = new List<GameObject>();
    private int selfDestructCount = 0;
    private float sensitivity = 1.5f;
    private float threshold;
    private float lastBeatTime;
    private float lastSpawnTime;
    private float timeBetweenBeats = 0.2f;

    void Awake()
    {
        int bandsize = audioSource.clip.frequency / 1024;

        FFTHistory_maxSize = audioSource.clip.frequency / 1024;

        beatDetector_bandLimits.Clear();

        beatDetector_bandLimits.Add(bassLowerLimit / bandsize);
        beatDetector_bandLimits.Add(bassUpperLimit / bandsize);
        beatDetector_bandLimits.Add(lowLowerLimit / bandsize);
        beatDetector_bandLimits.Add(lowUpperLimit / bandsize);

        beatDetector_bandLimits.TrimExcess();
        FFTHistory_beatDetector.Clear();
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            GetBeat(ref freqSpectrum, ref freqAvgSpectrum, ref bass, ref low);
        }
    }

    void GetBeat(ref float[] spectrum, ref float[] avgSpectrum, ref bool isBass, ref bool isLow)
    {
        int numBands = 2;
        int numChannels = audioSource.clip.channels;
        for (int numBand = 0; numBand < numBands; ++numBand)
        {
            for (int indexFFT = beatDetector_bandLimits[numBand]; indexFFT < beatDetector_bandLimits[numBand + 1]; ++indexFFT)
            {
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    float[] tempSample = new float[1024];
                    audioSource.GetSpectrumData(tempSample, channel, FFTWindow.Rectangular);
                    spectrum[numBand] += tempSample[indexFFT];
                }
            }
            spectrum[numBand] /= (beatDetector_bandLimits[numBand + 1] - beatDetector_bandLimits[numBand] * numBand);
        }
        if (FFTHistory_beatDetector.Count > 0)
        {
            FillAvgSpectrum(ref avgSpectrum, numBands, ref FFTHistory_beatDetector);

            float[] varianceSpectrum = new float[numBands];

            FillVarianceSpectrum(ref varianceSpectrum, numBands, ref FFTHistory_beatDetector, ref avgSpectrum);
            isBass = (spectrum[0]) > BeatThreshold(varianceSpectrum[0]) * avgSpectrum[0];
            isLow = (spectrum[1]) > BeatThreshold(varianceSpectrum[1]) * avgSpectrum[1];

            if (isBass || isLow)
            {
                float amplitude = isBass ? spectrum[0] : spectrum[1];
                amplitudeSamples.Add(amplitude);

                if (amplitudeSamples.Count > windowSize)
                {
                    amplitudeSamples.RemoveAt(0);
                }

                AdjustSensitivityAndThreshold();

                if (amplitude > threshold && Time.time > lastBeatTime + timeBetweenBeats)
                {
                    lastBeatTime = Time.time;
                    OnBeatDetected();
                }

                Debug.Log($"Amplitude: {amplitude}, Threshold: {threshold}, Sensitivity: {sensitivity}");
            }
        }

        List<float> fftResult = new List<float>(numBands);

        for (int index = 0; index < numBands; ++index)
        {
            fftResult.Add(spectrum[index]);
        }

        if (FFTHistory_beatDetector.Count >= FFTHistory_maxSize)
        {
            FFTHistory_beatDetector.Dequeue();
        }
        FFTHistory_beatDetector.Enqueue(fftResult);
    }

    void FillAvgSpectrum(ref float[] avgSpectrum, int numBands, ref Queue<List<float>> fftHistory)
    {
        foreach (List<float> fftResult in fftHistory)
        {
            for (int index = 0; index < fftResult.Count; ++index)
            {
                avgSpectrum[index] += fftResult[index];
            }
        }

        for (int index = 0; index < numBands; ++index)
        {
            avgSpectrum[index] /= (fftHistory.Count);
        }
    }

    void FillVarianceSpectrum(ref float[] varianceSpectrum, int numBands, ref Queue<List<float>> fftHistory, ref float[] avgSpectrum)
    {
        foreach (List<float> fftResult in fftHistory)
        {
            for (int index = 0; index < fftResult.Count; ++index)
            {
                varianceSpectrum[index] += (fftResult[index] - avgSpectrum[index]) * (fftResult[index] - avgSpectrum[index]);
            }
        }

        for (int index = 0; index < numBands; ++index)
        {
            varianceSpectrum[index] /= (fftHistory.Count);
        }
    }

    float BeatThreshold(float variance)
    {
        return -15f * variance + 1.55f;
    }

    public void ChangeClip(AudioClip clip)
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
        audioSource.clip = clip;
        audioSource.Play();
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
