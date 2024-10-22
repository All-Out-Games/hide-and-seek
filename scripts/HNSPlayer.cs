using AO;

public partial class HNSPlayer : Player
{
	private SyncVar<int> _playerRole = new((int)PlayerRole.Spectator);
	public PlayerRole PlayerRole
	{
		get => (PlayerRole)_playerRole.Value;
		set => _playerRole.Set((int)value);
	}

	public SyncVar<int> CurrentPropIndex = new(0);

	public SyncVar<Entity> PlayerCorpse = new();

	public Entity PropEntity;
	public Sprite_Renderer PropSpriteRenderer;
	public Spine_Animator PropEyes;

	public SyncVar<int> WinsSync = new();

	public bool WasPresentAtRoundStart;

	public Box_Collider ActualPropCollider;
	public float SeekerSpeed => 1.25f;
	public int Wins
	{
		get 
		{ 
			if (Network.IsClient) return 0;
			return Save.GetInt(this, "wins", 0); 
		}

		set 
		{ 
			if (Network.IsServer)
			{
				Save.SetInt(this, "wins", value); 
				Save.OrderedSet("wins", $"{this.UserId}", value);
				WinsSync.Set(value);
			}
		}
	}

	public override void Awake()
	{
		//SpineAnimator.Entity.LocalScale = new Vector2(0.528f, 0.528f);

		_playerRole.OnSync += (old, value) =>
		{
			RemoveEffect<SpectatorEffect>(false);
			if (PlayerRole == PlayerRole.Spectator)
			{
				AddEffect<SpectatorEffect>();
			}
		};
		
	

		var collisionEntity = Assets.GetAsset<Prefab>("PlayerCollision.prefab").Instantiate();
		collisionEntity.GetComponent<PlayerCollisionChild>().Player = this;
		collisionEntity.LocalScale = new Vector2(1.1f, 1.1f);
		collisionEntity.SetParent(Entity, false);

		PropEntity = Entity.Create();
		PropEntity.SetParent(Entity, false);
		PropSpriteRenderer = PropEntity.AddComponent<Sprite_Renderer>();
		
		ActualPropCollider = PropEntity.AddComponent<Box_Collider>();
		ActualPropCollider.Size = Vector2.Zero;
		ActualPropCollider.IsTrigger = true;
		ActualPropCollider.AddComponent<PlayerCollisionChild>().Player = this;
	
		
		PropEntity.LocalEnabled = false;
		{
			var propEyesEntity = Entity.Create();
			propEyesEntity.SetParent(Entity, false);
			PropEyes = propEyesEntity.AddComponent<Spine_Animator>();
			PropEyes.SpineInstance.SetSkeleton(Assets.GetAsset<SpineSkeletonAsset>("animations/eyes/Eyes_mIK.spine"));
			var sm = StateMachine.Make();
			var appearTrigger = sm.CreateVariable("appear", StateMachineVariableKind.TRIGGER);
			var layer = sm.CreateLayer("main");
			var appearState = layer.CreateState("appear", 0, false);
			var disappearState = layer.CreateState("disappear", 0, false);
			var idleState = layer.CreateState("idle_mIK", 0, true);
			layer.SetInitialState(idleState);
			layer.CreateGlobalTransition(appearState).CreateTriggerCondition(appearTrigger);
			layer.CreateTransition(appearState, idleState, true);
			PropEyes.SpineInstance.SetStateMachine(sm, Entity);
			propEyesEntity.LocalScale = new Vector2(1.0f, 1.0f);
			PropEyes.SetCrewchsia(ColorIndex);
			propEyesEntity.LocalEnabled = false;
		}

		{
			var murderLayer = SpineAnimator.SpineInstance.StateMachine.CreateLayer("murder_layer", 10);
			var aoLayer = SpineAnimator.SpineInstance.StateMachine.TryGetLayerByName("main");
			var aoIdleState = aoLayer.TryGetStateByName("Idle");
			var aoRunState = aoLayer.TryGetStateByName("Run_Fast");
			var idleState = murderLayer.CreateState("__CLEAR_TRACK__", 0, true);
			murderLayer.SetInitialState(idleState);

			var pointBool = SpineAnimator.SpineInstance.StateMachine.CreateVariable("point", StateMachineVariableKind.BOOLEAN);
			var pointExaggerateTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("point_exaggerate", StateMachineVariableKind.TRIGGER);
			var pointState = murderLayer.CreateState("MURD_002/point_mIK_AL", 0, true);
			var pointExaggerateState = murderLayer.CreateState("MURD_002/point_ex_mIK_AL", 0, false);
			murderLayer.CreateTransition(idleState, pointState, false).CreateBoolCondition(pointBool, true);
			murderLayer.CreateTransition(pointState, pointExaggerateState, false).CreateTriggerCondition(pointExaggerateTrigger);
			murderLayer.CreateTransition(pointExaggerateState, pointExaggerateState, false).CreateTriggerCondition(pointExaggerateTrigger);
			murderLayer.CreateTransition(pointExaggerateState, pointState, true);
			murderLayer.CreateTransition(pointState, idleState, false).CreateBoolCondition(pointBool, false);

			var attackTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("murder_attack", StateMachineVariableKind.TRIGGER);
			var attackState = murderLayer.CreateState("007BB/swing_weapon_mIK_AL", 0, false);
			murderLayer.CreateGlobalTransition(attackState).CreateTriggerCondition(attackTrigger);
			murderLayer.CreateTransition(attackState, idleState, true);

			var openFolderTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("open_folder", StateMachineVariableKind.TRIGGER);
			var closeFolderTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("close_folder", StateMachineVariableKind.TRIGGER);
			var takeOutFolderState = aoLayer.CreateState("MURD_002/evidence_take_out", 0, false);
			var folderLoopState = aoLayer.CreateState("MURD_002/evidence_loop", 0, true);
			var putAwayFolderState = aoLayer.CreateState("MURD_002/evidence_put_away", 0, false);
			aoLayer.CreateGlobalTransition(takeOutFolderState).CreateTriggerCondition(openFolderTrigger);
			aoLayer.CreateTransition(takeOutFolderState, folderLoopState, true);
			aoLayer.CreateTransition(folderLoopState, putAwayFolderState, false).CreateTriggerCondition(closeFolderTrigger);
			aoLayer.CreateTransition(putAwayFolderState, aoIdleState, true);

			var openCamerasTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("open_cameras", StateMachineVariableKind.TRIGGER);
			var closeCamerasTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("close_cameras", StateMachineVariableKind.TRIGGER);
			var takeOutCamerasState = aoLayer.CreateState("MURD_002/camera_tablet_take_out", 0, false);
			var camerasLoopState = aoLayer.CreateState("MURD_002/camera_tablet_loop", 0, true);
			var putAwayCamerasState = aoLayer.CreateState("MURD_002/camera_tablet_put_away", 0, false);
			aoLayer.CreateGlobalTransition(takeOutCamerasState).CreateTriggerCondition(openCamerasTrigger);
			aoLayer.CreateTransition(takeOutCamerasState, camerasLoopState, true);
			aoLayer.CreateTransition(camerasLoopState, putAwayCamerasState, false).CreateTriggerCondition(closeCamerasTrigger);
			aoLayer.CreateTransition(putAwayCamerasState, aoIdleState, true);

			var transformTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("transform_start", StateMachineVariableKind.TRIGGER);
			var transformBackTrigger = SpineAnimator.SpineInstance.StateMachine.CreateVariable("transform_back_end", StateMachineVariableKind.TRIGGER);
			var toKillerState = aoLayer.CreateState("MURD_002/transform_to_imposter_start", 0, false);
			var fromKillerState = aoLayer.CreateState("MURD_002/transform_to_normal_end", 0, false);
			aoLayer.CreateGlobalTransition(toKillerState).CreateTriggerCondition(transformTrigger);
			aoLayer.CreateTransition(toKillerState, aoIdleState, true);
			aoLayer.CreateGlobalTransition(fromKillerState).CreateTriggerCondition(transformBackTrigger);
			aoLayer.CreateTransition(fromKillerState, aoIdleState, true);

			var dragBodyBool = SpineAnimator.SpineInstance.StateMachine.CreateVariable("dragging_body", StateMachineVariableKind.BOOLEAN);
			var aoMovingBool = SpineAnimator.SpineInstance.StateMachine.TryGetVariableByName("moving");
			var dragIdle = aoLayer.CreateState("MURD_002/drag_body_idle_right", 0, true);
			var dragMove = aoLayer.CreateState("MURD_002/drag_body_walk_right", 0, true);
			aoLayer.CreateTransition(aoIdleState, dragIdle, false).CreateBoolCondition(dragBodyBool, true);
			aoLayer.CreateTransition(dragIdle, aoIdleState, false).CreateBoolCondition(dragBodyBool, false);
			aoLayer.CreateTransition(aoRunState, dragMove, false).CreateBoolCondition(dragBodyBool, true);
			aoLayer.CreateTransition(dragMove, aoRunState, false).CreateBoolCondition(dragBodyBool, false);
			aoLayer.CreateTransition(dragIdle, dragMove, false).CreateBoolCondition(aoMovingBool, true);
			aoLayer.CreateTransition(dragMove, dragIdle, false).CreateBoolCondition(aoMovingBool, false);
		}

		if (Network.IsServer)
		{
			if (GameManager.Instance.State == GameState.WaitingForPlayers || GameManager.Instance.State == GameState.VotingState || GameManager.Instance.State == GameState.CountingDown)
			{
				PlayerRole = PlayerRole.Prop;
				TeleportToLobby();
			}
			else
			{
				var spawns = WorldManager.Instance.CurrentWorld.PropSpawns;
				Teleport(spawns[0].Position);
			}

			WinsSync.Set(Wins);
		}
	}

