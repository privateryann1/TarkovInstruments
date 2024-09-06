using Comfort.Common;
using EFT;
using PrivateRyan.PlayableGuitar.Patches;
using UnityEngine;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarComponent : MonoBehaviour
    {
        public LocalPlayer player;
        public bool SongPlaying = false;
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

            if (handsController.FirearmsAnimator.Animator.HasParameter(
                    handsController.FirearmsAnimator.Animator.StringToHash("Strumming")))
            {
                currentKnifeController = handsController as Player.BaseKnifeController;
                guitarSoundComponent = currentKnifeController.ControllerGameObject.GetComponent<BaseSoundPlayer>();
            }
            else
            {
                return;
            }
            
            if (WeaponAnimSpeedControllerPatch.Strumming && !SongPlaying)
            {
                // Player is strumming, but song is not playing yet
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is strumming and no song is playing");
                SongPlaying = true;

                if (guitarSoundComponent != null)
                {
                    PlayableGuitarPlugin.PBLogger.LogInfo("Playing song");
                    guitarSoundComponent.SoundEventHandler("Song");
                    currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
                }
            } else if (!WeaponAnimSpeedControllerPatch.Strumming && SongPlaying)
            {
                // Player is no longer strumming, and the song is still playing
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is no longer strumming");
                SongPlaying = false;
                
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