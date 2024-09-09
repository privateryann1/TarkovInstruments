using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Melanchall.DryWetMidi.Multimedia; // DryWetMIDI for device management

namespace PrivateRyan.PlayableGuitar.Helpers
{
    internal class Settings
    {
        public const string GeneralSectionTitle = "1. MIDI Settings";

        public static ConfigFile Config;

        // Settings
        public static ConfigEntry<bool> AutoConnectMIDI;
        public static ConfigEntry<string> SelectedMIDIDevice;
        public static ConfigEntry<bool> ReconnectMIDI;
        public static ConfigEntry<string> SelectedMidiSong;  // Select MIDI song
        public static ConfigEntry<UnityEngine.KeyCode> PlayMidiKey;  // Key to play the selected song

        public static List<ConfigEntryBase> ConfigEntries = new List<ConfigEntryBase>();

        public static void Init(ConfigFile config)
        {
            Settings.Config = config;

            // Auto connect setting
            ConfigEntries.Add(AutoConnectMIDI = Config.Bind(
                GeneralSectionTitle,
                "Auto Connect",
                true,  // Default value
                new ConfigDescription(
                    "Auto connect to the MIDI device", 
                    null,
                    new ConfigurationManagerAttributes { Order = 0 }
                )));

            // MIDI device selection dropdown (using DryWetMIDI)
            var midiDevices = InputDevice.GetAll()
                                         .Select(device => device.Name)
                                         .ToArray();

            ConfigEntries.Add(SelectedMIDIDevice = Config.Bind(
                GeneralSectionTitle,
                "MIDI Device",
                midiDevices.FirstOrDefault(),  // Default to the first available device
                new ConfigDescription(
                    "Select the MIDI device to use",
                    null,
                    new ConfigurationManagerAttributes { Order = 1, CustomDrawer = DrawMIDIDeviceSelection }
                )));

            // Reconnect button
            ConfigEntries.Add(ReconnectMIDI = Config.Bind(
                GeneralSectionTitle,
                "Reconnect MIDI",
                false,  // This will act as a button, not a persistent value
                new ConfigDescription(
                    "Press to reconnect the selected MIDI device",
                    null,
                    new ConfigurationManagerAttributes { Order = 2, CustomDrawer = DrawReconnectButton }
                )));

            // Select MIDI Song
            var midiSongs = Directory.GetFiles($"{Utils.GetPluginDirectory()}/Midi-Songs", "*.mid")
                                     .Select(Path.GetFileName)
                                     .ToArray();

            ConfigEntries.Add(SelectedMidiSong = Config.Bind(
                GeneralSectionTitle,
                "MIDI Song",
                midiSongs.FirstOrDefault(), // Default to the first available song
                new ConfigDescription(
                    "Select the MIDI song to play",
                    null,
                    new ConfigurationManagerAttributes { Order = 3, CustomDrawer = DrawMidiSongSelection }
                )));

            // Key binding to play the selected song
            ConfigEntries.Add(PlayMidiKey = Config.Bind(
                GeneralSectionTitle,
                "Play MIDI Song Key",
                UnityEngine.KeyCode.P,  // Default key is P
                new ConfigDescription(
                    "Assign a key to play the selected MIDI song",
                    null,
                    new ConfigurationManagerAttributes { Order = 4 }
                )));

            RecalcOrder();
        }

        // Custom drawer for MIDI song selection dropdown
        private static void DrawMidiSongSelection(ConfigEntryBase entry)
        {
            var midiSongs = Directory.GetFiles($"{Utils.GetPluginDirectory()}/Midi-Songs", "*.mid")
                                     .Select(Path.GetFileName)
                                     .ToArray();

            ConfigEntry<string> songEntry = (ConfigEntry<string>)entry;
            int selectedIndex = System.Array.IndexOf(midiSongs, songEntry.Value);
            if (selectedIndex == -1)
                selectedIndex = 0;

            selectedIndex = UnityEngine.GUILayout.SelectionGrid(selectedIndex, midiSongs, 1);
            songEntry.Value = midiSongs[selectedIndex];
        }

        // Custom drawer for MIDI device selection dropdown
        private static void DrawMIDIDeviceSelection(ConfigEntryBase entry)
        {
            var midiDevices = InputDevice.GetAll()
                                         .Select(device => device.Name)
                                         .ToArray();

            ConfigEntry<string> deviceEntry = (ConfigEntry<string>)entry;
            int selectedIndex = System.Array.IndexOf(midiDevices, deviceEntry.Value);
            if (selectedIndex == -1)
                selectedIndex = 0;

            selectedIndex = UnityEngine.GUILayout.SelectionGrid(selectedIndex, midiDevices, 1);
            deviceEntry.Value = midiDevices[selectedIndex];
        }

        // Custom drawer for reconnect button
        private static void DrawReconnectButton(ConfigEntryBase entry)
        {
            if (UnityEngine.GUILayout.Button("Reconnect MIDI Device"))
            {
                ReconnectMIDI.Value = true;
                PlayableGuitarMidi.ReconnectToMIDI(SelectedMIDIDevice.Value);
            }
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
