using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Underlayer
{
    internal static class Rising
    {
        static readonly AccessTools.FieldRef<WindowStack, List<Window>> windowsOf = AccessTools.FieldRefAccess<WindowStack, List<Window>>("windows");
        static readonly AccessTools.FieldRef<WindowStack, Window> focusedOf = AccessTools.FieldRefAccess<WindowStack, Window>("focusedWindow");
        static readonly AccessTools.FieldRef<WindowStack, bool> reorderOf = AccessTools.FieldRefAccess<WindowStack, bool>("updateInternalWindowsOrderLater");

        static readonly Dictionary<Window, int> wanted = new Dictionary<Window, int>();
        static readonly Dictionary<Window, WindowLayer> raised = new Dictionary<Window, WindowLayer>();
        static readonly HashSet<Window> seen = new HashSet<Window>();
        static readonly List<Window> scratch = new List<Window>();

        public static bool IsRaised(Window window) => raised.ContainsKey(window);

        public static void Want(Window host) => wanted[host] = Time.frameCount;

        static bool Wants(Window host) => wanted.TryGetValue(host, out int frame) && Time.frameCount - frame <= 2;

        public static void MouseDown()
        {
            WindowStack stack = Find.WindowStack;
            if (stack == null || wanted.Count == 0)
                return;
            List<Window> list = windowsOf(stack);
            Vector2 mouse = UI.MousePositionOnUIInverted;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Window window = list[i];
                if (window is ImmediateWindow || !window.windowRect.Contains(mouse))
                    continue;
                if (Wants(window) && !OnTop(list, i))
                    Raise(stack, list, window);
                return;
            }
        }

        public static void Update()
        {
            WindowStack stack = Find.WindowStack;
            if (stack == null)
                return;
            List<Window> list = windowsOf(stack);
            if (raised.Count > 0)
            {
                scratch.Clear();
                scratch.AddRange(raised.Keys);
                foreach (Window host in scratch)
                {
                    if (!list.Contains(host))
                    {
                        host.layer = raised[host];
                        raised.Remove(host);
                        continue;
                    }
                    Window trigger = null;
                    Window focused = focusedOf(stack);
                    if (focused != null && focused != host && !(focused is ImmediateWindow) && list.Contains(focused))
                        trigger = focused;
                    foreach (Window window in list)
                        if (window != host && !(window is ImmediateWindow) && !seen.Contains(window))
                            trigger = window;
                    if (trigger != null || !Wants(host))
                        Lower(stack, list, host, trigger);
                }
            }
            seen.Clear();
            foreach (Window window in list)
                seen.Add(window);
            if (wanted.Count > 0)
            {
                scratch.Clear();
                foreach (var pair in wanted)
                    if (Time.frameCount - pair.Value > 120)
                        scratch.Add(pair.Key);
                foreach (Window window in scratch)
                    wanted.Remove(window);
            }
        }

        static bool OnTop(List<Window> list, int index)
        {
            for (int i = index + 1; i < list.Count; i++)
                if (!(list[i] is ImmediateWindow))
                    return false;
            return true;
        }

        static void Raise(WindowStack stack, List<Window> list, Window host)
        {
            if (!raised.ContainsKey(host))
                raised[host] = host.layer;
            WindowLayer top = host.layer;
            foreach (Window window in list)
                if (!(window is ImmediateWindow) && window.layer > top)
                    top = window.layer;
            host.layer = top;
            list.Remove(host);
            Insert(list, host);
            focusedOf(stack) = host;
            reorderOf(stack) = true;
        }

        static void Lower(WindowStack stack, List<Window> list, Window host, Window trigger)
        {
            host.layer = raised[host];
            raised.Remove(host);
            list.Remove(host);
            Insert(list, host);
            int below = trigger == null ? -1 : list.IndexOf(trigger);
            int at = list.IndexOf(host);
            if (below >= 0 && at > below)
            {
                list.RemoveAt(at);
                list.Insert(below, host);
            }
            reorderOf(stack) = true;
        }

        static void Insert(List<Window> list, Window window)
        {
            int index = 0;
            for (int i = 0; i < list.Count; i++)
                if (window.layer >= list[i].layer)
                    index = i + 1;
            list.Insert(index, window);
        }
    }
}
