using Comfort.Common;
using EFT;
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

        protected void Awake()
        {
            player = (LocalPlayer)Singleton<GameWorld>.Instance.MainPlayer;

            if (player == null)
            {
                Destroy(this);
            }

            if (!player.IsYourPlayer)
            {
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

            // Check if the current animator has the 'Strumming' parameter
            int param = handsController.FirearmsAnimator.Animator.StringToHash("Strumming");
            if (handsController.FirearmsAnimator.Animator.HasParameter(param))
            {
                currentKnifeController = handsController as Player.BaseKnifeController;
                guitarSoundComponent = currentKnifeController.ControllerGameObject.GetComponent<BaseSoundPlayer>();
            }
            else
            {
                songPlaying = false;
                return;
            }
            
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
        }

        
    }
}