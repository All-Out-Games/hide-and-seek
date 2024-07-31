11
459561500673
600605878258026 1714141066707632900
{
  "name": "CorpseRend",
  "local_enabled": true,
  "local_position": {
    "X": 0,
    "Y": 0
  },
  "local_rotation": 0,
  "local_scale": {
    "X": 0.5279999971389771,
    "Y": 0.5279999971389771
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
    "depth_offset": 0,
    "skeleton_scale": {
      "X": 1,
      "Y": 1
    },
    "mask_in_shadow": true
  }
},
{
  "cid": 2,
  "aoid": "1772234016052065:1718045277643545200",
  "component_type": "Mono_Component",
  "mono_component_type": "PlayerCorpse",
  "data": {
    "PlayerAnimator": "237992662795450:1718489814464556200",
    "InventoryBox": "1019462471833453:1720548097124452300",
    "IsDetectiveBox": false,
    "PlayerName": "",
    "ColorIndex": 0,
    "PlayerSkins": [

    ],
    "DeathAnim": ""
  }
},
{
  "cid": 3,
  "aoid": "1023798863353563:1720549334679875600",
  "component_type": "Internal_Component",
  "internal_component_type": "Interactable",
  "data": {
    "prompt_offset": {
      "X": 0,
      "Y": 1
    },
    "text": "",
    "hold_text": "",
    "radius": 2,
    "required_hold_time": 0.6000000238418579
  }
}
