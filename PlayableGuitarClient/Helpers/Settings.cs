using BepInEx.Configuration;
using System.Collections.Generic;

namespace PrivateRyan.PlayableGuitar.Helpers
{
    internal class Settings
    {
        public const string GeneralSectionTitle = "1. General";

        public static ConfigFile Config;

        // Settings
        public static ConfigEntry<bool> ASetting;

        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public static void Init(ConfigFile config)
        {
            Settings.Config = config;

            // Auto connect setting
            ConfigEntries.Add(ASetting = Config.Bind(
                GeneralSectionTitle,
                "Some Setting",
                false,  // Default value
                new ConfigDescription(
                    "Description", 
                    null,
                    new ConfigurationManagerAttributes { Order = 0 }
                )));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            int settingOrder = ConfigEntries.Count;
            foreach (var entry in ConfigEntries)
            {
                ConfigurationManagerAttributes attributes = entry.Description.Tags[0] as ConfigurationManagerAttributes;
                if (attributes != null)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }
}
