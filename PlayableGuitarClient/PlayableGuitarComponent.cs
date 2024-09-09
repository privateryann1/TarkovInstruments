using Comfort.Common;
using EFT;
using PrivateRyan.PlayableGuitar.Helpers;
using PrivateRyan.PlayableGuitar.Patches;
using UnityEngine;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarComponent : MonoBehaviour
    {
        public LocalPlayer player;
        private bool songPlaying = false;
        private BaseSoundPlayer guitarSoundComponent;
        private Player.AbstractHandsController handsController;
        private Player.BaseKnifeController currentKnifeController;
        
        private PlayableGuitarMidi guitarMidi;

        protected void Awake()
        {
            player = (LocalPlayer)Singleton<GameWorld>.Instance.MainPlayer;
            guitarMidi = new PlayableGuitarMidi();

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
                PlayableGuitarMidi.HasGuitar = true;
            }
            else
            {
                // Not a guitar, reset values and return
                WeaponAnimSpeedControllerPatch.Strumming = false;
                songPlaying = false;
                PlayableGuitarMidi.HasGuitar = false;
                return;
            }
            
            // Check if the key to play the MIDI song is pressed
            if (Input.GetKeyDown(Settings.PlayMidiKey.Value) && !songPlaying)
            {
                PlayableGuitarMidi.PlayMidiSong();
                songPlaying = true;
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to play song");
            }
            else if (Input.GetKeyDown(Settings.PlayMidiKey.Value) && songPlaying)
            {
                PlayableGuitarMidi.StopMidiSong();
                songPlaying = false;
                PlayableGuitarPlugin.PBLogger.LogInfo("Telling MIDI to stop song");
            }
            
            // MIDI Stuff
            if (!WeaponAnimSpeedControllerPatch.Strumming && PlayableGuitarMidi.NotePlaying)
            {
                currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, true);
                
                currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
                currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", true);

                WeaponAnimSpeedControllerPatch.Strumming = true;
            }
            else if (WeaponAnimSpeedControllerPatch.Strumming && !PlayableGuitarMidi.NotePlaying)
            {
                currentKnifeController.FirearmsAnimator.Animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, false);
                currentKnifeController.OnFireEnd();
                
                currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
                currentKnifeController.FirearmsAnimator.Animator.SetBool("Strumming", false);
                
                WeaponAnimSpeedControllerPatch.Strumming = false;
            }
            /*
            // Handle song playing
            if (WeaponAnimSpeedControllerPatch.Strumming && !songPlaying)
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
            } else if (!WeaponAnimSpeedControllerPatch.Strumming && songPlaying)
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
            */
        }

        
    }
}