# Underlayer

A RimWorld 1.7 framework that stacks Dear ImGui windows with vanilla windows.

On 1.7 every ImGui window draws over every vanilla window, so a mod's ImGui window can hide a dialog opened after it. Underlayer draws each vanilla window into an invisible ImGui window and sorts them every frame to match `Find.WindowStack`. Unclaimed ImGui windows (the game's own, mods without Underlayer) stay on top as before, and ImGui popups and tooltips stay above everything.

If no mod claims a window, Underlayer does nothing.

## Using it

1. Reference `Underlayer.dll` (`1.7/Assemblies`, `Private="false"`) and add `Unon.Underlayer` to your About.xml `modDependencies` and `loadAfter`.
2. Host each ImGui window in a `Verse.Window` whose `windowRect` matches it (in UI units).
3. Call `Layers.Claim(host)` right after `ImGui.Begin`, every frame:

```csharp
public override void ExtraOnGUI()
{
    if (Event.current.type != EventType.Repaint)
        return;
    bool visible = ImGui.Begin("My window");
    Layers.Claim(this);
    if (visible)
        ImGui.Text("Hello");
    ImGui.End();
}
```

Your ImGui window now sits at the host's place in the stack: click it to bring it forward, and dialogs opened later cover it.

Letters and confirmation boxes are modal and on the Dialog layer, so lower windows never come in front of them, and Underlayer blocks ImGui input under them too. To let your window come forward anyway, use `Layers.Claim(this, riseAboveDialogs: true)`. Clicking it then lifts it above the dialogs until another window is clicked or opened. `Layers.IsRaised(host)` says whether it is lifted.

`Layers.Available` is false if a game update broke the GUI backend parts Underlayer needs. It then switches off and logs a warning.

## Requirements

- RimWorld 1.7
- [Harmony](https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077)

## Building

`dotnet build -c Release` in `Source/Underlayer`. RimWorld and Harmony are expected in the default Steam folders. If yours are elsewhere, add a `Local.props` next to this README:

```xml
<Project>
  <PropertyGroup>
    <RimWorld17Dir>D:\Steam\steamapps\common\RimWorld</RimWorld17Dir>
    <HarmonyMod>D:\Steam\steamapps\workshop\content\294100\2009463077</HarmonyMod>
  </PropertyGroup>
</Project>
```
