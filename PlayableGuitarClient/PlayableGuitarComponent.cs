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

            // Other stuff
            // Play music?
            if (WeaponAnimSpeedControllerPatch.Strumming && !SongPlaying)
            {
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is strumming and no song is playing");
                SongPlaying = true;
                
                Player.AbstractHandsController handsController = player.HandsController;
                if (handsController is Player.BaseKnifeController currentKnifeController)
                {
                    BaseSoundPlayer knifeSounds =
                        currentKnifeController.ControllerGameObject.GetComponent<BaseSoundPlayer>();
                    if (knifeSounds != null)
                    {
                        knifeSounds.SoundEventHandler("Song");
                        PlayableGuitarPlugin.PBLogger.LogInfo("Playing song");
                        currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", true);
                    }
                }
            } else if (!WeaponAnimSpeedControllerPatch.Strumming && SongPlaying)
            {
                PlayableGuitarPlugin.PBLogger.LogInfo("Player is no longer strumming");
                SongPlaying = false;
                Player.AbstractHandsController handsController = player.HandsController;
                if (handsController is Player.BaseKnifeController currentKnifeController)
                {
                    BaseSoundPlayer knifeSounds =
                        currentKnifeController.ControllerGameObject.GetComponent<BaseSoundPlayer>();
                    if (knifeSounds != null)
                    {
                        knifeSounds.ReleaseClipsSource();
                        PlayableGuitarPlugin.PBLogger.LogInfo("Stopping song");
                        currentKnifeController.FirearmsAnimator.Animator.SetBool("SongPlaying", false);
                    }
                }
            }
        }

        
    }
}