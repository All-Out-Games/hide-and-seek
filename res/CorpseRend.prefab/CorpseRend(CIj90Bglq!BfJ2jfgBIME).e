13
459561500673
600605878258026 1714141066707632900
{
  "name": "CorpseRend",
  "local_enabled": true,
  "local_position": {

  },
  "local_rotation": 0,
  "local_scale": {
    "X": 1,
    "Y": 1
  },
  "spawn_as_networked_entity": true
},
{
  "cid": 1,
  "aoid": "237992662795450:1718489814464556200",
  "component_type": "Internal_Component",
  "internal_component_type": "Spine_Animator",
  "data": {
    "skeleton_data_asset": "animations/player/playercharacter.spine",
    "ordered_skins": [

    ],
    "mask_in_shadow": true
  }
},
{
  "cid": 2,
  "aoid": "1772234016052065:1718045277643545200",
  "component_type": "Mono_Component",
  "mono_component_type": "PlayerCorpse",
  "data": {
    "PlayerAnimator": "237992662795450:1718489814464556200"
  }
}
