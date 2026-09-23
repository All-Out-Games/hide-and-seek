# Hide & Seek player rig migration

Status: native authoring and source preservation verified; inactive hosted
validation pending. No candidate uploaded or activated yet.

The isolated checkout is `C:/allout-game-rig-rollout/hide-and-seek`, branch
`codex/hide-seek-rig-migration` in `All-Out-Games/hide-and-seek`. It starts from
current remote master `ec65c8a18e037315b761d2fe4c34f9fb3bbb2435` and preserves
the two commits newer than the original local checkout. Do not reset the user's
original checkout or discard those existing changes.

| Target | Game | Active version at preflight |
|---|---|---|
| Primary | `66a7eee64183dc70ad924539` | `6ab20436236e3076fa1ad9cf` |
| Staging | `671826d1359939499010bf4b` | `6ab1f7434c1aefa488cabe34` |

Fresh readbacks and downloads agree with the source inventory. Both source ZIPs
contain the same 818 entries, including 12 C# scripts and 800 scene entities.
The published authored content matches the original local commit
`d3d0d06a5ccb269a3c38ab8e013cead9aaa7c78b`. Each archive is 5,987,973 bytes;
primary SHA-256 is
`c9901d4c572ff9f32385e746678388cfe09b3f5e2fa4551ae3d49a283f6b9cac`,
staging is `970815bb92664a63dbf34f860969a93f3f6edfbe54222d2d6e872e6117eddfe5`.
The custom player JSON is byte-identical to the published cooked input:
4,776,584 bytes, SHA-256
`0336db3a15aef680f07a39605c0dc6a0cfce65cd2a8fb3d2746a6c2c2ebfd7da`.

Remote master has already migrated the scene from version 13 to 14. Resolving
AOID references to stable entity identity and component cid preserves the
800-entity graph. The remaining field differences are 83 omitted legacy
`wait_for_load` fields and the explicit WorldManager lobby reference, pointing
to the same WorldLobby component previously assigned during `WorldLobby.Awake`.
Two existing script edits implement that serialized lobby reference. These are
pre-existing remote-master changes, not part of the new rig migration; they must
be retained and included in gameplay validation.

## Planned runtime changes

Use the native merge baker to author an ordinary player rig that preserves the
custom base, shared skins, bones and animation behavior. Update both the default
player rig and `PlayerCorpse.Awake`'s explicit skeleton selection. Keep the
original custom rig because `CorpseRend.prefab` still references it. Preserve
other required rigs, textures, gameplay scripts and cosmetics.

Archive `res/NoisyBaseRig` outside the runtime resource tree. A 5,124-record scan
of authoring files, published source, decoded bundled assets and serialized
game data found no references outside the rig's own resources and manifest
identity entries. Unlike the unused Digging authoring rig, this rig is actually
bundled: 1,418,920 cooked bytes, 3,484,280 decoded bytes. Its two raw texture pages
and authoring files remain available in Git. These sizes are not a measured
network saving; final cold and persistent-cache measurements are still required.

The deployed general OPFS reader is unchanged. Merge-output caching is outside
scope. No engine API, memory layout, protocol or game economy change is planned.

Evidence lives in `C:/allout-rig-startup-local/.codex-tmp/rig-startup/`:
`hide-seek-source-preflight.json`, `hide-seek-authoring-baseline.zip`,
`hide-seek-authoring-audit.json`, `hide-seek-authoring-field-differences.json`,
and `hide-seek-unused-rig-audit.json`.

## Native composition and reviewed publication

The ordinary composition contains 22,309,390 JSON bytes and a 331,346-byte atlas
with 794 pages. Seven native inventory queries, all 1,037 skin attachment lists,
all 310 animation details and 1,046 sampled bone/slot layouts match the original
merge exactly, without truncated responses. Samples cover each animation's
start/midpoint/end with the base skin, and detective/noir detective skins for
the MURD_002 and Death_No_HP clips. This does not substitute for layered-animation
or actual gameplay checks.

Both native compiles and asset validation passed. The normal compact publisher
produced `hide-seek-baked.proj`: 1,621,114 bytes, SHA-256
`eab2442395125f41ec1b7a7e6f14c34f598185bb04d00a73925190a2c0f6ed54`.
It created no game version. The package contains 927 manifest assets, 24 bundled
entries, 15 external ordinary rigs, zero bundled rigs and zero merge recipes.
The unused NoisyBaseRig and its textures are absent. The required custom player,
its two texture pages, the eyes, killer rig and corpse prefab remain.

Against the immutable current-master baseline, all 800 entity identities, AOIDs
and values are preserved after three native Sprite_Renderer `tint` to `color`
aliases. No entity was hand-edited. The only semantic scene configuration change
selects `player_composed/player.spine`; the only new script change selects that
same asset in PlayerCorpse. The other 11 scripts are unchanged. Both original and
candidate editors have closed. Generated authoring manifests are included with
the source changes; raw composition and archived inputs remain editable in Git.

The new rig captures the current shared engine player. Future shared-rig changes
must be deliberately reauthored. Game state, economy, network protocol, memory
layout and general OPFS policy are unchanged by this migration. Source ZIP size
is not total join transfer. Hosted versions, multiplayer gameplay, same-session
cold loading and persistent-cache reuse still need validation before activation.

Additional evidence: `hide-seek-rig-native-comparison-summary.json`,
`hide-seek-rig-native-comparison.json.gz`, both compile responses,
`hide-seek-candidate-assets-validated.json`, and
`migration-candidates/hide-seek-baked.json`.
