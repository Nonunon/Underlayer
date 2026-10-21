using HarmonyLib;
using UnityEngine;
using Verse;

namespace Underlayer
{
    public class UnderlayerMod : Mod
    {
        public UnderlayerMod(ModContentPack content) : base(content)
        {
            var harmony = new Harmony("Unon.Underlayer");
            harmony.Patch(AccessTools.Method(typeof(Window), "InnerWindowOnGUI"),
                prefix: new HarmonyMethod(typeof(Proxies), nameof(Proxies.Prefix)),
                finalizer: new HarmonyMethod(typeof(Proxies), nameof(Proxies.Finalizer)));
            harmony.Patch(AccessTools.Method(typeof(GUI), nameof(GUI.ProcessPendingWindows)),
                postfix: new HarmonyMethod(typeof(Ordering), nameof(Ordering.Postfix)));
            Log.Message($"[Underlayer] {typeof(UnderlayerMod).Assembly.GetName().Version.ToString(3)} loaded");
        }
    }
}
