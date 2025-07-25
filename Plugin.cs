using BepInEx;
using HarmonyLib;

namespace BskyRoyale {
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin {
        public const string GUID = "BskyRoyale";
        public const string NAME = "BskyRoyale";
        public const string VERSION = "1.0.0";

        private readonly Harmony harmony = new Harmony(GUID);

        public Plugin() {
            BskyPatch.Enabled = Config.Bind("API", "Enabled", true).Value;
        }

        public void Awake() {
            harmony.PatchAll(typeof(BskyPatch));
        }
    }
}
