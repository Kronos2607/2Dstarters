# Platformer Starter (CRE132)

A small, working 2D platformer to build your own game on: an animated hero with a double jump,
coins, spikes, two levels with a goal portal, a main menu, a pause menu, and the three manager
singletons from the course (GameManager, AudioManager, SceneLoader).

## Getting started

1. Use a Unity 6 project made with the **Universal 2D** template (it has the Input System and URP 2D).
2. Double-click `CRE132_PlatformerStarter.unitypackage` (or **Assets > Import Package > Custom Package...**)
   and click **Import**.
3. If Unity shows the **TMP Importer** window, click **Import TMP Essentials**. The menu and HUD text
   need them. (It only appears once per project.)
4. **Tools > Platformer Starter > Open Main Menu**, then press **Play**.

The three scenes are added to **File > Build Profiles > Scene List** for you when the package is imported
(scenes can only be loaded by name if they are in that list). If they go missing, use
**Tools > Platformer Starter > Add Scenes to Build List**.

## Controls

| Action | Keyboard | Gamepad |
|---|---|---|
| Run | A / D or the arrow keys | left stick or D-pad |
| Jump (press again in the air to double jump) | Space | A / Cross |
| Pause | Esc | Start |

Hold Jump for a high jump, tap it for a small hop.

## What's inside

| Folder | What it holds |
|---|---|
| `Scenes` | `Platformer_MainMenu`, `Platformer_Level1`, `Platformer_Level2` |
| `Scripts/Managers` | `GameManager` (coins), `AudioManager` (music, sounds, volumes), `SceneLoader` (fades between scenes) |
| `Scripts/Player` | `PlayerController` (run, jump, double jump), `PlayerAnimator`, `PlayerRespawn` |
| `Scripts/Level` | `Coin`, `KillZone` (spikes and pits), `LevelGoal` (the portal), `CameraFollow` |
| `Scripts/UI` | `MainMenu`, `PauseMenu`, `VolumeSlider`, `CoinCounter` |
| `Prefabs` | `Player`, `Coin`, `Goal Portal`, `Level UI` (HUD and pause menu), and `Managers/` |
| `Tiles` | the ground tiles and `Platformer Palette` (open **Window > 2D > Tile Palette**) |
| `Animation` | the hero's clips and `Hero` Animator Controller (Idle, Run, Jump, Fall, Land) |
| `Input` | `PlatformerControls` (Move, Jump, Pause) |
| `Audio` | `MainMixer` (Music and SFX groups), music and sound effects |

## The managers

Each is a **singleton**: there is only one, it survives scene changes (`DontDestroyOnLoad`), and any
script can reach it through `Instance`:

```csharp
GameManager.Instance.AddCoins(1);
AudioManager.Instance.PlaySound(mySound);
SceneLoader.Instance.LoadScene("Platformer_Level2");
```

Every scene has the three prefabs from `Prefabs/Managers`, so you can press Play in any scene.
**A new scene you make needs them too:** drag `GameManager`, `AudioManager` and `SceneLoader` in.

The starter's scripts are in a **namespace**, `PlatformerStarter`, so they don't clash with scripts of
the same name in your project (your own `GameManager`, for example). To use them from your own
script, add this line at the top:

```csharp
using PlatformerStarter;
```

## Ideas for building on it

- **Paint more level:** select the `Ground` tilemap (under `Grid`) and paint with the Platformer Palette.
  Ground tiles are solid. Paint on `Decoration` for things the player walks past.
- **Tune the jump:** select the Player and change Jump Speed, Extra Jumps (2 = triple jump) or the
  gravity values on `PlayerController` while playing.
- **Add a level:** duplicate `Platformer_Level2`, name it `Platformer_Level3`, add it to the scene list
  (**File > Build Profiles**), and set **Next Scene Name** on Level 2's `Goal Portal` to `Platformer_Level3`.
- **Add a coin:** drag `Prefabs/Coin` into the scene.
- **More:** enemies (P08), health and knockback (S01, P09), checkpoints and lives (P10), parallax (P12).

## Credits

For CRE132 coursework only. The art and sound packs are licensed to the course: don't share them
or use them outside your coursework.

- Hero, Cliffside Greens tiles, coins, backgrounds and sound effects: ElvGames
  ([Humble Bundle game assets](https://elvgames.itch.io/humble-bundle-game-assets)).
- Spikes, jump sounds and music: made for CRE132.
