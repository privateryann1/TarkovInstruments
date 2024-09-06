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
            if (!animator.HasParameter(animator.StringToHash("Strumming")))
                return true;
            
            if (!Input.GetKey(KeyCode.Mouse1))
                return true;
            
            PlayableGuitarComponent component = Comfort.Common.Singleton<GameWorld>.Instance.MainPlayer.gameObject.GetComponent<PlayableGuitarComponent>();
            
            if (Time.time - lastToggleTime < debounceDelay)
            {
                Logger.LogInfo("WeaponAnimSpeedControllerPatch Debounce in effect, skipping toggle");
                return false;
            }
            
            lastToggleTime = Time.time;

            Strumming = !animator.GetBool("Strumming");
            animator.SetBool(WeaponAnimationSpeedControllerClass.BOOL_ALTFIRE, Strumming);
            if (!Strumming)
            {
                component.player.GetComponent<Player.KnifeController>().OnFireEnd();
                animator.SetBool("Strumming", false);
                animator.SetBool("SongPlaying", false);
            }
            else
            {
                animator.SetBool("Strumming", true);
            }
            
            Logger.LogInfo($"Strumming: {animator.GetBool("Strumming")}");
            Logger.LogInfo($"Song Playing: {animator.GetBool("SongPlaying")}");

            return false;
        }
    }
}