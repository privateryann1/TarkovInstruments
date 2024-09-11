using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using PrivateRyan.PlayableGuitar.Helpers;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarMidi
    {
        private static InputDevice midiInputDevice;
        private static Playback midiPlayback;
        
        public static TinySoundFont SoundFont;
        
        private static Timer noteOffTimer;
        private static double noteOffDelay = 2000;
        public static bool NotePlaying = false;
        
        public static bool HasGuitar = false;
        public static PlayableGuitarComponent GuitarComponent;
        private static bool isSongPlaying = false;

        public PlayableGuitarMidi()
        {
            if (!Settings.UseMIDI.Value)
                return;
            
            InitializeMidi();
            InitializeSoundFont();
        }

        private void InitializeSoundFont()
        {
            string soundFontPath = Path.Combine($"{Utils.GetPluginDirectory()}/SoundFonts", Settings.SelectedSoundFont.Value);
            SoundFont = new TinySoundFont(soundFontPath);

            if (!SoundFont.IsLoaded)
            {
                PlayableGuitarPlugin.PBLogger.LogError("Failed to load SoundFont.");
            }
            else
            {
                PlayableGuitarPlugin.PBLogger.LogInfo("SoundFont loaded successfully.");
                SoundFont.SetOutput(44100, 2);
            }
        }

        private static void InitializeMidi()
        {
            if (!Settings.AutoConnectMIDI.Value)
                return;

            try
            {
                var selectedDeviceName = Settings.SelectedMIDIDevice.Value;
                midiInputDevice = InputDevice.GetByName(selectedDeviceName);

                if (midiInputDevice != null)
                {
                    midiInputDevice.EventReceived += OnMidiEventReceived;
                    midiInputDevice.StartEventsListening();

                    PlayableGuitarPlugin.PBLogger.LogInfo($"MIDI input device connected: {midiInputDevice.Name}");
                }
                else
                {
                    PlayableGuitarPlugin.PBLogger.LogWarning("No MIDI input device found.");
                }

                noteOffTimer = new Timer(noteOffDelay);
                noteOffTimer.Elapsed += ResetNotePlaying;
                noteOffTimer.AutoReset = false;
            }
            catch (Exception ex)
            {
                PlayableGuitarPlugin.PBLogger.LogError($"MIDI initialization error: {ex.Message}");
            }
        }

        private static void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (!HasGuitar || GuitarComponent == null)
                return;

            if (e.Event is NoteOnEvent noteOn)
            {
                PlayNoteForMIDI(noteOn.NoteNumber, noteOn.Velocity);
                GuitarComponent.PlayNoteTriggered(noteOn.NoteNumber, noteOn.Velocity);
            }
            else if (e.Event is NoteOffEvent noteOff)
            {
                StopNoteForMIDI(noteOff.NoteNumber);
            }
        }
        
        private static void PlayNoteForMIDI(int noteNumber, int velocity)
        {
            SoundFont.PlayNote(noteNumber, velocity / 127f);

            NotePlaying = true;
            noteOffTimer.Stop();
            noteOffTimer.Start();
        }
        
        private static void StopNoteForMIDI(int noteNumber)
        {
            SoundFont.StopNote(noteNumber);
            NotePlaying = false;
        }
        
        public static async Task PlayMidiSong()
        {
            if (isSongPlaying)
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("A song is already playing.");
                return;
            }

            string selectedSongPath = Path.Combine($"{Utils.GetPluginDirectory()}/Midi-Songs", Settings.SelectedMidiSong.Value);
            if (File.Exists(selectedSongPath))
            {
                PlayableGuitarPlugin.PBLogger.LogInfo($"Playing MIDI song: {selectedSongPath}");
                MidiFile midiFile = MidiFile.Read(selectedSongPath);
                
                midiPlayback = midiFile.GetPlayback();
                isSongPlaying = true;
                
                midiPlayback.EventPlayed += (obj, args) =>
                {
                    if (args.Event is NoteOnEvent noteOn)
                    {
                        PlayNoteForMIDI(noteOn.NoteNumber, noteOn.Velocity);
                        GuitarComponent.PlayNoteTriggered(noteOn.NoteNumber, noteOn.Velocity);
                    }
                    else if (args.Event is NoteOffEvent noteOff)
                    {
                        StopNoteForMIDI(noteOff.NoteNumber);
                    }
                };
                
                midiPlayback.Finished += (s, e) =>
                {
                    isSongPlaying = false;
                    PlayableGuitarPlugin.PBLogger.LogInfo("MIDI song playback finished.");
                };
                
                await Task.Run(() => midiPlayback.Start());
            }
            else
            {
                PlayableGuitarPlugin.PBLogger.LogWarning($"MIDI song not found: {selectedSongPath}");
            }
        }

        
        public static void StopMidiSong()
        {
            if (!isSongPlaying || midiPlayback == null)
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("No MIDI song is currently playing.");
                return;
            }

            midiPlayback.Stop();
            isSongPlaying = false;

            PlayableGuitarPlugin.PBLogger.LogInfo("MIDI song playback stopped.");
        }


        private static void ResetNotePlaying(object sender, ElapsedEventArgs e)
        {
            NotePlaying = false;
            PlayableGuitarPlugin.PBLogger.LogInfo("No note played recently, NotePlaying set to false.");
        }

        public static void ReconnectToMIDI(string selectedDeviceName)
        {
            DisposeMidiInputDevice();

            var inputDevice = InputDevice.GetByName(selectedDeviceName);
            if (inputDevice != null)
            {
                midiInputDevice = inputDevice;
                midiInputDevice.EventReceived += OnMidiEventReceived;
                midiInputDevice.StartEventsListening();

                PlayableGuitarPlugin.PBLogger.LogInfo($"Successfully reconnected to: {midiInputDevice.Name}");
            }
            else
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("Selected MIDI device not found.");
            }
        }

        private static void DisposeMidiInputDevice()
        {
            if (midiInputDevice != null)
            {
                midiInputDevice.StopEventsListening();
                midiInputDevice.Dispose();
                PlayableGuitarPlugin.PBLogger.LogInfo("MIDI input device disconnected.");
            }
        }

        public void Dispose()
        {
            DisposeMidiInputDevice();

            if (SoundFont != null)
            {
                SoundFont.Dispose();
            }

            if (noteOffTimer != null)
            {
                noteOffTimer.Stop();
                noteOffTimer.Dispose();
            }

            if (midiPlayback != null)
            {
                midiPlayback.Dispose();
            }
        }
    }
}
