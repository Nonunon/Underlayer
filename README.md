# Underlayer

A RimWorld 1.7 framework that stacks Dear ImGui windows together with vanilla windows.

On 1.7 every ImGui window draws on top of every vanilla window, so a mod's ImGui window can cover a dialog that opened after it. With Underlayer, each vanilla window is drawn into its own invisible ImGui window, and once per frame the ImGui windows are put in the same order as `Find.WindowStack`. ImGui windows that no mod claims (the game's own tools, mods that don't use Underlayer) stay on top like before, and ImGui popups and tooltips stay above everything.

When no mod claims a window, Underlayer does nothing.

## Using it

1. Reference `Underlayer.dll` (`1.7/Assemblies`, `Private="false"`) and add `Unon.Underlayer` to your About.xml `modDependencies` and `loadAfter`.
2. Give each ImGui window a `Verse.Window` in the window stack whose `windowRect` matches it (in UI units), the same way you would block clicks to the map.
3. Call `Layers.Claim(hostWindow)` between your `ImGui.Begin` and `ImGui.End`, every frame:

```csharp
public override void ExtraOnGUI()
{
    if (Event.current.type != EventType.Repaint)
        return;
    if (ImGui.Begin("My window"))
    {
        Layers.Claim(this);
        ImGui.Text("Hello");
    }
    ImGui.End();
}
```

Your ImGui window now draws at the host window's place in the stack. Click the host to bring it to the front, open a dialog and it covers your window.

`Layers.Available` is false if a game update changed the GUI backend parts Underlayer needs. It then switches itself off and logs a warning.

## Requirements

- RimWorld 1.7
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)

## Building

`dotnet build -c Release` in `Source/Underlayer`. The project looks for RimWorld and Harmony in the default Steam folders. If yours are elsewhere, create `Local.props` next to this README:

```xml
<Project>
  <PropertyGroup>
    <RimWorld17Dir>D:\Steam\steamapps\common\RimWorld</RimWorld17Dir>
    <HarmonyMod>D:\Steam\steamapps\workshop\content\294100\2009463077</HarmonyMod>
  </PropertyGroup>
</Project>
```
