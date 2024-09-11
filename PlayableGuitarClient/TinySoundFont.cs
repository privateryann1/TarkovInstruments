using System;
using System.Runtime.InteropServices;

public class TinySoundFont
{
    private IntPtr soundFont;

    [DllImport("tinysoundfont.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr tsf_load_filename([MarshalAs(UnmanagedType.LPStr)] string filename);

    [DllImport("tinysoundfont.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tsf_set_output(IntPtr soundFont, int mode, int samplerate, int channels);

    [DllImport("tinysoundfont.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tsf_note_on(IntPtr soundFont, int presetIndex, int key, float velocity);

    [DllImport("tinysoundfont.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tsf_note_off(IntPtr soundFont, int presetIndex, int key);

    [DllImport("tinysoundfont.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tsf_render_float(IntPtr soundFont, float[] outputBuffer, int samples);

    [DllImport("tinysoundfont.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern void tsf_close(IntPtr soundFont);

    public TinySoundFont(string soundFontPath)
    {
        soundFont = tsf_load_filename(soundFontPath);
        if (soundFont == IntPtr.Zero)
        {
            throw new Exception("Failed to load SoundFont.");
        }
    }

    public bool IsLoaded => soundFont != IntPtr.Zero;

    public void SetOutput(int samplerate, int channels)
    {
        tsf_set_output(soundFont, 0, samplerate, channels);
    }

    public void PlayNote(int key, float velocity)
    {
        tsf_note_on(soundFont, 0, key, velocity);
    }

    public void StopNote(int key)
    {
        tsf_note_off(soundFont, 0, key);
    }

    public void RenderAudio(float[] buffer)
    {
        tsf_render_float(soundFont, buffer, buffer.Length / 2);
    }

    public void Dispose()
    {
        if (soundFont != IntPtr.Zero)
        {
            tsf_close(soundFont);
            soundFont = IntPtr.Zero;
        }
    }
}