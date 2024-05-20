using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.iOS;

public class BPM : MonoBehaviour
{
    private static BPM _bpm;
    public float _bpmcount;
    private float _beatInterval, _beatTimer, _BeatIntervalD8, _BeatTimerD8;
    public static bool _beatfull, _beatD8;
    public static int _beatCountFull, _beatCountD8;
    private void Awake()
    {
        if (_bpm != null && _bpm != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _bpm = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        BeatDetection();
    }

     void BeatDetection()
     {
         _beatfull = false;
         _beatInterval = 60 / _bpmcount;
         _beatTimer += Time.deltaTime;
         if (_beatTimer >= _beatInterval)
         {
             _beatTimer -= _beatInterval;
             _beatfull = true;
             _beatCountFull++;
             Debug.Log("Full");
         }

         _beatD8 = false;
         _BeatIntervalD8 = _beatInterval / 8;
         _BeatTimerD8 += Time.deltaTime;

         
         if (_BeatTimerD8 >= _BeatIntervalD8)
         {
             _BeatTimerD8 -= _BeatIntervalD8;
             _beatD8 = true;
             _beatCountD8++;
             Debug.Log("D8");
         }

     }
}
