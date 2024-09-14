using Comfort.Common;
using EFT;
using PrivateRyan.PlayableGuitar.Patches;
using PrivateRyan.TarkovMIDI.Controllers;
using PrivateRyan.TarkovMIDI.Interfaces;
using UnityEngine;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarComponent : MonoBehaviour, IInstrumentComponent
    {
        public LocalPlayer player;
        private BaseSoundPlayer guitarSoundComponent;
        private PlayableGuitarSoundHandler guitarSoundHandler;
        private Player.AbstractHandsController handsController;
        private Player.BaseKnifeController currentKnifeController;
        private MIDIController guitarMidi;
        private bool normalSongPlaying;

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
                if (guitarSoundHandler == null)
                {
                    guitarSoundHandler = currentKnifeController.ControllerGameObject.AddComponent<PlayableGuitarSoundHandler>();
                    guitarSoundHandler.Initialize(guitarMidi);
                }
                    
                
                if (TarkovMIDI.Helpers.Settings.UseMIDI.Value)
                    guitarMidi.HasInstrument = true;
            }
            else
            {
                // Not a guitar, reset values and return
                if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && guitarMidi != null)
                {
                    guitarMidi.HasInstrument = false;
                    if (guitarMidi.SongPlaying)
                    {
                        guitarMidi.StopMidiSong();
                    }
                }
                
                WeaponAnimSpeedControllerPatch.Strumming = false;
                normalSongPlaying = false;
                
                return;
            }
            
            // MIDI Song Start
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && Input.GetKeyDown(TarkovMIDI.Helpers.Settings.PlayMidiKey.Value) && !guitarMidi.SongPlaying)
            {
                guitarMidi.PlayMidiSong();
                PlayStrumming();
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to play song");
                return;
            }
            
            // MIDI Song End
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && Input.GetKeyDown(TarkovMIDI.Helpers.Settings.PlayMidiKey.Value) && guitarMidi.SongPlaying)
            {
                guitarMidi.StopMidiSong();
                EndStrumming();
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to stop song");
                return;
            }
            
            // MIDI Note Play
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && !WeaponAnimSpeedControllerPatch.Strumming && guitarMidi.NotePlaying && !guitarMidi.SongPlaying)
            {
                PlayStrumming();
                return;
            }
            
            // MIDI Note End
            if (TarkovMIDI.Helpers.Settings.UseMIDI.Value && WeaponAnimSpeedControllerPatch.Strumming && !guitarMidi.NotePlaying && !guitarMidi.SongPlaying)
            {
                EndStrumming();
                return;
            }
            
            // Handle normal song playing
            if (WeaponAnimSpeedControllerPatch.Strumming && !normalSongPlaying && !TarkovMIDI.Helpers.Settings.UseMIDI.Value)
            {
                // Player is strumming, but song is not playing yet
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is strumming and no song is playing");
                normalSongPlaying = true;

                if (guitarSoundComponent != null)
                {
                    PlayableGuitarPlugin.PBLogger.LogInfo("Playing song");
                    guitarSoundComponent.SoundEventHandler("Song");
                    currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
                }
            } else if (!WeaponAnimSpeedControllerPatch.Strumming && normalSongPlaying && !TarkovMIDI.Helpers.Settings.UseMIDI.Value)
            {
                // Player is no longer strumming, and the song is still playing
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is no longer strumming");
                normalSongPlaying = false;
                
                if (guitarSoundComponent != null)
                {
                    PlayableGuitarPlugin.PBLogger.LogInfo("Stopping song");
                    guitarSoundComponent.ReleaseClipsSource();
                    currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
                }
            }
            
        }

        private void PlayStrumming()
        {
            currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, true);
            currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
            currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", true);
            WeaponAnimSpeedControllerPatch.Strumming = true;
            PlayableGuitarPlugin.PBLogger.LogInfo("Song Playing True");
        }
        
        private void EndStrumming()
        {
            currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, false);
            currentKnifeController.OnFireEnd();
            currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
            currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", false);
            WeaponAnimSpeedControllerPatch.Strumming = false;
            PlayableGuitarPlugin.PBLogger.LogInfo("Song Playing False");
        }
        
        public void PlayNoteTriggered(int note, int velocity)
        {
            guitarSoundHandler.PlayNoteTriggered(note, velocity);
        }

        public void StopNoteTriggered(int note)
        {
            guitarSoundHandler.StopNoteTriggered(note);
        }
    }
}