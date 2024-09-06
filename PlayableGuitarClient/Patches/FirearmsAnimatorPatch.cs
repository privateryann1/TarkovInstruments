using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace PrivateRyan.PlayableGuitar.Patches
{
    public class FirearmsAnimatorPatch : ModulePatch
    {
        private static float lastToggleTime = 0f;
        private static float debounceDelay = 1f; // 0.5 seconds debounce delay
        
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(
                typeof(FirearmsAnimator),
                "SetAlternativeFire"
            );
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref bool fire, FirearmsAnimator __instance)
        {
            var baseType = typeof(ObjectInHandsAnimator);
            var animatorProperty = baseType.GetProperty("Animator", BindingFlags.Instance | BindingFlags.Public);
            if (animatorProperty == null)
                return true;
            
            var animatorInstance = animatorProperty.GetValue(__instance) as IAnimator;
            if (animatorInstance == null)
                return true;
            
            int parameterHash = animatorInstance.StringToHash("Strumming");
            var hasParameterMethod = baseType.GetMethod("HasParameter", BindingFlags.Instance | BindingFlags.Public);
            if (hasParameterMethod == null)
                return true;
            
            bool hasTestParameter = (bool)hasParameterMethod.Invoke(__instance, new object[] { parameterHash });
            if (!hasTestParameter)
            {
                Logger.LogWarning("FirearmsAnimatorPatch: Object is not a guitar, patch disabled");
                return true;
            }
            
            if (!Input.GetKey(KeyCode.Mouse1))
                return true;
            
            if (Time.time - lastToggleTime < debounceDelay)
            {
                Logger.LogInfo("FirearmsAnimatorPatch Debounce in effect, skipping toggle");
                return false;
            }
            
            lastToggleTime = Time.time;

            if (WeaponAnimSpeedControllerPatch.Strumming)
                fire = false;
            
            Logger.LogInfo($"Firearms Animator SetAltFire: {fire}");

            return true;
        }
    }
}