	public void TeleportToLobby()
	{
		var spawns = WorldManager.Instance.Lobby.PropSpawns;
		var rand = new Random();
		Teleport(spawns[rand.Next(0, spawns.Count)].Position);
	}
	public bool IsAlreadyInLobby()
	{
		return PlayerRole == PlayerRole.Prop && Entity.Position.X < -45.126f;
	}

	public void PreparePlayerForRound()
	{
		if (Network.IsServer)
		{
			var playerCorpse = Assets.GetAsset<Prefab>("CorpseRend.prefab").Instantiate<PlayerCorpse>(e =>
            {
                var playerCorpse = e.GetComponent<PlayerCorpse>();
    			playerCorpse.Entity.Name = $"{Name}_corpse";
    			playerCorpse.Entity.Position = new Vector2(1000, 1000);
    			playerCorpse.PlayerName = Name;
    			playerCorpse.ColorIndex = ColorIndex;
    			playerCorpse.PlayerSkins = SpineAnimator.SpineInstance.GetSkins();
            });
			Network.Spawn(playerCorpse.Entity);
			PlayerCorpse.Set(playerCorpse.Entity);
		}

		WasPresentAtRoundStart = true;

		if (PlayerRole == PlayerRole.Seeker || PlayerRole == PlayerRole.Prop)
		{
			AddEffect<RoundStartAnimationEffect>();
		}
	}

