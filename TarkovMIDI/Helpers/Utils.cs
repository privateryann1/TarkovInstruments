using System.IO;
using System.Reflection;
using EFT;

namespace PrivateRyan.TarkovMIDI.Helpers
{
    internal class Utils
    {
        public static string GetPluginDirectory()
        {
            // Get the path of the currently executing assembly (your plugin DLL)
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation);
        }
    }
}