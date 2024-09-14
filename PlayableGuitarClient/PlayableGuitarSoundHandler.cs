using System;
using PrivateRyan.PlayableGuitar.Helpers;
using PrivateRyan.TarkovMIDI.Controllers;
using UnityEngine;

namespace PrivateRyan.PlayableGuitar
{
    public class PlayableGuitarSoundHandler : MonoBehaviour
    {
        private float[] buffer;
        private int bufferSize = 44100 * 5 * 2;
        private MIDIController guitarMidi;

        private bool isNotePlaying = false;
        private int activeChannels = 2;

        private AudioSource audioSource;

        private void Awake()
        {
            buffer = new float[bufferSize];
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.spatialize = true;
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            
            if (audioSource.clip == null)
            {
                audioSource.clip = AudioClip.Create("EmptyClip", bufferSize, activeChannels, 44100, false);
            }

            audioSource.loop = true;
            audioSource.Play();
        }

        private void ClearBuffer()
        {
            Array.Clear(buffer, 0, buffer.Length);
        }
        
        private void OnAudioFilterRead(float[] data, int channels)
        {
            ClearBuffer();
            
            if (buffer.Length != data.Length)
            {
                Array.Resize(ref buffer, data.Length);
            }
            
            if (isNotePlaying)
            {
                guitarMidi.SoundFont.RenderAudio(buffer);
            }
            
            float volume = Settings.GuitarVolume.Value;
            
            int lengthToCopy = Mathf.Min(buffer.Length, data.Length);
            for (int i = 0; i < lengthToCopy; i++)
            {
                data[i] = Mathf.Clamp(buffer[i] * volume, -1.0f, 1.0f);
            }
        }

        public void PlayNoteTriggered(int note, int velocity)
        {
            isNotePlaying = true;
        }

        public void StopNoteTriggered(int note)
        {
            if (!guitarMidi.NotePlaying)
            {
                isNotePlaying = false;
            }
        }

        public void Initialize(MIDIController midiController)
        {
            guitarMidi = midiController;
            PlayableGuitarPlugin.PBLogger.LogInfo("Guitar Sound Handler Initialized");
        }
    }
}