	public override bool DisableDirectionalFlipping => HasEffect<KnifeSwingEffect>();

	public override void Update()
	{
		if (IsLocal)
		{
			if (PlayerRole == PlayerRole.Seeker && GameManager.Instance.State == GameState.Round)
			{
				DrawDefaultAbilityUI(new AbilityDrawOptions(){
					AbilityElementSize = 75,
					Abilities = new Ability[]{
						GetAbility<GunAbility>(),
						GetAbility<KnifeAbility>(),
					}
				});
			}

			if (PlayerRole == PlayerRole.Prop && (GameManager.Instance.State == GameState.Round || GameManager.Instance.State == GameState.Hiding))
			{
				DrawDefaultAbilityUI(new AbilityDrawOptions(){
					Abilities = new Ability[] {
						//GetAbility<SwapAbility>(),
						GetAbility<DecoyAbility>()
					}
				});
			}

			// if (PlayerRole == PlayerRole.Prop)
			// {
			//     DrawDefaultAbilityUI(new AbilityDrawOptions(){
			//         AbilityElementSize = 75,
			//         Abilities = new Ability[]{
			//             HasEffect<KillerEffect>() ? GetAbility<KillerKillAbility>() : GetAbility<KillAbility>(),
			//             GetAbility<TransformAbility>(),
			//             HasEffect<KillerEffect>() ? GetAbility<KillerThrowAbility>() : GetAbility<DragBodyAbility>(),
			//             GetAbility<LockDoorsAbility>(),
			//             GetAbility<PlaceSilentAlarmAbility>(),
			//         }
			//     });
			// }
		}
	}

