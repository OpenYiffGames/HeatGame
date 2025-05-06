namespace PatreonPatcher.Core;

internal class Constants
{
    public static class Directories
    {
        public const string AssembliesDirectory = "_Data/Managed";
    }

    public static class UnityEngineTypes
    {
        public const string PlayerPrefs = "PlayerPrefs";
        public const string PlayerPrefs_HasKey = "HasKey";

        public const string MonoBehaviour = "MonoBehaviour";
        public const string MonoBehaviour_Awake = "Awake";
    }

    public static class Patterns
    {
        public const string WriteAuthFunction = "02 7B ?? ?? ?? 04 28 {token} 2C 0C 72 {string_token} 28 {token} 2D 52";
        public const string InvokeSuccessFunction = "02 7B ?? ?? ?? ?? 7B ?? ?? ?? ?? 28 ?? ?? ?? 0A 2D 27 02 7B ?? ?? ?? ?? 72 ?? ?? ?? ?? 02 7B ?? ?? ?? ?? 7B ?? ?? ?? ?? 72 ?? ?? ?? ?? 28 ?? ?? ?? ?? 7D ?? ?? ?? ?? 2B 10 02 7B ?? ?? ?? ?? 72 ?? ?? ?? ?? 7D ?? ?? ?? ?? 02 7B ?? ?? ?? ?? 2C 0C";
        public const string BypassAuthFunction = "02 28 ?? ?? ?? 06 02 28 ?? ?? ?? 06 2A";
    }

    public const string DefaultLogFileName = "Log.txt";

    public const string UnityEngineAssembly = "UnityEngine.dll";
    public const string UnityPlayerAssembly = "UnityPlayer.dll";

    public const string PatchAttributeNamespace = "PatreonPatcher.Patch";
    public const string PatchAttributeTypeName = "PatchAttribute";
}
