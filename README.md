# Stack Island

*Catch it. Cook it. Stack it high.*

A compact carry-and-stack island tycoon for Windows, in the style of mobile hits like Idle Mafia and Supermarket
Tycoon: one bright walled yard at a time, a close camera, tall wobbling stacks, buy spots painted on the floor and
customers queueing with speech bubbles.

## How it plays

1. Walk into the **crab pen**. You swing at nearby crabs automatically; they drop crab meat. Walk over it to stack it.
2. Drop the meat on the **grill's left tray**. Crab cakes pile up on the **right tray**.
3. Stand on the painted **SELL** sign: cakes hop onto the counter. Customers on the other side take what their bubble
   asks for and pay in cash bundles at the end of the counter.
4. Walk over the cash to stack it on your back, then stand still on a **dark buy tile** to spend it:
   more crabs, a bigger stack, hire a Crab Catcher or a Waiter, a hotter or second grill, a better recipe.

## Controls

| Key | Action |
|---|---|
| WASD / arrows | Walk |
| Right-click | Walk to that spot |
| Mouse wheel | Zoom |
| Esc | Pause menu (volume, windowed, main menu, quit) |

## Working on it

- Unity **6000.3.25f1** (URP). Menu **Stack Island** has the tools.
- Everything is built in code at runtime from `Game.cs` (progression and prices live in `Game.BuildPads`).
- Art: Kenney CC0 packs plus models drawn in code (`Art/ProcArt.cs`: meat, crab cakes, cash bundles, the dome grill,
  counter, walls, cage fences, floor signs and buy tiles).
- `pwsh Tools/unity-batch.ps1 StackIsland.EditorTools.ProjectSetup.RunBatch` sets the project up (art library, scene).
- `pwsh Tools/run-tests.ps1` runs the EditMode and PlayMode tests.
- `pwsh Tools/build-and-shoot.ps1` builds `Builds/Windows/StackIsland.exe` and captures screenshots (the game plays
  itself on a throwaway save).