	[ClientRpc]
	public void RoundStart()
	{
		if (PlayerRole == PlayerRole.Prop)
		{
			AddEffect<PropEffect>();
		}
	}

	public void SetCorpsePosition(KillEffect.DeathSourceEnum deathSource)
	{
		var corpse = PlayerCorpse.Value.GetComponent<PlayerCorpse>();
		corpse.Entity.Position = Entity.Position;
		if (deathSource == KillEffect.DeathSourceEnum.Bullet)
		{
			corpse.DeathAnim = "die";
			corpse.PlayerAnimator.SpineInstance.StateMachine.SetTrigger(corpse.DeathAnim);
			SFX.Play(Assets.GetAsset<AudioAsset>("sfx/more/get_shot.wav"), new(){Positional=true, Position=Entity.Position});
		}
		else
		{
			corpse.DeathAnim = "die";
			corpse.PlayerAnimator.SpineInstance.StateMachine.SetTrigger(corpse.DeathAnim);
			SFX.Play(Assets.GetAsset<AudioAsset>("sfx/MurderMystery Player Character/swiped_revision.wav"), new(){Positional=true, Position=Entity.Position});
		}
	}
	
	public override Vector2 CalculatePlayerVelocity(Vector2 currentVelocity, Vector2 input, float deltaTime)
	{
		float speedMultiplier = PlayerRole == PlayerRole.Seeker ? SeekerSpeed : 1f;
		return DefaultPlayerVelocityCalculation(currentVelocity, input, deltaTime, speedMultiplier);
	}
}

public abstract class MyEffect : AEffect
{
	public new HNSPlayer Player => (HNSPlayer)base.Player;
}

public class PropEffect : MyEffect
{
	public override bool IsActiveEffect => false;
	public override bool GetInterruptedByNewActiveEffects => true;
	public override bool IsValidTarget => true;

	public Vector2 EyeTarget;
	public Entity LastPropSelected;

	public override void OnEffectStart(bool isDropIn)
	{
		Player.CurrentPropIndex.OnSync += OnPropChange;
		Player.PropEntity.LocalEnabled = true;
		Player.AddEmoteBlockReason(nameof(PropEffect));
		Player.AddInvisibilityReason(nameof(PropEffect));
		Player.AddNameInvisibilityReason(nameof(PropEffect));
		
		RefreshProp();
	}

	public override void OnEffectEnd(bool interrupt)
	{
		Player.CurrentPropIndex.OnSync -= OnPropChange;
		Player.PropEntity.LocalEnabled = false;
		Player.PropEyes.Entity.LocalEnabled = false;
		Player.RemoveInvisibilityReason(nameof(PropEffect));
		Player.RemoveNameInvisibilityReason(nameof(PropEffect));
		Player.RemoveEmoteBlockReason(nameof(PropEffect));
	}

