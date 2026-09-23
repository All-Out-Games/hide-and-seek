# Hide & Seek player rig migration

Status: both variants are activated and independently verified. Source, native
equivalence, hosted multiplayer gameplay, cold comparison and persistent storage
checks passed. Actual primary production rendering, movement and OPFS reuse passed.

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

## Runtime changes

The native merge baker authored an ordinary player rig that preserves the
custom base, shared skins, bones and animation behavior. Both the default
player rig and `PlayerCorpse.Awake` select it. The migration keeps the
original custom rig because `CorpseRend.prefab` still references it. Preserve
other required rigs, textures, gameplay scripts and cosmetics.

`res/NoisyBaseRig` is archived outside the runtime resource tree. A 5,124-record scan
of authoring files, published source, decoded bundled assets and serialized
game data found no references outside the rig's own resources and manifest
identity entries. Unlike the unused Digging authoring rig, this rig is actually
bundled: 1,418,920 cooked bytes, 3,484,280 decoded bytes. Its two raw texture pages
and authoring files remain available in Git. These sizes are not a measured
network saving; measured cold and persistent-cache results are recorded below.

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
cold loading and persistent-cache reuse are validated below.

Additional evidence: `hide-seek-rig-native-comparison-summary.json`,
`hide-seek-rig-native-comparison.json.gz`, both compile responses,
`hide-seek-candidate-assets-validated.json`, and
`migration-candidates/hide-seek-baked.json`.

## Hosted candidate verification

Source implementation: `8c512101fc61de2aa4f6f153bfe205065aad5448`.
Each variant was uploaded once with `setActive=false`. Do not upload again.
Both compile successfully on P48 with 1,005,992-byte DAT files. The hosted source
archives exactly match the reviewed SHA above. Compiled manifest, configuration
and bundled records match review, with 15 external ordinary rigs and no recipes.

| Target | Existing candidate | Build hash | DAT SHA-256 |
|---|---|---|---|
| Primary | `6ab409ce2e1d629109efe075` | `e5fa3a5e9deb8835` | `e3169a4f91f27a0e6d5ef8897e1fa71cec1b048b1e0b594554030b7f78e0a6b6` |
| Staging | `6ab409d32e1d629109efe07b` | `df54c48b9f16fb4d` | `4ab707b9b89ad462763486f488963939f094244d5765f400ae503b5fb6f4fc8f` |

Preserve primary public/stable and staging private/stable settings. Old versions
remain available. This release needs no engine/server/Jenkins deployment, new
configuration, migration, protocol change or game-memory-layout change.

## Cold startup comparison

Canonical Chrome full-dev benchmark: pinned Debug Wasm SHA-256
`a8ccde20a30e7cea03e30133c4dfdb3ef9698c680860f27ff599d3b50a980ac6`,
CPU6, 655,360 bytes/s and 80 ms latency. Two fresh-profile/all-storage-cleared
runs per version, same session, warm-server precheck, no retries, no concurrent
benchmark, editor or heavy build. All four runs passed.

| Median through player spawn | Original | Candidate |
|---|---:|---:|
| Navigation to spawn | 77.618 s | 73.508 s |
| Whole-page encoded transfer | 35.799 MB | 39.779 MB |
| Separate game-asset transfer | 10.378 MB | 18.673 MB |
| Game-data transfer | 5.418 MB | 1.105 MB |
| Wasm heap capacity | 556.794 MB | 386.662 MB |
| Native allocated snapshot | 115.768 MB | 107.625 MB |
| Asset-worker heap capacity | 113.770 MB | 113.770 MB |

The median gain is 4.110 seconds (5.29%), with 3.980 MB more cold transfer.
This removes merge construction and reduces Wasm capacity by 170.131 MB, but
is not the minimum-payload solution. Capacity and allocation snapshots are not
peak or resident memory. These Debug timings are not production speed or player
retention. Source compilation, composition and unused-asset removal changed
together; this comparison does not isolate one cause. Raw/source ZIP size is
not join bandwidth. Exact runs and engine phases: `hide-seek-cold-bake-comparison.json`.

## Gameplay and persistent storage

Each actual hosted candidate ran in three isolated Chrome clients against the
full local development environment, without game-data or bundle overrides.
Trusted UI input exercised role assignment, movement, prop transformation,
decoy separation, gun aiming/hit, death/corpse rendering, seeker revival,
knife hit, final-hider death and win/lose UI. Primary continued into a new round
on the Zoo map. Staging returned to the island lobby with map voting; its hider
changed from a recycling bin to a streetlamp. This also exercises the existing
remote-master lobby-reference change retained in the source.

Both variants had zero runtime merges, JavaScript page exceptions and engine
error states. Local guest translation requests to the `unseen-strings` endpoint
returned HTTP 401; there were no game-asset HTTP failures. This is not exhaustive
coverage of every map, cosmetic combination or animation frame.

Each browser was then restarted, retaining OPFS but clearing HTTP cache.
Primary retained 376 files; staging retained 369. Both rendered and moved in
all four tested directions with the exact version, zero game-asset requests
or bytes and zero rig requests. This confirms the already-deployed general
OPFS reader works for these packages. It does not imply zero client/game-data
traffic or eliminate decoding. No merge-output cache work was added.

Evidence: `hide-seek-gameplay-summary.json`, `hide-seek-{primary,staging}-multiplayer/`,
`hide-seek-{primary,staging}-multiplayer-cache-restart/opfs-only.json` and
`hide-seek-{primary,staging}-hosted-verification.json`. All owned browsers,
editors and local game jobs are closed. Activation and independent production
verification will be recorded separately after the coordinator completes them.

## Activation and production verification

The coordinator activated staging first at 2026-09-23 17:45:35.732 UTC and
primary at 17:45:43.755 UTC through the normal guarded API (HTTP 200).
The exact candidate IDs above are active, old versions retained, no pending
activation, version counts unchanged, primary public/stable and staging
private/stable unchanged. Root independently read both production selections.
Source and review commit `adc65840811216b01654368bee685dc0332f0f2f` were clean
and pushed to master before activation. No additional uploads were made.
Coordinator receipts: `C:/Users/matth/AppData/Local/Temp/hide-seek-activation-20260923/`.

Actual primary production Chrome used the expected current PRIMARY web
`4932bc8a15714e3c3bf5f98e5c990564c53968f8` and the exact active game version,
with no bundle or data substitutions. Fresh and restarted browser processes
both rendered, spawned and moved with zero runtime merges, page exceptions,
engine error states and HTTP failures. These are unthrottled functional checks,
not a production timing A/B or a population retention measurement.

The fresh profile downloaded 18,682,527 game-asset bytes through spawn; later
streaming brought the observation to 32,332,765 bytes. After browser restart
with HTTP cleared, 306 cached asset files were retained. It downloaded zero
game assets through spawn and re-fetched zero previously cached assets
throughout. Three new assets (799,008 bytes) streamed later; none had been
requested in the cold capture. Do not report zero whole-session downloads.
Client and game-data traffic are separate from these game-asset totals.

Evidence: `hide-seek-production-migration-{cold,opfs}/result.json` and screenshots,
`hide-seek-{primary,staging}-active-readback.json`. Private staging gameplay and
cache checks used its actual hosted package in isolated development; its
production check verifies selection, not an authenticated private playthrough.
All owned editors, browsers and local game jobs are closed. This game family is
complete; other catalog migrations, minimum-payload work and the broader Poki
90% real-player loading objective remain open. Do not repeat these publications.
