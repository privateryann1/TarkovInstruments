using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;
using PrivateRyan.PlayableGuitar.Helpers;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarMidi
    {
        private static InputDevice midiInputDevice; // DryWetMIDI for MIDI input
        private static OutputDevice midiOutputDevice; // DryWetMIDI for MIDI output
        private static Playback midiPlayback; // DryWetMIDI for song playback
        
        public static bool HasGuitar = false;
        public static bool NotePlaying = false;
        private static bool isSongPlaying = false;

        private static Timer noteOffTimer;
        private static double noteOffDelay = 2000;
        
        public PlayableGuitarMidi()
        {
            InitializeMidi();
        }
        
        private static void InitializeMidi()
        {
            if (!Settings.AutoConnectMIDI.Value)
                return;

            try
            {
                // Get the first available MIDI input device
                var selectedDeviceName = Settings.SelectedMIDIDevice.Value;
                midiInputDevice = InputDevice.GetByName(selectedDeviceName);
                
                if (midiInputDevice != null)
                {
                    midiInputDevice.EventReceived += OnMidiEventReceived;  // Hook into MIDI input event listener
                    midiInputDevice.StartEventsListening(); // Start listening for MIDI input

                    PlayableGuitarPlugin.PBLogger.LogInfo($"MIDI input device connected: {midiInputDevice.Name}");
                }
                else
                {
                    PlayableGuitarPlugin.PBLogger.LogWarning("No MIDI input device found.");
                }

                // Initialize MIDI output device (e.g., Microsoft GS Wavetable Synth)
                midiOutputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
                midiOutputDevice.SendEvent(new ProgramChangeEvent((SevenBitNumber)24));
                PlayableGuitarPlugin.PBLogger.LogInfo("MIDI output initialized with Acoustic Guitar sound.");

                // Initialize the timer to handle note-off logic
                noteOffTimer = new Timer(noteOffDelay);
                noteOffTimer.Elapsed += ResetNotePlaying;
                noteOffTimer.AutoReset = false; // Only trigger once after delay
            }
            catch (Exception ex)
            {
                PlayableGuitarPlugin.PBLogger.LogError($"MIDI initialization error: {ex.Message}");
            }
        }

        // Handle MIDI input messages
        private static void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (!HasGuitar)
                return;
            
            var midiEvent = e.Event as NoteOnEvent;
            if (midiEvent != null)
            {
                PlayNoteForMIDI(midiEvent.NoteNumber, midiEvent.Velocity);
            }
        }

        // Play the corresponding note using system MIDI output
        private static void PlayNoteForMIDI(int noteNumber, int velocity)
        {
            // Send the note-on message to play the acoustic guitar sound
            PlayableGuitarPlugin.PBLogger.LogInfo($"Playing MIDI note: {noteNumber}");
            midiOutputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)noteNumber, (SevenBitNumber)velocity));

            // Set NotePlaying to true
            NotePlaying = true;

            // Reset and start the timer for note-off logic
            noteOffTimer.Stop();  // Stop if it's already running
            noteOffTimer.Start(); // Start (or restart) the timer
        }
        
        // Use DryWetMIDI to play the selected MIDI song
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

                // Use DryWetMIDI to read and play the MIDI file
                MidiFile midiFile = MidiFile.Read(selectedSongPath);
                midiPlayback = midiFile.GetPlayback(midiOutputDevice); // Use DryWetMIDI's OutputDevice for output
                isSongPlaying = true;
                NotePlaying = true;

                // Hook up the Finished event to stop the song when it's done
                midiPlayback.Finished += (s, e) => StopMidiSong();
                
                // Start the song playback (asynchronously by wrapping in Task)
                await Task.Run(() => midiPlayback.Start());
            }
            else
            {
                PlayableGuitarPlugin.PBLogger.LogWarning($"MIDI song not found: {selectedSongPath}");
            }
        }

        // Stop the MIDI song by stopping the DryWetMIDI playback
        public static void StopMidiSong()
        {
            if (!isSongPlaying || midiPlayback == null)
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("No song is currently playing.");
                return;
            }

            PlayableGuitarPlugin.PBLogger.LogInfo("Stopping the MIDI song.");
            midiPlayback.Stop();
            isSongPlaying = false;
            NotePlaying = false;
        }

        // This method will be called after the timer elapses
        private static void ResetNotePlaying(object sender, ElapsedEventArgs e)
        {
            NotePlaying = false;
            PlayableGuitarPlugin.PBLogger.LogInfo("No note played recently, NotePlaying set to false.");
        }
        
        public static void ReconnectToMIDI(string selectedDeviceName)
        {
            // Dispose of the current MIDI input device if it's already connected
            DisposeMidiInputDevice();

            // Find the new MIDI input device by name
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


        // Dispose of the current MIDI input device
        private static void DisposeMidiInputDevice()
        {
            if (midiInputDevice != null)
            {
                midiInputDevice.StopEventsListening();
                midiInputDevice.Dispose();
                PlayableGuitarPlugin.PBLogger.LogInfo("MIDI input device disconnected.");
            }
        }

        // Stop listening for MIDI events when you're done
        public void Dispose()
        {
            DisposeMidiInputDevice();

            if (midiOutputDevice != null)
            {
                midiOutputDevice.Dispose();
                PlayableGuitarPlugin.PBLogger.LogInfo("MIDI output device disconnected.");
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