	public void RefreshProp()
	{
		var prop = WorldManager.Instance.CurrentWorld.GetPristineProp(Player.CurrentPropIndex);
		LastPropSelected = prop.Entity;
		Player.PropSpriteRenderer.Sprite = prop.Sprite;
		// Player.PropSpriteRenderer.DepthOffset = prop.DepthOffset;
		Player.PropEntity.LocalScale = prop.Entity.Scale;
		
		var propRoot = prop.Entity.TryGetChildByName("root");
		if (propRoot != null) 
		{
			Player.PropEntity.LocalPosition = propRoot.LocalPosition * -1 * prop.Entity.Scale;
			Player.PropSpriteRenderer.DepthOffset = (Player.Position.Y - Player.PropEntity.Position.Y) / prop.Entity.Scale.Y;

			// set the new collider size
			Player.ActualPropCollider.Size = prop.GetWorldSize();
			

			var propEyes = propRoot.TryGetChildByName("eyes");
			if (propEyes != null)
			{
				Player.PropEyes.Entity.LocalPosition = propEyes.LocalPosition * prop.Entity.Scale;
				Player.PropEyes.DepthOffset = Player.Position.Y - Player.PropEyes.Position.Y - 0.001f;
			}
		}

		Player.PropEyes.SpineInstance.StateMachine.SetTrigger("appear");
	}

	public void OnPropChange(int old, int newValue)
	{
		RefreshProp();
	}

	public override void OnEffectUpdate()
	{
		if (Player.PropEntity != null && LastPropSelected != null)
		{
			var scale = Player.GetFacingDirection() ? LastPropSelected.Scale : LastPropSelected.Scale * new Vector2(-1,1);
			Player.PropEntity.LocalScale = scale;
			
			// shows a debug collider
			// var min = Player.PropEntity.Position - Player.PropSpriteRenderer.GetWorldSize() / 2 * Player.PropEntity.LocalScale;
			// var max = Player.PropEntity.Position + Player.PropSpriteRenderer.GetWorldSize() / 2 * Player.PropEntity.LocalScale;
			// using var _ = UI.PUSH_CONTEXT(UI.Context.WORLD);
			// IM.Quad(new IM.QuadData(min, max, Vector4.Red * .5f, UI.WhiteSprite));
		}
		if (Network.IsClient)
		{
			var localPlayerRole = PlayerRole.Spectator;
			if (Network.LocalPlayer.Alive())
			{
				localPlayerRole = (Network.LocalPlayer as HNSPlayer).PlayerRole;
			}


			Player.PropEyes.Entity.LocalEnabled = localPlayerRole == PlayerRole.Prop;
		}

		HNSPlayer nearestHunter = null;
		foreach (var player in Scene.Components<HNSPlayer>())
		{
			if (player.PlayerRole == PlayerRole.Seeker)
			{
				if (nearestHunter == null || Vector2.Distance(Player.Position, player.Position) < Vector2.Distance(Player.Position, nearestHunter.Position))
				{
					nearestHunter = player;
				}
			}
		}

		var target = Player.GetMousePosition();
		if (nearestHunter != null && Vector2.Distance(Player.Position, nearestHunter.Position) < 5)
		{
			target = nearestHunter.Position;
		}

		EyeTarget = Vector2.Lerp(EyeTarget, target, Time.DeltaTime * 20);

		var bonePos = EyeTarget - Player.PropEyes.Position;
		bonePos.X *= Math.Sign(Player.Entity.LocalScale.X);
		Player.PropEyes.SpineInstance.SetBonePosition("AIM", bonePos);
	}
}


public class WaitForAnimEffect : MyEffect
{
	public override bool IsActiveEffect => true;
	public override bool FreezePlayer => true;

	public override void OnEffectStart(bool isDropIn)
	{
		if (!isDropIn)
		{
			DurationRemaining = Player.SpineAnimator.SpineInstance.StateMachine.TryGetLayerByIndex(0).GetCurrentStateLength();
		}
	}

	public override void OnEffectEnd(bool interrupt)
	{
	}

	public override void OnEffectUpdate()
	{
	}
}

