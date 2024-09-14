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
        private InputDevice midiInputDevice;
        private Playback midiPlayback;
        
        public TinySoundFont SoundFont;
        
        private Timer noteOffTimer;
        private double noteOffDelay = 2000;
        public bool NotePlaying = false;
        
        public bool HasInstrument = false;
        public bool SongPlaying = false;
        private bool midiDeviceConnected = false;
        
        public IInstrumentComponent InstrumentComponent;

        public MIDIController(IInstrumentComponent instrumentComponent)
        {
            if (!Settings.UseMIDI.Value)
                return;
         
            InstrumentComponent = instrumentComponent;
            
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

        private void TryInitializeMidiDevice()
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

        private void OnMidiEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (!HasInstrument)
                return;

            if (e.Event is NoteOnEvent noteOn)
            {
                PlayNoteForMIDI(noteOn.NoteNumber, noteOn.Velocity);
                InstrumentComponent.PlayNoteTriggered(noteOn.NoteNumber, noteOn.Velocity);

                NotePlaying = true;
                noteOffTimer.Stop();
                noteOffTimer.Start();
            }
            else if (e.Event is NoteOffEvent noteOff)
            {
                StopNoteForMIDI(noteOff.NoteNumber);
                NotePlaying = false;
                noteOffTimer.Stop();
            }
        }

        private void PlayNoteForMIDI(int noteNumber, int velocity)
        {
            SoundFont.PlayNote(noteNumber, velocity / 127f);
        }

        private void StopNoteForMIDI(int noteNumber)
        {
            SoundFont.StopNote(noteNumber);
        }

        public async Task PlayMidiSong()
        {
            if (SongPlaying)
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
                SongPlaying = true;
                
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
                    SongPlaying = false;
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


        public void StopMidiSong()
        {
            if (!SongPlaying || midiPlayback == null)
            {
                TarkovMIDIPlugin.PBLogger.LogWarning("No MIDI song is currently playing.");
                return;
            }

            midiPlayback.Stop();
            SongPlaying = false;

            TarkovMIDIPlugin.PBLogger.LogInfo("MIDI song playback stopped.");
        }

        private void ResetNotePlaying(object sender, ElapsedEventArgs e)
        {
            NotePlaying = false;
            TarkovMIDIPlugin.PBLogger.LogInfo("No note played recently, NotePlaying set to false.");
        }

        public void ReconnectToMIDI(string selectedDeviceName)
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

        private void DisposeMidiInputDevice()
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

            SoundFont?.Dispose();
            //noteOffTimer?.Stop();
            //noteOffTimer?.Dispose();
            midiPlayback?.Dispose();
        }
        
    }

}
