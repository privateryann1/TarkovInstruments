using System.Collections;
using Comfort.Common;
using EFT;
using PrivateRyan.PlayableGuitar.Patches;
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
        private TarkovMIDI.Controllers.MIDIController guitarMidi;
        
        private float[] buffer;

        protected void Awake()
        {
            player = (LocalPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            guitarMidi = new TarkovMIDI.Controllers.MIDIController();

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
            
            buffer = new float[44100 * 5 * 2];
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
                    TarkovMIDI.Controllers.MIDIController.HasInstrument = true;
            }
            else
            {
                // Not a guitar, reset values and return
                WeaponAnimSpeedControllerPatch.Strumming = false;
                songPlaying = false;
                if (TarkovMIDI.Helpers.Settings.UseMIDI.Value)
                    TarkovMIDI.Controllers.MIDIController.HasInstrument = false;
                return;
            }
            
            // Check if the key to play the MIDI song is pressed
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && Input.GetKeyDown(TarkovMIDI.Helpers.Settings.PlayMidiKey.Value) && !songPlaying)
            {
                TarkovMIDI.Controllers.MIDIController.PlayMidiSong();
                songPlaying = true;
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to play song");
            }
            else if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && Input.GetKeyDown(TarkovMIDI.Helpers.Settings.PlayMidiKey.Value) && songPlaying)
            {
                TarkovMIDI.Controllers.MIDIController.StopMidiSong();
                songPlaying = false;
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to stop song");
            }
            
            // MIDI Stuff
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && !WeaponAnimSpeedControllerPatch.Strumming && TarkovMIDI.Controllers.MIDIController.NotePlaying)
            {
                currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, true);
                
                currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
                currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", true);

                WeaponAnimSpeedControllerPatch.Strumming = true;
            }
            else if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && WeaponAnimSpeedControllerPatch.Strumming && !TarkovMIDI.Controllers.MIDIController.NotePlaying)
            {
                currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, false);
                currentKnifeController.OnFireEnd();
                
                currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
                currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", false);
                
                WeaponAnimSpeedControllerPatch.Strumming = false;
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
            
            TarkovMIDI.Controllers.MIDIController.SoundFont.RenderAudio(buffer);
            
            ApplyFadeIn(buffer, 1000);
            ApplyFadeOut(buffer, 1000);
            
            AudioClip noteClip = CreateAudioClipFromBuffer(buffer, 44100, 2);
            
            guitarSoundComponent.PlayClip(noteClip, 30, 1f);
            
            PlayableGuitarPlugin.PBLogger.LogInfo("Note playing, rendering audio...");
            
            StartCoroutine(ReleaseClipAfterPlay(noteClip, 5.0f));
        }

        public void StopNoteTriggered(int note)
        {
            
        }
        
        // Need to remove the audio clip from memory otherwise we hog all the rams :(
        private IEnumerator ReleaseClipAfterPlay(AudioClip clip, float playTime)
        {
            yield return new WaitForSeconds(playTime);
    
            if (clip != null)
            {
                AudioClip.Destroy(clip);
                clip = null;
                PlayableGuitarPlugin.PBLogger.LogInfo("AudioClip released from memory");
            }
        }
        
        private void ClearBuffer()
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0f;
            }
        }
        
        private AudioClip CreateAudioClipFromBuffer(float[] buffer, int sampleRate, int channels)
        {
            AudioClip clip = AudioClip.Create("GuitarNote", buffer.Length / channels, channels, sampleRate, false);
            clip.SetData(buffer, 0);
            return clip;
        }
        
    }
}