public class SpectatorEffect : MyEffect
{
	public override bool IsActiveEffect => false;
	public override bool IsValidTarget => false;

	public bool HasNameInvisReason = false;

	public void UpdateInvis()
	{
		if (Player.IsLocal || (Network.LocalPlayer.Alive() && Network.LocalPlayer.HasEffect<SpectatorEffect>()))
		{
			Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0.5f);
			if (HasNameInvisReason)
			{
				HasNameInvisReason = false;
				Player.RemoveNameInvisibilityReason(nameof(SpectatorEffect));
			}
		}
		else
		{
			Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 0);
			if (!HasNameInvisReason)
			{
				HasNameInvisReason = true;
				Player.AddNameInvisibilityReason(nameof(SpectatorEffect));
			}
		}
	}

	public override void OnEffectStart(bool isDropIn)
	{
		UpdateInvis();
		Player.SpineAnimator.DepthOffset = -10000;
		Player.SpineAnimator.SpineInstance.StateMachine.SetBool("ghost_form", true);
		Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = false;
		if (!isDropIn)
		{
			Player.AddEmoteBlockReason(nameof(SpectatorEffect));
		}

		if (Player.IsLocal && GameManager.Instance.VoiceChatEnabled)
		{
			Game.SetVoiceEnabled(false);
		}
	}

	public override void OnEffectUpdate()
	{
		UpdateInvis();
	}

	public override void OnEffectEnd(bool interrupt)
	{
		Player.RemoveEmoteBlockReason(nameof(SpectatorEffect));
		Player.SpineAnimator.DepthOffset = 0;
		Player.SpineAnimator.SpineInstance.StateMachine.SetBool("ghost_form", false);
		Player.SpineAnimator.SpineInstance.ColorMultiplier = new Vector4(1, 1, 1, 1);
		if (HasNameInvisReason)
		{
			Player.RemoveNameInvisibilityReason(nameof(SpectatorEffect));
		}
		Player.Entity.GetComponent<Circle_Collider>().LocalEnabled = true;

		if (Player.IsLocal && GameManager.Instance.VoiceChatEnabled)
		{
			Game.SetVoiceEnabled(true);
		}
	}
}

public partial class RoundStartAnimationEffect : MyEffect
{
	public override bool IsActiveEffect => true;
	public override bool FreezePlayer => true;

	public float Hold01;

	public override void OnEffectStart(bool isDropIn)
	{
	}

	public override void OnEffectEnd(bool interrupt)
	{
	}

	[ServerRpc]
	public static void ServerSkipIntro()
	{
		var player = Network.GetRemoteCallContextPlayer();
		if (player == null) return;
		CallClient_SkipIntro(player);
	}

	[ClientRpc]
	public static void SkipIntro(Player player)
	{
		player.RemoveEffect<RoundStartAnimationEffect>(true);
	}

