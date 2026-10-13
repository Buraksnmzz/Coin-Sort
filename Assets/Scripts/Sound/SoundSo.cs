using System;
using System.Collections.Generic;
using Sound;
using UnityEngine;

[CreateAssetMenu]
public class SoundSo : ScriptableObject
{
    [SerializeField] private List<ClipNamePair> audioClipsPair = new();

    [Serializable]
    public class ClipNamePair
    {
        public ClipName soundType;
        public AudioClip audioClip;
        public float volume = 1f;
    }

    public AudioClip GetAudioClip(ClipName clipName)
    {
        var pair = audioClipsPair.Find(x => x.soundType == clipName);
        return pair?.audioClip;
    }

    public float GetVolume(ClipName clipName)
    {
        var pair = audioClipsPair.Find(x => x.soundType == clipName);
        return pair != null ? pair.volume : 1f;
    }
}