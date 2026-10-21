using System;
using System.Collections.Generic;
using Brutal.ImGuiApi;
using Brutal.ImGuiApi.Internal;
using Brutal.Numerics;
using UnityEngine;
using Verse;

namespace Underlayer
{
    internal static class Ordering
    {
        const ImGuiWindowFlags AlwaysOnTop = ImGuiWindowFlags.Popup | ImGuiWindowFlags.Tooltip | ImGuiWindowFlags.ChildMenu | ImGuiWindowFlags.Modal;
        const ImGuiWindowFlags BlockerFlags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoBringToFrontOnFocus
            | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoNav;
        const string BlockerName = "##Underlayer.Blocker";

        static readonly List<ImGuiWindowPtr> stacked = new List<ImGuiWindowPtr>();
        static readonly List<ImGuiWindowPtr> loose = new List<ImGuiWindowPtr>();
        static readonly List<ImGuiWindowPtr> onTop = new List<ImGuiWindowPtr>();
        static int sortedFrame = -1;

        public static void Postfix()
        {
            if (!Layers.InFrame())
                return;
            int frame = ImGui.GetFrameCount();
            if (frame == sortedFrame)
                return;
            sortedFrame = frame;
            try
            {
                Rising.Update();
                if (Layers.Claims.Count > 0 || Proxies.Drawn.Count > 0)
                    Sort();
            }
            catch (Exception e)
            {
                Log.ErrorOnce("[Underlayer] " + e, 0x5A1D02);
            }
            finally
            {
                Layers.Claims.Clear();
                Proxies.EndFrame(frame);
            }
        }

        static void Sort()
        {
            stacked.Clear();
            loose.Clear();
            onTop.Clear();
            WindowStack stack = Find.WindowStack;
            if (stack != null)
            {
                int modal = -1;
                for (int i = 0; i < stack.Count; i++)
                    if (stack[i].absorbInputAroundWindow && Proxies.Drawn.ContainsKey(stack[i]))
                        modal = i;
                for (int i = 0; i < stack.Count; i++)
                {
                    Window window = stack[i];
                    if (i == modal)
                        stacked.Add(Blocker());
                    if (Proxies.Drawn.TryGetValue(window, out ImGuiWindowPtr proxy))
                        stacked.Add(proxy);
                    foreach (var claim in Layers.Claims)
                        if (claim.Host == window)
                            stacked.Add(claim.Window);
                }
            }

            ImVector<ImGuiWindowPtr> all = ImGui.GetCurrentContext().Windows;
            for (int i = 0; i < all.Count; i++)
            {
                ImGuiWindowPtr window = all[i];
                if ((window.Flags & ImGuiWindowFlags.ChildWindow) != 0)
                    continue;
                if ((window.Flags & AlwaysOnTop) != 0)
                    onTop.Add(window);
                else if (!Contains(stacked, window))
                    loose.Add(window);
            }

            foreach (ImGuiWindowPtr window in stacked)
                ImGui.Internal.BringWindowToDisplayFront(window);
            foreach (ImGuiWindowPtr window in loose)
                ImGui.Internal.BringWindowToDisplayFront(window);
            foreach (ImGuiWindowPtr window in onTop)
                ImGui.Internal.BringWindowToDisplayFront(window);
        }

        static ImGuiWindowPtr Blocker()
        {
            ImGui.SetNextWindowPos(new float2(0f, 0f), ImGuiCond.Always);
            ImGui.SetNextWindowSize(ImGui.GetIO().DisplaySize, ImGuiCond.Always);
            ImGui.Begin(BlockerName, BlockerFlags);
            ImGuiWindowPtr window = ImGui.Internal.GetCurrentWindow();
            ImGui.End();
            return window;
        }

        static bool Contains(List<ImGuiWindowPtr> list, ImGuiWindowPtr window)
        {
            foreach (ImGuiWindowPtr item in list)
                if (Layers.Same(item, window))
                    return true;
            return false;
        }
    }
}