	public override void OnEffectUpdate()
	{
		var totalTime = 6f;
		if (Player.IsLocal)
		{
			var bgTint01 = Ease.FadeInAndOut(0.1f, totalTime, ElapsedTime);

			using var _1 = UI.PUSH_LAYER(GameManager.IntroLayer);
			using var _2 = UI.PUSH_COLOR_MULTIPLIER(new Vector4(bgTint01, bgTint01, bgTint01, bgTint01));

			UI.Image(UI.ScreenRect, null, new Vector4(0, 0, 0, 0.9f));

			var pos01 = Ease.SlideInAndOut(0.1f, totalTime, ElapsedTime);
			var ts = GameManager.Instance.GetTextSettings(52);
			ts.Color = Vector4.White;
			ts.WordWrap = true;
			var rect = UI.SafeRect.Offset(pos01 * 100, 0);
			switch (Player.PlayerRole)
			{
				case PlayerRole.Prop:
				{
					var actualRect = UI.Text(rect, "Hide from the Seekers until time runs out!", ts);
					ts.Size = 64;
					ts.Color = new Vector4(0, 1, 1, 1);
					UI.Text(actualRect.TopRect().Grow(100, 500, 0, 500), "You are a Hider.\n\n", ts);
					break;
				}
				case PlayerRole.Seeker:
				{
					var actualRect = UI.Text(rect, "Kill all the Hiders before time runs out!", ts);
					ts.Size = 64;
					ts.Color = new Vector4(1, 0, 0, 1);
					UI.Text(actualRect.TopRect().Grow(100, 500, 0, 500), "You are a Seeker.\n\n", ts);
					break;
				}
			}

			var emptyButtonSettings = new UI.ButtonSettings(){ ColorMultiplier = Vector4.Zero };
			if (UI.Button(UI.ScreenRect, "CLOSE", emptyButtonSettings, new UI.TextSettings()).Pressed)
			{
				Hold01 += Time.DeltaTime;
			}
			else
			{
				if (Hold01 < 1) Hold01 = 0;
			}

			if (Hold01 >= 1)
			{
				CallServer_ServerSkipIntro();
			}

			Hold01 = (float)Math.Clamp(Hold01, 0, 1);

			ts.Color = new Vector4(1, 1, 1, 1);
			var holdRectBg = UI.SafeRect.BottomCenterRect().Offset(0, 200).Grow(10, 150, 10, 150);
			var holdRect = holdRectBg.Inset(2, 2, 2, 2).SubRect(Hold01, 0, 1, 1);
			UI.Image(holdRectBg, null, new Vector4(0.8f, 0.8f, 0.8f, 1));
			UI.Image(holdRect,   null, new Vector4(0.1f, 0.1f, 0.1f, 1));
			ts.Size = 28;
			var str = "Click and hold to close";
			if (Game.IsMobile)
			{
				str = "Tap and hold to close";
			}
			UI.Text(holdRectBg.Offset(0, 35), str, ts);
		}

		if (ElapsedTime >= totalTime)
		{
			Player.RemoveEffect(this, false);
		}
	}
}

public abstract class MyAbility : Ability
{
	public new HNSPlayer Player => (HNSPlayer)base.Player;

	public override bool CanTarget(Player p)
	{
		var player = (HNSPlayer)p;
		if (player.PlayerRole == PlayerRole.Spectator)
		{
			return false;
		}
		return true;
	}

	public override bool CanUse()
	{
		if (Player.PlayerRole == PlayerRole.Seeker && GameManager.Instance.State != GameState.Round)
		{
			return false;
		}
		if (Player.PlayerRole == PlayerRole.Prop && GameManager.Instance.State != GameState.Round && GameManager.Instance.State != GameState.Hiding)
		{
			return false;
		}
		if (Player.PlayerRole == PlayerRole.Spectator)
		{
			return false;
		}
		return true;
	}
}

public class KillEffect : MyEffect
{
	public enum DeathSourceEnum
	{
		Knife,
		Bullet,
	}

	public override bool IsActiveEffect => true;
	public override bool FreezePlayer => true;
	public override bool IsValidTarget => false;
	public override float DefaultDuration => Player.WasPresentAtRoundStart ? 10.0f : 1.0f;

	public DeathSourceEnum DeathSource = DeathSourceEnum.Knife;
	bool _canMove = false;
	float _timer = 1f;
	public override void OnEffectStart(bool isDropIn)
	{
		if (!isDropIn)
		{
			Player.AddInvisibilityReason(nameof(KillEffect));
			Player.SetCorpsePosition(DeathSource);
		}

		if (Network.IsServer)
		{
			Player.PlayerRole = PlayerRole.Spectator;
		}
	}

	public override void OnEffectEnd(bool interrupt)
	{
		Player.RemoveInvisibilityReason(nameof(KillEffect));
		if (!Player.WasPresentAtRoundStart) return;
		if (GameManager.Instance.State == GameState.EndRound) return;
		Player.PlayerCorpse.Value.Position = new Vector2(1000, 1000);
		if (Network.IsServer)
		{
			Player.PlayerRole = PlayerRole.Seeker;
			var hunterSpawns = WorldManager.Instance.CurrentWorld.HunterSpawns;
			var rand = new Random();
			Player.Teleport(hunterSpawns[rand.Next(0, hunterSpawns.Count - 1)].Position);
		}
	}

