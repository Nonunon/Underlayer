using System;
using System.Collections.Generic;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Internal;
using Brutal.Numerics;
using Verse;

namespace Underlayer
{
    internal static class Proxies
    {
        const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoNav;

        public static readonly Dictionary<Window, ImGuiWindowPtr> Drawn = new Dictionary<Window, ImGuiWindowPtr>();
        static readonly Dictionary<Window, int> slotOf = new Dictionary<Window, int>();
        static readonly List<Window> owners = new List<Window>();
        static readonly List<int> drawnFrame = new List<int>();
        static readonly List<string> names = new List<string>();

        public class Session
        {
            public ImDrawListPtr Background, Proxy;
            public int ClipDepth;
        }

        public static string NameOf(Window window) => window != null && slotOf.TryGetValue(window, out int slot) ? names[slot] : null;

        public static void Prefix(Window __instance, out Session __state)
        {
            __state = null;
            try
            {
                if (!Layers.InFrame() || !Layers.Engaged || !Backend.Bind() || !Backend.CanDraw)
                    return;
                __state = Open(__instance);
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Underlayer] " + e, 0x5A1D01);
            }
        }

        public static void Finalizer(Session __state)
        {
            if (__state == null)
                return;
            int depth = Backend.ClipDepth;
            while (depth > __state.ClipDepth)
            {
                __state.Proxy.PopClipRect();
                depth--;
            }
            Backend.ClipDepth = depth;
            Backend.DrawList = __state.Background;
            __state.Proxy.PopClipRect();
            ImGui.End();
        }

        static Session Open(Window window)
        {
            int frame = ImGui.GetFrameCount();
            int slot = SlotFor(window);
            var session = new Session { Background = Backend.DrawList };
            float2 min = session.Background.GetClipRectMin();
            float2 max = session.Background.GetClipRectMax();
            if (drawnFrame[slot] != frame)
            {
                drawnFrame[slot] = frame;
                ImGui.SetNextWindowPos(min, ImGuiCond.Always);
                ImGui.SetNextWindowSize(new float2(Math.Max(0f, max.X - min.X), Math.Max(0f, max.Y - min.Y)), ImGuiCond.Always);
            }
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new float2(0f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.Begin(names[slot], Flags);
            ImGui.PopStyleVar(2);
            Drawn[window] = ImGui.Internal.GetCurrentWindow();
            session.Proxy = ImGui.GetWindowDrawList();
            session.Proxy.PushClipRect(min, max, false);
            session.ClipDepth = Backend.ClipDepth;
            Backend.DrawList = session.Proxy;
            return session;
        }

        static int SlotFor(Window window)
        {
            if (slotOf.TryGetValue(window, out int slot))
                return slot;
            slot = owners.IndexOf(null);
            if (slot < 0)
            {
                slot = owners.Count;
                owners.Add(null);
                drawnFrame.Add(-1);
                names.Add("##Underlayer.Proxy" + slot);
            }
            owners[slot] = window;
            slotOf[window] = slot;
            return slot;
        }

        public static void EndFrame(int frame)
        {
            Drawn.Clear();
            for (int slot = 0; slot < owners.Count; slot++)
            {
                if (owners[slot] != null && drawnFrame[slot] != frame)
                {
                    slotOf.Remove(owners[slot]);
                    owners[slot] = null;
                }
            }
        }
    }
}
