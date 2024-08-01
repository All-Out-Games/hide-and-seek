using AO;

public class HNSPlayer : Player
{
    private SyncVar<int> _playerRole = new((int)PlayerRole.Spectator);
    public PlayerRole PlayerRole
    {
        get => (PlayerRole)_playerRole.Value;
        set => _playerRole.Set((int)value);
    }

    public SyncVar<int> CurrentPropIndex = new(0);

    public SyncVar<Entity> PlayerCorpse = new();

    public override void Awake()
    {
        SpineAnimator.Entity.LocalScale = new Vector2(0.528f, 0.528f);
        CurrentPropIndex.OnSync += OnPropChange;

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
    }

    public override void Start()
    {
        if (Network.IsServer)
        {
            if (GameManager.Instance.State == GameState.WaitingForPlayers || GameManager.Instance.State == GameState.CountingDown)
            {
                PlayerRole = PlayerRole.Prop;
            }
        }
    }

    public void PreparePlayerForRound()
    {
        if (Network.IsServer)
        {
            var playerCorpse = Assets.GetAsset<Prefab>("CorpseRend.prefab").Instantiate<PlayerCorpse>();
            playerCorpse.Entity.Name = $"{Name}_corpse";
            playerCorpse.Entity.Position = new Vector2(1000, 1000);
            playerCorpse.PlayerName = Name;
            playerCorpse.ColorIndex = ColorIndex;
            playerCorpse.PlayerSkins = SpineAnimator.SpineInstance.GetSkins();
            Network.Spawn(playerCorpse.Entity);
            PlayerCorpse.Set(playerCorpse.Entity);
        }

        if (PlayerRole == PlayerRole.Hunter || PlayerRole == PlayerRole.Prop)
        {
            AddEffect<RoundStartAnimationEffect>();
        }
    }

    public override void Update()
    {
        if (IsLocal && GameManager.Instance.State == GameState.Round)
        {
            if (PlayerRole == PlayerRole.Hunter)
            {
                DrawDefaultAbilityUI(new AbilityDrawOptions(){
                    AbilityElementSize = 75,
                    Abilities = new Ability[]{
                        GetAbility<GunAbility>(),
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

    public void OnPropChange(int old, int newValue)
    {
    }

    public void SetCorpsePosition(KillEffect.DeathSourceEnum deathSource)
    {
        var corpse = PlayerCorpse.Value.GetComponent<PlayerCorpse>();
        corpse.Entity.Position = Entity.Position;
        if (deathSource == KillEffect.DeathSourceEnum.Bullet)
        {
            corpse.PlayerAnimator.SpineInstance.StateMachine.SetTrigger("die");
            corpse.DeathAnim = "die";
            SFX.Play(Assets.GetAsset<AudioAsset>("sfx/more/get_shot.wav"), new(){Positional=true, Position=Entity.Position});
        }
        else
        {
            corpse.PlayerAnimator.SpineInstance.StateMachine.SetTrigger("die");
            corpse.DeathAnim = "die";
            SFX.Play(Assets.GetAsset<AudioAsset>("sfx/MurderMystery Player Character/swiped_revision.wav"), new(){Positional=true, Position=Entity.Position});
        }
    }
}

public abstract class MyEffect : AEffect
{
    public new HNSPlayer Player => (HNSPlayer)base.Player;
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
            var ts = GameManager.GetTextSettings(52);
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
                case PlayerRole.Hunter:
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
        if (GameManager.Instance.State != GameState.Round)
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
    public override float DefaultDuration => 1.0f;

    public DeathSourceEnum DeathSource = DeathSourceEnum.Knife;

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
    }

    public override void OnEffectUpdate()
    {
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

    [Serialized] public string DeathAnim;

    public override void Start()
    {
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
            var deathSwiped = baseLayer.CreateState("MURD_002/death_swiped", 0, false);
            var deathThrown = baseLayer.CreateState("MURD_002/death_throw", 0, false);
            var dieTrigger = sm.CreateVariable("die", StateMachineVariableKind.TRIGGER);
            var dieThrowTrigger = sm.CreateVariable("die_throw", StateMachineVariableKind.TRIGGER);
            baseLayer.SetInitialState(idleState);
            baseLayer.CreateGlobalTransition(deathSwiped).CreateTriggerCondition(dieTrigger);
            baseLayer.CreateGlobalTransition(deathThrown).CreateTriggerCondition(dieThrowTrigger);
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
            PlayerAnimator.SpineInstance.StateMachine.SetTrigger(DeathAnim);
            PlayerAnimator.SpineInstance.Update(0);
            PlayerAnimator.SpineInstance.Update(10);
        }
    }
}