	public override void OnEffectUpdate()
	{
		_timer -= Time.DeltaTime;
		if (_timer <= 0.0 && !_canMove)
		{
			_canMove = true;
			Player.RemoveInvisibilityReason(nameof(KillEffect));
			Player.RemoveFreezeReason(nameof(KillEffect));
		}
		
		if (!Player.IsLocal) return;
		if (!Player.WasPresentAtRoundStart) return;
		if (GameManager.Instance.State == GameState.EndRound) return;
		var timeLeftRect = UI.ScreenRect.CutBottom(500);
		UI.Text(timeLeftRect, $"You'll be revived as seeker in: {(int)DurationRemaining}", GameManager.Instance.GetTextSettings(60, 0f, null, UI.HorizontalAlignment.Center));
	}
}



public class PlayerCollisionChild : Component
{
	public HNSPlayer Player;
}

public class ProjectileIgnore : Component
{
}

public partial class PlayerCorpse : Component
{
	[Serialized] public Spine_Animator PlayerAnimator;
	[Serialized] public string PlayerName;
	[Serialized] public int ColorIndex;
	[Serialized] public string[] PlayerSkins;

	public string DeathAnim = "die";

	public override void Awake()
	{
        PlayerAnimator.Awaken();

		{
			var sm = StateMachine.Make();
			var baseLayer = sm.CreateLayer("base");
			var appearState = baseLayer.CreateState("appear", 0, false);
			var idleState = baseLayer.CreateState("idle", 0, true);
			var appearTrigger = sm.CreateVariable("appear", StateMachineVariableKind.TRIGGER);
			baseLayer.SetInitialState(idleState);
			baseLayer.CreateTransition(idleState, appearState, false).CreateTriggerCondition(appearTrigger);
			baseLayer.CreateTransition(appearState, idleState, true);
			Entity.TryGetChildByIndex(0).LocalEnabled = false;
		}

		{
			var sm = StateMachine.Make();
			var baseLayer = sm.CreateLayer("base");
			var idleState = baseLayer.CreateState("Idle", 0, true);
			var deathSwiped = baseLayer.CreateState("Death_No_HP", 0, false);
			//var deathThrown = baseLayer.CreateState("Death_No_HP", 0, false);
			var dieTrigger = sm.CreateVariable("die", StateMachineVariableKind.TRIGGER);
			//var dieThrowTrigger = sm.CreateVariable("die_throw", StateMachineVariableKind.TRIGGER);
			baseLayer.SetInitialState(idleState);
			baseLayer.CreateGlobalTransition(deathSwiped).CreateTriggerCondition(dieTrigger);
			//baseLayer.CreateGlobalTransition(deathThrown).CreateTriggerCondition(dieThrowTrigger);
			PlayerAnimator.SpineInstance.SetStateMachine(sm, Entity);

			PlayerAnimator.SetCrewchsia(ColorIndex);
			PlayerAnimator.SpineInstance.SetSkeleton(Assets.GetAsset<SpineSkeletonAsset>("hns_rig"));
			foreach (var skin in PlayerSkins)
			{
				PlayerAnimator.SpineInstance.EnableSkin(skin);
			}
			PlayerAnimator.SpineInstance.RefreshSkins();
		}

		if (!string.IsNullOrEmpty(DeathAnim))
		{
			PlayerAnimator.SpineInstance.Update(0);
			//PlayerAnimator.SpineInstance.StateMachine.SetTrigger(DeathAnim);
			PlayerAnimator.SpineInstance.StateMachine.SetTrigger("death");
			//PlayerAnimator.SpineInstance.Update(0);
			//PlayerAnimator.SpineInstance.Update(10);
		}
	}
}
