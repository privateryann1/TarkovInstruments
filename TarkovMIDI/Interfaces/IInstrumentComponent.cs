namespace PrivateRyan.TarkovMIDI.Interfaces
{
    public interface IInstrumentComponent
    {
        void PlayNoteTriggered(int note, int velocity);
        void StopNoteTriggered(int note);
    }
}