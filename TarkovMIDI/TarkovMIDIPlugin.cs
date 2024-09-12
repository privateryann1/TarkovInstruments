using BepInEx;
using BepInEx.Logging;
using PrivateRyan.TarkovMIDI.Helpers;

namespace PrivateRyan.TarkovMIDI
{
    [BepInPlugin("privateryan.tarkovmidi", "TarkovMIDI", "1.0.0")]
    [BepInDependency("com.SPT.core", "3.9.0")]
    public class TarkovMIDIPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource PBLogger;
        private void Awake()
        {
            PBLogger = Logger;
            
            Settings.Init(Config);
        }
        
    }
}