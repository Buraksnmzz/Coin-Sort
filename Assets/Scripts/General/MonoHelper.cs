using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class MonoHelper : MonoBehaviour
{

    private static MonoHelper _instance;

    public static event Action<bool> OnApplicationPauseEvent;

    public static MonoHelper Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject runner = new GameObject("MonoRunner");
                _instance = runner.AddComponent<MonoHelper>();
                DontDestroyOnLoad(runner);
            }
            return _instance;
        }
    }
 
    public void DestroyObject(GameObject objectToDestroy)
    {
        Destroy(objectToDestroy);
    }
}