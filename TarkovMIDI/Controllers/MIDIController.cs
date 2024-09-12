using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using PrivateRyan.TarkovMIDI.Helpers;
using PrivateRyan.TarkovMIDI.Interfaces;

namespace PrivateRyan.TarkovMIDI.Controllers
{
    public class MIDIController
    {
        private static InputDevice midiInputDevice;
        private static Playback midiPlayback;
        
        public static TinySoundFont SoundFont;
        
        private static Timer noteOffTimer;
        private static double noteOffDelay = 2000;
        public static bool NotePlaying = false;
        
        public static bool HasInstrument = false;
        private static bool isSongPlaying = false;
        private static bool midiDeviceConnected = false;
        
        public static IInstrumentComponent InstrumentComponent;

        public MIDIController()
        {
            if (!Settings.UseMIDI.Value)
                return;
            
            InitializeSoundFont();
            
            TryInitializeMidiDevice();
        }

        private void InitializeSoundFont()
        {
            string soundFontPath = Path.Combine($"{Utils.GetPluginDirectory()}/SoundFonts", Settings.SelectedSoundFont.Value);
            SoundFont = new TinySoundFont(soundFontPath);

            if (!SoundFont.IsLoaded)
            {
                TarkovMIDIPlugin.PBLogger.LogError("Failed to load SoundFont.");
            }
            else
            {
                TarkovMIDIPlugin.PBLogger.LogInfo("SoundFont loaded successfully.");
                SoundFont.SetOutput(44100, 2);
            }
        }

        private static void TryInitializeMidiDevice()
        {
            if (!Settings.AutoConnectMIDI.Value)
                return;

            try
            {
                var availableDevices = InputDevice.GetAll();
                if (availableDevices.Count == 0)
                {
                    midiDeviceConnected = false;
                    TarkovMIDIPlugin.PBLogger.LogWarning("No MIDI devices available. Continuing without a device.");
                    
                    noteOffTimer = new Timer(noteOffDelay);
                    noteOffTimer.Elapsed += ResetNotePlaying;
                    noteOffTimer.AutoReset = false;
                    
                    return;
                }
                
                var selectedDeviceName = Settings.SelectedMIDIDevice.Value;
                
                midiInputDevice = InputDevice.GetByName(selectedDeviceName);
                if (midiInputDevice != null)
                {
                    midiInputDevice.EventReceived += OnMidiEventReceived;
                    midiInputDevice.StartEventsListening();
                    midiDeviceConnected = true;
                    TarkovMIDIPlugin.PBLogger.LogInfo($"MIDI input device connected: {midiInputDevice.Name}");
                }
                else
                {
                    midiDeviceConnected = false;
                    TarkovMIDIPlugin.PBLogger.LogWarning($"Selected MIDI input device '{selectedDeviceName}' not found. Continuing without a device.");
                }
                
                noteOffTimer = new Timer(noteOffDelay);
                noteOffTimer.Elapsed += ResetNotePlaying;
                noteOffTimer.AutoReset = false;
            }
            catch (Exception ex)
            {
                midiDeviceConnected = false;
                TarkovMIDIPlugin.PBLogger.LogError($"MIDI initialization error: {ex.Message}");
            }
        }

        private static void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (!HasInstrument)
                return;

            if (e.Event is NoteOnEvent noteOn)
            {
                PlayNoteForMIDI(noteOn.NoteNumber, noteOn.Velocity);
                InstrumentComponent.PlayNoteTriggered(noteOn.NoteNumber, noteOn.Velocity);
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
                TarkovMIDIPlugin.PBLogger.LogWarning("A song is already playing.");
                return;
            }

            TarkovMIDIPlugin.PBLogger.LogWarning("Attempting to play song");

            string selectedSongPath = Path.Combine($"{Utils.GetPluginDirectory()}/Midi-Songs", Settings.SelectedMidiSong.Value);
            if (File.Exists(selectedSongPath))
            {
                TarkovMIDIPlugin.PBLogger.LogInfo($"Playing MIDI song: {selectedSongPath}");
                MidiFile midiFile = MidiFile.Read(selectedSongPath);
        
                midiPlayback = midiFile.GetPlayback();
                isSongPlaying = true;
                
                midiPlayback.EventPlayed += (obj, args) =>
                {
                    if (args.Event is NoteOnEvent noteOn)
                    {
                        PlayNoteForMIDI(noteOn.NoteNumber, noteOn.Velocity);
                        InstrumentComponent.PlayNoteTriggered(noteOn.NoteNumber, noteOn.Velocity);
                    }
                    else if (args.Event is NoteOffEvent noteOff)
                    {
                        StopNoteForMIDI(noteOff.NoteNumber);
                    }
                };
        
                midiPlayback.Finished += (s, e) =>
                {
                    isSongPlaying = false;
                    TarkovMIDIPlugin.PBLogger.LogInfo("MIDI song playback finished.");
                };

                TarkovMIDIPlugin.PBLogger.LogWarning("Midi Playback Start");
        
                await Task.Run(() => midiPlayback.Start());
            }
            else
            {
                TarkovMIDIPlugin.PBLogger.LogWarning($"MIDI song not found: {selectedSongPath}");
            }
        }


        public static void StopMidiSong()
        {
            if (!isSongPlaying || midiPlayback == null)
            {
                TarkovMIDIPlugin.PBLogger.LogWarning("No MIDI song is currently playing.");
                return;
            }

            midiPlayback.Stop();
            isSongPlaying = false;

            TarkovMIDIPlugin.PBLogger.LogInfo("MIDI song playback stopped.");
        }

        private static void ResetNotePlaying(object sender, ElapsedEventArgs e)
        {
            NotePlaying = false;
            TarkovMIDIPlugin.PBLogger.LogInfo("No note played recently, NotePlaying set to false.");
        }

        public static void ReconnectToMIDI(string selectedDeviceName)
        {
            DisposeMidiInputDevice();

            midiDeviceConnected = false;
            TryInitializeMidiDevice();

            if (midiDeviceConnected)
            {
                TarkovMIDIPlugin.PBLogger.LogInfo($"Successfully reconnected to: {midiInputDevice.Name}");
            }
            else
            {
                TarkovMIDIPlugin.PBLogger.LogWarning("Reconnection failed, continuing without MIDI device.");
            }
        }

        private static void DisposeMidiInputDevice()
        {
            if (midiInputDevice != null)
            {
                midiInputDevice.StopEventsListening();
                midiInputDevice.Dispose();
                midiInputDevice = null;
                midiDeviceConnected = false;

                TarkovMIDIPlugin.PBLogger.LogInfo("MIDI input device disconnected.");
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
