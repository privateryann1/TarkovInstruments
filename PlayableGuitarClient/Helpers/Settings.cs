using BepInEx.Configuration;
using System.Collections.Generic;

namespace PrivateRyan.PlayableGuitar.Helpers
{
    internal class Settings
    {
        public const string GeneralSectionTitle = "1. General";

        public static ConfigFile Config;

        public static ConfigEntry<bool> SomeSetting;

        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public static void Init(ConfigFile Config)
        {
            Settings.Config = Config;

            ConfigEntries.Add(SomeSetting = Config.Bind(
                GeneralSectionTitle,
                "Some Setting",
                true,  // Default value
                new ConfigDescription(
                    "Some description", 
                    null,
                    new ConfigurationManagerAttributes { Order = 0 }
                )));

            RecalcOrder();
        }

        private static void RecalcOrder()
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
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