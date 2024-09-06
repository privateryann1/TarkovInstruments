using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using System.Reflection;

namespace PrivateRyan.PlayableGuitar.Patches
{
    internal class PlayableGuitarPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.RegisterPlayer));
        }

        [PatchPostfix]
        public static void PostFix(IPlayer iPlayer)
        {
            if (iPlayer == null)
            {
                Logger.LogError("Could not add component, player was null!");
                return;
            }

            if (!iPlayer.IsYourPlayer)
            {
                return;
            }

            Singleton<GameWorld>.Instance.MainPlayer.gameObject.AddComponent<PlayableGuitarComponent>();
            Logger.LogInfo("Added PG Component to player: " + Singleton<GameWorld>.Instance.MainPlayer.Profile.Nickname);
        }
    }
}