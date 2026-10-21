using System.Linq;
using System.Reflection;
using Brutal.ImGuiApi;
using UnityStubs.Gui;
using Verse;

namespace Underlayer
{
    internal static class Backend
    {
        public static bool Broken;
        static object current;
        static FieldInfo drawList, clipDepth, frameActive, drawingEnabled, drawTarget;

        public static bool Bind()
        {
            if (Broken)
                return false;
            object backend = GuiBackend.Current;
            if (backend == null)
                return false;
            if (backend == current)
                return true;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = backend.GetType();
            drawList = type.GetField("drawList", flags);
            clipDepth = type.GetField("clipDepth", flags);
            frameActive = type.GetField("frameActive", flags);
            drawingEnabled = type.GetField("drawingEnabled", flags);
            drawTarget = type.GetField("drawTarget", flags);
            var missing = new[] { ("drawList", drawList), ("clipDepth", clipDepth), ("frameActive", frameActive), ("drawingEnabled", drawingEnabled), ("drawTarget", drawTarget) }
                .Where(f => f.Item2 == null).Select(f => f.Item1).ToList();
            if (missing.Count > 0)
            {
                Broken = true;
                Log.Warning($"[Underlayer] This game build changed its GUI backend ({type.FullName}, missing {string.Join(", ", missing)}). Underlayer is switched off, ImGui windows stay on top as usual.");
                return false;
            }
            current = backend;
            return true;
        }

        public static bool CanDraw =>
            (bool)frameActive.GetValue(current) && (bool)drawingEnabled.GetValue(current) && drawTarget.GetValue(current) == null;

        public static ImDrawListPtr DrawList
        {
            get => (ImDrawListPtr)drawList.GetValue(current);
            set => drawList.SetValue(current, value);
        }

        public static int ClipDepth
        {
            get => (int)clipDepth.GetValue(current);
            set => clipDepth.SetValue(current, value);
        }
    }
}
