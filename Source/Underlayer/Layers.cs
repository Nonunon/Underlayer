using System.Collections.Generic;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Internal;
using UnityEngine;
using Verse;

namespace Underlayer
{
    public static class Layers
    {
        internal static readonly List<(ImGuiWindowPtr Window, Window Host)> Claims = new List<(ImGuiWindowPtr, Window)>();
        static int lastClaimFrame = -10;

        public static bool Available => !Backend.Broken;

        public static void Claim(Window host)
        {
            if (host == null || Backend.Broken || !InFrame())
                return;
            ImGuiWindowPtr window = ImGui.Internal.GetCurrentWindow();
            if (window.IsNull())
                return;
            ImGuiWindowPtr root = window.RootWindow;
            if (root.IsNull())
                root = window;
            root.Flags |= ImGuiWindowFlags.NoBringToFrontOnFocus;
            foreach (var claim in Claims)
                if (Same(claim.Window, root))
                    return;
            Claims.Add((root, host));
            lastClaimFrame = ImGui.GetFrameCount();
        }

        public static string ProxyName(Window window) => Proxies.NameOf(window);

        internal static bool Engaged => ImGui.GetFrameCount() - lastClaimFrame <= 1;

        internal static bool InFrame()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint)
                return false;
            ImGuiContextPtr context = ImGui.GetCurrentContext();
            return !context.IsNull() && context.WithinFrameScope;
        }

        internal static unsafe bool Same(ImGuiWindowPtr a, ImGuiWindowPtr b) => (ImGuiWindow*)a == (ImGuiWindow*)b;
    }
}
