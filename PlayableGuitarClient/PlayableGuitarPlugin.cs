using BepInEx;
using BepInEx.Logging;
using PrivateRyan.PlayableGuitar.Helpers;
using PrivateRyan.PlayableGuitar.Patches;

namespace PrivateRyan.PlayableGuitar
{
    [BepInPlugin("privateryan.playableguitar", "PlayableGuitar", "1.1.0")]
    [BepInDependency("com.SPT.core", "3.9.0")]
    [BepInDependency("privateryan.tarkovmidi", "1.0.0")]
    public class PlayableGuitarPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource PBLogger;
        private void Awake()
        {
            PBLogger = Logger;
            
            Settings.Init(Config);
            
            new WeaponAnimSpeedControllerPatch().Enable();
            new PlayableGuitarPatch().Enable();
        }
        
    }
}

// TODO:
// Instead of playing each note event on a single audio clip,
// Use an AudioSource to continuously stream the audio.
// Hopefully this stops the crashing? Otherwise the issue might be
// with TinySoundFont, which could be a pain to figure out