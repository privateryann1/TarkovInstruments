using System;
using System.IO;
using System.Threading.Tasks;
using System.Timers;
using ManagedBass;
using ManagedBass.Midi;
using PrivateRyan.PlayableGuitar.Helpers;

namespace PrivateRyan.PlayableGuitar
{
    internal class PlayableGuitarMidi : IDisposable
    {
        private static int midiStreamHandle; // Handle for Fluidsynth MIDI stream
        private static int soundFontHandle; // Handle for loaded soundfont
        private static Timer noteOffTimer;
        private static double noteOffDelay = 2000; // Time for note-off delay
        private static MidiInProcedure midiInCallback;

        public static bool NotePlaying = false;
        public static bool HasGuitar = false;
        private static bool isSongPlaying = false;

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
                // Initialize BASS
                if (!Bass.Init())
                {
                    PlayableGuitarPlugin.PBLogger.LogError("Failed to initialize BASS.");
                    return;
                }

                // Load the soundfont from settings
                string soundFontPath = Path.Combine(Utils.GetPluginDirectory(), "SoundFonts", Settings.SelectedSoundFont.Value);
                soundFontHandle = BassMidi.FontInit(soundFontPath, FontInitFlags.Unicode);
                if (soundFontHandle == 0)
                {
                    PlayableGuitarPlugin.PBLogger.LogError("Failed to load soundfont.");
                    return;
                }

                MidiFont[] fonts = BassMidi.StreamGetFonts(soundFontHandle);
                BassMidi.StreamSetFonts(soundFontHandle, fonts, 1); // Apply the soundfont to all MIDI streams

                // Create a MIDI stream for real-time note playback
                midiStreamHandle = BassMidi.CreateStream(16, BassFlags.Default, 44100);
                if (midiStreamHandle == 0)
                {
                    PlayableGuitarPlugin.PBLogger.LogError("Failed to create MIDI stream.");
                    return;
                }

                // Initialize the note-off timer
                noteOffTimer = new Timer(noteOffDelay);
                noteOffTimer.Elapsed += ResetNotePlaying;
                noteOffTimer.AutoReset = false;

                // Connect to the selected MIDI device
                ReconnectToMIDI(Settings.SelectedMIDIDevice.Value);
            }
            catch (Exception ex)
            {
                PlayableGuitarPlugin.PBLogger.LogError($"Error initializing MIDI: {ex.Message}");
            }
        }

        // Play a MIDI note using Fluidsynth
        private static void PlayNoteForMIDI(int noteNumber, int velocity)
        {
            if (!HasGuitar)
                return;

            PlayableGuitarPlugin.PBLogger.LogInfo($"Playing MIDI note: {noteNumber}");

            // Send a note-on event to the MIDI stream
            BassMidi.StreamEvent(midiStreamHandle, 0, MidiEventType.Note, noteNumber | (velocity << 8));

            NotePlaying = true;
            noteOffTimer.Stop();
            noteOffTimer.Start();
        }

        // Play the selected MIDI song using Fluidsynth
        public static async Task PlayMidiSong()
        {
            if (isSongPlaying)
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("A song is already playing.");
                return;
            }

            string midiSongPath = Path.Combine(Utils.GetPluginDirectory(), "Midi-Songs", Settings.SelectedMidiSong.Value);
            if (!File.Exists(midiSongPath))
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("MIDI song file not found.");
                return;
            }

            PlayableGuitarPlugin.PBLogger.LogInfo($"Playing MIDI song: {midiSongPath}");

            // Play the MIDI file using Fluidsynth
            Bass.StreamFree(midiStreamHandle);
            midiStreamHandle = BassMidi.CreateStream(midiSongPath, 0, 0, BassFlags.Default);
            if (midiStreamHandle == 0)
            {
                PlayableGuitarPlugin.PBLogger.LogError("Failed to play MIDI song.");
                return;
            }

            isSongPlaying = true;
            Bass.ChannelPlay(midiStreamHandle);

            await Task.CompletedTask;
        }

        // Stop the currently playing MIDI song
        public static void StopMidiSong()
        {
            if (!isSongPlaying)
            {
                PlayableGuitarPlugin.PBLogger.LogWarning("No song is currently playing.");
                return;
            }

            PlayableGuitarPlugin.PBLogger.LogInfo("Stopping the MIDI song.");
            Bass.ChannelStop(midiStreamHandle);
            isSongPlaying = false;
            NotePlaying = false;
        }

        // Reconnect to the MIDI device by its device ID
        public static void ReconnectToMIDI(int selectedDeviceIndex)
        {
            if (BassMidi.InStop(selectedDeviceIndex))
            {
                BassMidi.InFree(selectedDeviceIndex);
            }

            // Define the MIDI input callback to handle incoming MIDI events
            midiInCallback = new MidiInProcedure(MidiDataReceived);

            // Initialize the selected MIDI device with the callback
            if (!BassMidi.InInit(selectedDeviceIndex, midiInCallback, IntPtr.Zero))
            {
                PlayableGuitarPlugin.PBLogger.LogError("Failed to initialize the selected MIDI device.");
                return;
            }

            // Start receiving MIDI events
            if (!BassMidi.InStart(selectedDeviceIndex))
            {
                PlayableGuitarPlugin.PBLogger.LogError($"Failed to start the MIDI input device. Error: {Bass.LastError}");
            }
        }

        // Corrected callback function to process MIDI data
        private static void MidiDataReceived(int device, double time, IntPtr buffer, int length, IntPtr user)
        {
            // Parse the MIDI data from the callback and trigger appropriate events
            byte[] midiData = new byte[length];
            System.Runtime.InteropServices.Marshal.Copy(buffer, midiData, 0, length);

            // Example: Handling Note On and Off events
            byte statusByte = midiData[0];
            byte note = midiData[1];
            byte velocity = midiData[2];

            // Check for Note On (statusByte & 0xF0 == 0x90)
            if ((statusByte & 0xF0) == 0x90 && velocity > 0)
            {
                PlayNoteForMIDI(note, velocity);
            }
            // Check for Note Off (statusByte & 0xF0 == 0x80) or Note On with velocity 0
            else if ((statusByte & 0xF0) == 0x80 || ((statusByte & 0xF0) == 0x90 && velocity == 0))
            {
                StopNoteForMIDI(note);
            }
        }

        // Stop playing a MIDI note (note-off logic)
        private static void StopNoteForMIDI(int noteNumber)
        {
            PlayableGuitarPlugin.PBLogger.LogInfo($"Stopping MIDI note: {noteNumber}");
            BassMidi.StreamEvent(midiStreamHandle, 0, MidiEventType.NotesOff, noteNumber);
        }

        // Reset the note-playing state after the note-off delay
        private static void ResetNotePlaying(object sender, ElapsedEventArgs e)
        {
            PlayableGuitarPlugin.PBLogger.LogInfo("NoteOff triggered, stopping note.");
            NotePlaying = false;
            BassMidi.StreamEvent(midiStreamHandle, 0, MidiEventType.NotesOff, 0);
        }

        public void Dispose()
        {
            if (noteOffTimer != null)
            {
                noteOffTimer.Stop();
                noteOffTimer.Dispose();
            }

            Bass.Free();
            PlayableGuitarPlugin.PBLogger.LogInfo("MIDI system disposed.");
        }
    }
}
