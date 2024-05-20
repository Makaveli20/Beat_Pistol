using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BeatDetector : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip startingAudioClip;
    public Renderer bassObjectRenderer;
    public Color bassColOld;
    public Color bassColNew;
    public Material bassObjectMaterial;
    public Renderer lowObjectRenderer;
    public Color lowColOld;
    public Color lowColNew;
    public Material lowObjectMaterial;

    public int bassLowerLimit = 60;
    public int bassUpperLimit = 180;
    public int lowLowerLimit = 500;
    public int lowUpperLimit = 2000;

    const float lerp = 0.1f;

    private int windowSize;
    private float samplingFrequency;

    float[] freqSpectrum = new float[4];
    float[] freqAvgSpectrum = new float[4];

    public bool bass, low;

    Queue<List<float>> FFTHistory_beatDetector = new Queue<List<float>>();

    int FFTHistory_maxSize;
    List<int> beatDetector_bandLimits = new List<int>();

    // Define UnityEvents
    public UnityEvent onBassBeat;
    public UnityEvent onLowBeat;

    void Awake()
    {
        // initialize
        audioSource.clip = startingAudioClip;
        audioSource.Play();
        int bandsize = audioSource.clip.frequency / 1024; // bandsize = (samplingFrequency / windowSize)

        FFTHistory_maxSize = audioSource.clip.frequency / 1024;

        beatDetector_bandLimits.Clear();

        // bass 60hz-180hz
        beatDetector_bandLimits.Add(bassLowerLimit / bandsize);
        beatDetector_bandLimits.Add(bassUpperLimit / bandsize);
        // low midrange 500hz-2000hz
        beatDetector_bandLimits.Add(lowLowerLimit / bandsize);
        beatDetector_bandLimits.Add(lowUpperLimit / bandsize);

        beatDetector_bandLimits.TrimExcess();
        FFTHistory_beatDetector.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        // Check if current sample is above statistical threshold
        GetBeat(ref freqSpectrum, ref freqAvgSpectrum, ref bass, ref low);

        // Invoke events based on beat detection
        if (bass) 
        {
            Debug.Log("Bass beat detected!");
            onBassBeat.Invoke();
        }
        if (low) 
        {
            Debug.Log("Low beat detected!");
            onLowBeat.Invoke();
        }
    }

    private void LateUpdate()
    {
        // change color of cubes based on booleans
        if (bass)
        {
            bassObjectMaterial.color = Color.Lerp(bassObjectMaterial.color, bassColNew, lerp);
        }
        else
        {
            bassObjectMaterial.color = Color.Lerp(bassObjectMaterial.color, bassColOld, lerp);
        }

        if (low)
        {
            lowObjectMaterial.color = Color.Lerp(lowObjectMaterial.color, lowColNew, lerp);
        }
        else
        {
            lowObjectMaterial.color = Color.Lerp(lowObjectMaterial.color, lowColOld, lerp);
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
        foreach (List<float> iterator in fftHistory)
        {
            List<float> fftResult = iterator;

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
        foreach (List<float> iterator in fftHistory)
        {
            List<float> fftResult = iterator;

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
}
