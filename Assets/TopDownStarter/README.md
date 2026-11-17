# Top-Down Starter (CRE132)

A small, working top-down game to build your own on: a character that walks in eight directions with
four-direction animation, a tiled world with water, trees and rocks to bump into, gems to collect, two
areas joined by a path, a main menu, a pause menu, and the three manager singletons from the course
(GameManager, AudioManager, SceneLoader).

## Getting started

1. Use a Unity 6 project made with the **Universal 2D** template (it has the Input System and URP 2D).
2. Double-click `CRE132_TopDownStarter.unitypackage` (or **Assets > Import Package > Custom Package...**)
   and click **Import**.
3. If Unity shows the **TMP Importer** window, click **Import TMP Essentials**. The menu and HUD text
   need them. (It only appears once per project.)
4. **Tools > Top-Down Starter > Open Main Menu**, then press **Play**.

The three scenes are added to **File > Build Profiles > Scene List** for you when the package is imported
(scenes can only be loaded by name if they are in that list). If they go missing, use
**Tools > Top-Down Starter > Add Scenes to Build List**.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Walk | WASD or the arrow keys | left stick or D-pad |
| Pause | Esc | Start |

Follow the dirt path off the east edge of the Meadow to reach the Lake, and back.

## What's inside

| Folder | What it holds |
|---|---|
| `Scenes` | `TopDown_MainMenu`, `TopDown_Meadow`, `TopDown_Lake` |
| `Scripts/Managers` | `GameManager` (gems), `AudioManager` (music, sounds, volumes), `SceneLoader` (fades between scenes, spawn points) |
| `Scripts/Player` | `PlayerMovement` (8 directions), `PlayerAnimator` (4-direction blend trees), `PlayerSpawner` |
| `Scripts/World` | `Gem`, `SceneExit`, `SpawnPoint`, `SortByY`, `CameraFollow` |
| `Scripts/UI` | `MainMenu`, `PauseMenu`, `VolumeSlider`, `GemCounter` |
| `Prefabs` | `Player`, `Gem`, `Level UI` (HUD and pause menu), `Environment/` (Tree, Rock, Bush) and `Managers/` |
| `Tiles` | grass, path, water and flower tiles, and `Top-Down Palette` (open **Window > 2D > Tile Palette**) |
| `Animation` | the player's clips and `Player` Animator Controller (Idle and Walk blend trees) |
| `Input` | `TopDownControls` (Move, Pause) |
| `Audio` | `MainMixer` (Music and SFX groups), music and sound effects |

## The managers

Each is a **singleton**: there is only one, it survives scene changes (`DontDestroyOnLoad`), and any
script can reach it through `Instance`:

```csharp
GameManager.Instance.AddGems(1);
AudioManager.Instance.PlaySound(mySound);
SceneLoader.Instance.LoadScene("TopDown_Lake", "From Meadow");   // scene name, then a spawn point name
```

Every scene has the three prefabs from `Prefabs/Managers`, so you can press Play in any scene.
**A new scene you make needs them too:** drag `GameManager`, `AudioManager` and `SceneLoader` in.

The starter's scripts are in a **namespace**, `TopDownStarter`, so they don't clash with scripts of
the same name in your project (your own `GameManager`, for example). To use them from your own
script, add this line at the top:

```csharp
using TopDownStarter;
```

## How the world is put together

- **Tilemaps** (under `Grid`): `Ground` (grass and paths, walk on it), `Water` (solid: it has a
  Tilemap Collider 2D), `Flowers` (decoration). Paint with the Top-Down Palette; paint water tiles on
  the `Water` tilemap so they block the player.
- **Trees, rocks and bushes** are prefabs with a small collider at their base, so the player walks
  round the trunk but can step behind the leaves.
- **Drawing order:** `SortByY` draws things lower on the screen in front of things higher up, so the
  player walks behind a tree and in front of it. Put it on anything you add that stands in the world.
- **Moving between areas:** a `SceneExit` (a trigger at the end of a path) loads another scene and
  names the `SpawnPoint` to arrive at. The player's `PlayerSpawner` moves it there.
- Collected gems are remembered by name in the GameManager, so they don't come back when you return.
  Give each gem in a scene its own name (Gem 1, Gem 2...).

## Ideas for building on it

- **Add an area:** duplicate `TopDown_Lake`, rename it, add it to the scene list (**File > Build
  Profiles**), then add a `SceneExit` and a `SpawnPoint` at each end of the path between the two areas.
- **Tune the walk:** change Move Speed on the Player's `PlayerMovement` while playing.
- **More:** melee attack (T04), enemies (T06), health and knockback (T08), inventory (T09),
  doors and keys (T10), dialogue (T11).

## Credits

For CRE132 coursework only. The art and sound packs are licensed to the course: don't share them
or use them outside your coursework.

- Player, tiles, trees, rocks and bushes: [Mystic Woods](https://game-endeavor.itch.io/mystic-woods) by Game Endeavor.
- Gem icon: [Kyrise's Free 16x16 RPG Icon Pack](https://kyrise.itch.io/kyrises-free-16x16-rpg-icon-pack) by Kyrise (CC BY 4.0).
- Sound effects: ElvGames ([Humble Bundle game assets](https://elvgames.itch.io/humble-bundle-game-assets)).
- Music: made for CRE132.
