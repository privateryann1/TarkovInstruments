using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using PrivateRyan.PlayableGuitar.Helpers;
using PrivateRyan.PlayableGuitar.Patches;
using PrivateRyan.TarkovMIDI.Controllers;
using PrivateRyan.TarkovMIDI.Interfaces;
using UnityEngine;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarComponent : MonoBehaviour, IInstrumentComponent
    {
        public LocalPlayer player;
        private bool songPlaying = false;
        private BaseSoundPlayer guitarSoundComponent;
        private Player.AbstractHandsController handsController;
        private Player.BaseKnifeController currentKnifeController;
        private MIDIController guitarMidi;
        
        private float[] buffer;
        
        private Queue<AudioClip> audioClipPool = new Queue<AudioClip>();
        private const int PoolSize = 40;
        private const int ClipDurationInSamples = 44100 * 3;

        protected void Awake()
        {
            player = (LocalPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            guitarMidi = new MIDIController(this);

            if (player == null)
            {
                guitarMidi.Dispose();
                Destroy(this);
            }

            if (!player.IsYourPlayer)
            {
                guitarMidi.Dispose();
                Destroy(this);
            }
            
            buffer = new float[44100 * 3 * 2];
            
            InitializeAudioClipPool();
        }
        
        protected void Update()
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                return;
            }

            if (player == null)
            {
                return;
            }

            if (TarkovMIDI.Helpers.Settings.ReconnectMIDI.Value)
            {
                guitarMidi.ReconnectToMIDI(TarkovMIDI.Helpers.Settings.SelectedMIDIDevice.Value);
                TarkovMIDI.Helpers.Settings.ReconnectMIDI.Value = false;
            }

            if (handsController == null)
                handsController = player.HandsController;

            if (handsController.FirearmsAnimator == null || handsController.FirearmsAnimator.Animator == null)
                return;

            // Check if the current animator has the 'Strumming' parameter
            int param = handsController.FirearmsAnimator.Animator.StringToHash("Strumming");
            if (handsController.FirearmsAnimator.Animator.HasParameter(param))
            {
                currentKnifeController = handsController as Player.BaseKnifeController;
                if (currentKnifeController == null)
                    return;
                
                guitarSoundComponent = currentKnifeController.ControllerGameObject.GetComponent<BaseSoundPlayer>();
                
                if (TarkovMIDI.Helpers.Settings.UseMIDI.Value)
                    guitarMidi.HasInstrument = true;
            }
            else
            {
                // Not a guitar, reset values and return
                WeaponAnimSpeedControllerPatch.Strumming = false;
                songPlaying = false;
                
                if (TarkovMIDI.Helpers.Settings.UseMIDI.Value)
                    guitarMidi.HasInstrument = false;
                
                return;
            }
            
            // Check if the key to play the MIDI song is pressed
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && Input.GetKeyDown(TarkovMIDI.Helpers.Settings.PlayMidiKey.Value) && !songPlaying)
            {
                guitarMidi.PlayMidiSong();
                SongPlaying();
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to play song");
                return;
            }
            
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && Input.GetKeyDown(TarkovMIDI.Helpers.Settings.PlayMidiKey.Value) && songPlaying)
            {
                guitarMidi.StopMidiSong();
                SongEnd();
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to stop song");
                return;
            }
            
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && !WeaponAnimSpeedControllerPatch.Strumming && guitarMidi.NotePlaying && !songPlaying)
            {
                SongPlaying();
                return;
            }
            
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && WeaponAnimSpeedControllerPatch.Strumming && !guitarMidi.NotePlaying && !songPlaying)
            {
                SongEnd();
                return;
            }
            
            // Handle normal song playing
            if (WeaponAnimSpeedControllerPatch.Strumming && !songPlaying && !TarkovMIDI.Helpers.Settings.UseMIDI.Value)
            {
                // Player is strumming, but song is not playing yet
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is strumming and no song is playing");
                songPlaying = true;

                if (guitarSoundComponent != null)
                {
                    PlayableGuitarPlugin.PBLogger.LogInfo("Playing song");
                    guitarSoundComponent.SoundEventHandler("Song");
                    currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
                }
            } else if (!WeaponAnimSpeedControllerPatch.Strumming && songPlaying && !TarkovMIDI.Helpers.Settings.UseMIDI.Value)
            {
                // Player is no longer strumming, and the song is still playing
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is no longer strumming");
                songPlaying = false;
                
                if (guitarSoundComponent != null)
                {
                    PlayableGuitarPlugin.PBLogger.LogInfo("Stopping song");
                    guitarSoundComponent.ReleaseClipsSource();
                    currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
                }
            }
            
        }

        private void SongPlaying()
        {
            currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, true);
            currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
            currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", true);
            WeaponAnimSpeedControllerPatch.Strumming = true;
            songPlaying = true;
            PlayableGuitarPlugin.PBLogger.LogInfo("Song Playing True");
        }
        
        private void SongEnd()
        {
            currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, false);
            currentKnifeController.OnFireEnd();
            currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
            currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", false);
            WeaponAnimSpeedControllerPatch.Strumming = false;
            songPlaying = false;
            PlayableGuitarPlugin.PBLogger.LogInfo("Song Playing False");
        }
        
        private void InitializeAudioClipPool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                AudioClip clip = AudioClip.Create("PooledClip", ClipDurationInSamples, 2, 44100, false);
                audioClipPool.Enqueue(clip);
            }
            PlayableGuitarPlugin.PBLogger.LogWarning($"Pool initialized with {PoolSize} clips");
        }
        
        private AudioClip GetPooledClip()
        {
            if (audioClipPool.Count > 0)
            {
                // PlayableGuitarPlugin.PBLogger.LogInfo("Using Pooled Clip");
                return audioClipPool.Dequeue();
            }
            else
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("No pooled clips available");
                return null;
            }
        }
        
        private void ReturnClipToPool(AudioClip clip)
        {
            audioClipPool.Enqueue(clip);
        }

        private void ApplyFadeIn(float[] buffer, int fadeSamples)
        {
            for (int i = 0; i < fadeSamples; i++)
            {
                float fadeFactor = (float)i / fadeSamples;
                buffer[i] *= fadeFactor;
            }
        }

        private void ApplyFadeOut(float[] buffer, int fadeSamples)
        {
            int totalSamples = buffer.Length;
            for (int i = 0; i < fadeSamples; i++)
            {
                float fadeFactor = (float)(fadeSamples - i) / fadeSamples;
                buffer[totalSamples - fadeSamples + i] *= fadeFactor;
            }
        }
        
        public void PlayNoteTriggered(int note, int velocity)
        {
            ClearBuffer();
            
            guitarMidi.SoundFont.RenderAudio(buffer);
            
            ApplyFadeIn(buffer, 1000);
            ApplyFadeOut(buffer, 1000);
            
            AudioClip noteClip = GetPooledClip();
            if (noteClip == null)
                return;
            
            if (buffer.Length != noteClip.samples * noteClip.channels)
                PlayableGuitarPlugin.PBLogger.LogError("Buffer size mismatch with AudioClip.");
            
            noteClip.SetData(buffer, 0);
            
            guitarSoundComponent.PlayClip(noteClip, 30, Settings.GuitarVolume.Value);
            
            StartCoroutine(ReturnClipToPoolAfterPlay(noteClip, 3.0f));
        }

        public void StopNoteTriggered(int note)
        {
            
        }
        
        private IEnumerator ReturnClipToPoolAfterPlay(AudioClip clip, float playTime)
        {
            yield return new WaitForSeconds(playTime);

            if (clip != null)
            {
                ReturnClipToPool(clip);
                // PlayableGuitarPlugin.PBLogger.LogInfo("AudioClip returned to pool");
            }
        }

        private void ClearBuffer()
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0f;
            }
        }
    }
}