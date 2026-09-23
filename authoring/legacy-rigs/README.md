# Preserved rig authoring

`player.merged_spine_rig` records the former runtime merge recipe. Its native
composition is now authored in `res/player_composed/player.spine` and the
accompanying atlas. The custom input remains in `res/animations/player` because
the corpse prefab still uses it.

`NoisyBaseRig` was included in the old runtime bundle but has no references from
game source, decoded assets or serialized scene data. Keep these files for
authoring history without including them in the game's runtime resources.

See `docs/player-rig-migration-2026-09-23.md` for the preservation audit and
release status. Changes to the shared engine player require deliberate
reauthoring of the composition; runtime merging is no longer its update path.
