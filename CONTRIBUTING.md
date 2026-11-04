# Brawlkin Contributing Guide

## Git workflow
- No direct pushes to `main`. Everything goes through a Pull Request (1 approval).
- Always branch from an up-to-date `main`.
- Branch names: `feature/...`, `art/...`, `fix/...`, `setup/...`.
- Keep branches short-lived (1-3 days). Delete the branch after merge.
- Commit messages: English, short summary line in the imperative
  (e.g. `Add target selection for Tank priority`).
- Never commit `Library/`, `Temp/`, `Logs/`, `UserSettings/` (covered by .gitignore).
- Do not commit the `.meta` file of an empty folder. It is committed together
  with the first file that goes into the folder.

## Unity rules
- Same Unity version for everyone (see `ProjectSettings/ProjectVersion.txt`).
- Never edit the same scene or prefab at the same time as someone else.
- Each person works in their own test scene (`Scene_Battle_Dev`, `Scene_UI_Dev`).
  The main scene is only touched during integration, by one person.
- Character prefabs are edited by the artist who owns that character.
- Large binary files (png, psd, wav, fbx, ...) are stored with Git LFS.

## Folder ownership
- `Assets/_Game/Sim`: battle simulation (Can)
- `Assets/_Game/Art`, character prefabs: art
- `Assets/_Game/UI`, `Assets/_Game/Battle`: gameplay/UI developers

## Code structure
- `Brawlkin.Sim` is pure C#, no `UnityEngine` references.
  It takes two team layouts and a seed, and produces a list of events per tick.
- `Brawlkin.Game` reads those events and handles visuals, UI and input.
- Sim never references Game.

## Sprite rules
- File name: `CharacterName_State_FrameNo` (e.g. `Cinderoo_Walk_03`)
- Cell size 256x256, pivot at bottom center, characters face right.
- States: Idle, Walk (8 frames), Attack, Hurt, Special, Death.

## Sim to view contract
(To be defined: event structure, e.g. `AttackEvent { attackerId, targetId, damage, tick }`)