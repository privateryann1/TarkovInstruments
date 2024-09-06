using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
namespace PrivateRyan.PlayableGuitar.Patches
{
    public class WeaponAnimSpeedControllerPatch : ModulePatch
    {
        private static float lastToggleTime = 0f;
        private static float debounceDelay = 1f;
        public static bool Strumming = false;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(WeaponAnimationSpeedControllerClass),
                "SetAltFire",
                new[] { typeof(IAnimator), typeof(bool) }
            );
        }

        [PatchPrefix]
        private static bool PatchPrefix(IAnimator animator, bool altFire)
        {
            // Check if it is a guitar
            if (!animator.HasParameter(animator.StringToHash("Strumming")))
                return true;
            
            // Check if it is right click
            if (!Input.GetKey(KeyCode.Mouse1))
                return true;
            
            PlayableGuitarComponent component = Comfort.Common.Singleton<GameWorld>.Instance.MainPlayer.gameObject.GetComponent<PlayableGuitarComponent>();
            
            // Debounce because SetAltFire was being called twice with one right click, need to investigate further
            // Not ideal
            if (Time.time - lastToggleTime < debounceDelay)
            {
                PlayableGuitarPlugin.PBLogger.LogInfo("WepAnimPatch Debounce in effect, skipping toggle");
                return false;
            }
            lastToggleTime = Time.time;
            
            // Update the strumming parameter on the animator
            Strumming = !animator.GetBool("Strumming");
            animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, Strumming);
            if (!Strumming)
            {
                // No longer strumming, end the action
                component.player.GetComponent<Player.KnifeController>().OnFireEnd();
                animator.SetBool("Strumming", false);
                animator.SetBool("SongPlaying", false);
            }
            else
            {
                animator.SetBool("Strumming", true);
            }
            
            PlayableGuitarPlugin.PBLogger.LogInfo($"Strumming: {animator.GetBool("Strumming")}");

            return false;
        }
    }
}